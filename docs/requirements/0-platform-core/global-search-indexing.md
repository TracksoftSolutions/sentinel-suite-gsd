# Global Search & Data Indexing

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Global Search & Data Indexing is the shared search backbone the Command Palette's "universal search" and CLI-Style Input's identifier lookups both already assumed exists but never specified. A feature explicitly registers which of its record types (and which fields) are searchable — mirroring the opt-in registration pattern already used for Settings, Feature Flags, and Command/Action Bus actions — and this engine keeps a precomputed search index in sync as those records change, driven by Domain Events. It does not decide who can see a given result: every search query re-applies the requesting user's full RBAC+ABAC scope live, consistent with the platform's existing "ABAC evaluated at access time, never cached into a stale grant" posture (Authentication & Authorization).

This doc owns indexing registration, index sync, and query/ranking; it does not own what any specific record type's searchable fields mean, and it does not own permission evaluation itself (that stays with each record type's existing access rules).

## Actors & Roles

- **Platform Super Admin** — manages the search adaptor configuration platform-wide and per-tenant.
- **Tenant Admin** — selects/confirms their tenant's search adaptor where more than one is eligible for their isolation tier.
- **Every platform feature/module** — registers its searchable record types/fields against this engine rather than building bespoke search.
- **Any user** — the actual consumer, via Command Palette and CLI-Style Input (this doc has no UI of its own).

## User Stories

- As a **user**, I want typing a partial incident number, a person's name, or a plate number into the palette to surface matching records instantly, regardless of which module they live in.
- As a **feature developer**, I want to register my record type's searchable fields once and have indexing, sync-on-change, and query support come for free, rather than building my own search.
- As a **DOE Tenant Admin** on `on_prem` isolation, I want to choose a self-hostable search adaptor so our search index never depends on a cloud service, matching every other Module 0 mechanism's isolation posture.
- As a **Security Officer**, I want to be confident that a search result never bypasses a user's actual data-scope/clearance restrictions, even if the index itself is slightly stale — the live permission check is the real gate, not the index.
- As a **feature developer** with a genuinely internal/draft record type, I want it to simply never appear in global search unless I explicitly register it, so nothing gets surfaced platform-wide by accident.

## Functional Requirements

### Registration (opt-in)
1. A feature registers a **Search Registration** for a record type: which fields are indexed, field weighting for relevance (e.g., an identifier field weighted higher than a free-text notes field), and the display template for a result row. A record type not registered is never searchable, by default — explicit opt-in, not opt-out.
2. A feature can register only a subset of a record type's fields (e.g., index a Person's name/ID but not a free-text medical note field), independent of what that record type otherwise stores.

### Indexing & sync
3. The engine maintains a precomputed search index (adaptor-pattern — see below), kept in sync via Domain Events: create/update/delete events for any registered record type trigger an index write, so the index reflects near-real-time state without the query path ever reading the primary datastore directly for search.
4. Index entries carry enough coarse metadata (tenant_id, record type, registered fields) to make candidate retrieval efficient, but **carry no fine-grained permission/clearance data** — the index is not, and must never become, a second source of truth for access control.
5. A failed or delayed index write (e.g., a sync job failure) does not corrupt search — it is a background job (Background Job Processing) with the platform's standard retry/dead-letter behavior; a temporarily stale index degrades result completeness, never correctness (a user is never shown something they shouldn't see because of index staleness — permission is always re-checked live, item 6).

### Query & permission filtering
6. Every search query re-applies the requesting user's full RBAC+ABAC scope to candidate results **live, at query time** — the index returns candidates, the platform's existing access-control evaluation (Authentication & Authorization) filters them before any result reaches the user. This is the platform's deliberate choice over baking scope into the index: a stale or misconfigured index entry can never be the only thing standing between a user and a record they're not cleared for.
7. Results are ranked by relevance (registered field weighting, item 1) blended with recency/frequency signals the Command Palette already tracks per user.

### Adaptor & isolation
8. Global Search is provider-adaptor architected, matching GIS/AI-LLM/Blob Storage: a managed cloud search service for `shared`/`dedicated_db` tenants, and a self-hostable adaptor (e.g., an open-source search engine) for tenants that want or require it — including `on_prem` tenants, who are restricted to self-hostable adaptors only, enforced at configuration time.
9. Unlike storage/DB/background-job placement (which hard-require `on_prem` isolation), search adaptor choice is tenant-configurable within what their isolation tier allows — a `shared`-tier tenant could still opt into a self-hosted search adaptor if they wanted, though the platform default steers them to the managed option.

## Data Model / Fields

**Search Registration** (registered by a feature)
- record_type, owning_feature, indexed_fields[] (field, weight), result_display_template

**Search Index Entry** (adaptor-internal, one per indexed record)
- record_type, record_id, tenant_id, indexed_field_values (denormalized copy for search matching only — never the record's authoritative data)
- last_synced_at

**Search Adaptor Configuration**
- tenant_id, adaptor_key, eligible_adaptors[] (constrained by isolation tier per item 8)

## States & Transitions

**Search Index Entry:** `synced` → `stale` (a change event failed to apply, retrying per Background Job Processing) → `synced` (retry succeeds) — a record is never removed from "existing" state due to sync failure; worst case it's found via slower fallback paths the owning feature may offer (e.g., a direct lookup by exact ID), outside this doc's scope.

## Integrations

- **Command Palette**: primary consumer — its "universal search" result category is this engine.
- **CLI-Style Input**: identifier-based lookups (record number, name, plate) resolve through this engine.
- **Domain Events**: index sync is driven entirely by Domain Events emitted on create/update/delete of any registered record type.
- **Background Job Processing**: index sync writes and any reindexing sweep are jobs against that engine, inheriting its retry/dead-letter behavior.
- **Authentication & Authorization**: every query's result set is filtered through the existing RBAC+ABAC evaluation before reaching the user — this doc never duplicates that logic.
- **Tenant Management**: search adaptor eligibility is constrained by a tenant's isolation tier.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Feature developer (via feature build) | Any user |
|---|---|---|---|---|
| Configure enabled adaptors platform-wide | ✅ | ❌ | ❌ | ❌ |
| Select tenant's search adaptor (where eligible) | ✅ | ✅ (own tenant) | ❌ | ❌ |
| Register a record type as searchable | ✅ | ❌ | ✅ | ❌ |
| Perform a search (results filtered to own scope) | ✅ | ✅ | n/a | ✅ |

## Non-Functional / Constraints

- Query latency must meet the Command Palette's existing sub-100ms-perceived NFR — the index exists specifically to make that achievable without querying the primary datastore per keystroke.
- The index must never store or expose data a record type didn't explicitly register as a searchable field (item 2) — this is a data-minimization requirement, not just a feature convenience.
- Permission filtering (item 6) must add negligible latency — evaluated efficiently against the already-loaded candidate set, not as N individual re-fetches.
- `on_prem` tenants selecting a self-hostable adaptor must have their index run entirely within their own infrastructure, consistent with the isolation posture of every other Module 0 mechanism.

## Acceptance Criteria

- [ ] A record type not registered for search never appears in any search result, regardless of how permissive the querying user's access is.
- [ ] Creating, updating, or deleting a registered record's indexed fields is reflected in search results within the platform's near-real-time sync target, without requiring a manual reindex.
- [ ] A user with restricted data-scope (e.g., a Site-scoped role) never sees a search result for a record outside their scope, even if that record is present in the index.
- [ ] An index sync failure retries per Background Job Processing's standard behavior and does not silently and permanently drop a record from search.
- [ ] An `on_prem` tenant configured with a self-hostable search adaptor has zero dependency on a cloud search service.
- [ ] Search results are ranked with registered field weighting reflected (e.g., an exact identifier match outranks a partial free-text match).

## Open Questions

- Exact reindexing/backfill strategy when a feature registers a new Search Registration against pre-existing records (a one-time backfill job, presumably against Background Job Processing) — to be confirmed during technical spec.
- Whether relevance ranking needs a genuine scoring algorithm (BM25-class) versus the platform's default adaptor's out-of-the-box ranking — deferred to technical spec, adaptor-dependent.
- Whether search needs to support fuzzy/typo-tolerant matching beyond the Command Palette's already-stated fuzzy matching, or whether that's entirely the palette's own concern layered on top of this engine's exact/prefix candidate retrieval — to be confirmed.
