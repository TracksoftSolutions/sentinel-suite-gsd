# Clearance Profiles

## Overview

Clearance Profiles closes the seam Access Credential Management deliberately left open (`clearance_profile_ref` as a bare forward pointer, "never models zones, doors, or access levels") — and, in doing so, corrects that doc's own field shape: **what a person can access is decoupled from which physical credential they carry.** A badge is an identifier; the *access groups/zones* assigned to a person are a separate, many-to-many, independently-expiring relationship. `CredentialAssignment.clearance_profile_ref` (singular) is retired here in favor of a proper **Clearance Profile Assignment** (Person ↔ Clearance Profile, many-to-many) — the realistic PACS shape, where one badge commonly carries several stacked access groups (general building access + a restricted server room + after-hours loading dock) that can change independently of reissuing the card itself.

This doc follows Access Credential Management's exact multi-vendor ingestion-first pattern: a connected PACS's own access-group/level definitions are the primary source (mirrored in, authoritative, never independently overridden), with native authoring as the fallback for a site with no adaptor. Zone boundaries reference **Location Registry directly** — no new place concept, and no wait for Module 9's future Zone Mapping/Location Hierarchy Designer, which only adds authoring convenience and GIS geometry later, not a new reference mechanism this doc needs today.

## Actors & Roles

- **Security/Access Admin** — authors native Clearance Profiles and Schedule Definitions, approves profile assignments per policy.
- **Site/Tenant Admin** — configures PIAM Adaptor `clearance_sync`, Clearance Assignment Approval Policy.
- **Supervisor** — requests a profile assignment for a report.
- **Employee/Contractor** — the assignment holder; sees their own current access.

## Functional Requirements

### Clearance Profile as a Definition, not an Entity
1. **Clearance Profile** registers as a Site/Tenant Admin-authored **Definition** (the same catalog shape as Call Type Definition or Badge Template — no dedup risk, no independent identity lifecycle) rather than a new Entity Registry Core type, carrying a name, description, an optional **Schedule Definition** reference, and zero or more **zone boundaries**.

### Multi-vendor ingestion (primary mode), mirroring Access Credential Management
2. A PIAM Adaptor Registration's `clearance_sync` capability (new — alongside `visitor_sync`/`credential_sync`) pulls a connected PACS's own access-group/level definitions in as ingested Clearance Profiles (`source_system` set to the adaptor); ingested profiles are authoritative and never independently edited in Sentinel Suite, the identical posture Access Credential Management established for ingested Credentials.
3. Native authoring is the fallback for a site with no adaptor covering it, or where a tenant deliberately keeps profile authorship Sentinel-side.

### Zone boundaries
4. A Clearance Profile's zone boundary is one or more **Location refs**, each with an explicit `include_descendants` flag (default `false`) — listing a Building does **not** silently grant every Room beneath it unless an Admin explicitly opts into that cascade, the same "never let the system silently assume a real-world decision" discipline Guard Tour established for ambiguous matches. This reuses Location Registry's existing `HierarchyAssociation` traversal wholesale; no new place/zone concept is introduced.

### Time restriction
5. A Clearance Profile may reference a reusable, named **Schedule Definition** (`day_of_week_windows[]` — day, start time, end time; multiple windows per day are allowed, e.g. a lunch-hour lockout) — authored once, attached to any number of profiles, edited in one place. A profile with no schedule reference is unrestricted by time.

### Assignment (many-to-many, decoupled from Credential)
6. **Clearance Profile Assignment** registers as a new EntityAssociation kind (Person ↔ Clearance Profile), independent of which Credential(s) the person holds — a person can hold any number of profiles simultaneously, each tracked and expirable independently. *(Retrofit — Access Credential Management: `CredentialAssignment.clearance_profile_ref` is removed; a Credential and a person's Clearance Profile Assignments are now two independent relationships, both anchored on the same Person.)*
7. Granting a **native** assignment is gated behind a tenant-configurable **Clearance Assignment Approval Policy** (Settings & Preferences Definition, the same shape as Credential Approval Policy: required approver(s) — none/security/admin/both) — granting broader access is a risk decision, mirroring provisioning's own gate. An **ingested** assignment (mirroring PACS group membership) carries no local approval gate, consistent with ingestion's authoritative-mirror posture.
8. **Temporary Clearances**: an assignment may carry an `expires_at`. Expiration is scheduled as a one-off Background Job Processing job at assignment-creation time (no new future-dated-trigger mechanism) and executes with the same urgency as Access Credential Management's termination-triggered revocation — immediate, no approval delay, pushed out through a write-capable adaptor with the identical pending-confirmation honesty (`removal_pending_confirmation` → `removed`) where one applies, or immediate native removal where none does.

## Data Model / Fields

**Clearance Profile** (Definition, local to this doc)
- profile_id, tenant_id, name, description
- source_system (sentinel_native, or a specific PIAM Adaptor's adaptor_type)
- external_profile_ref (nullable)
- schedule_ref (nullable, FK → Schedule Definition)
- zone_boundaries[] (location_ref, include_descendants: bool)
- status (active, archived)

**Schedule Definition** (Settings & Preferences Definition, reusable)
- schedule_id, tenant_id, name
- day_of_week_windows[] (day, start_time, end_time)

**Clearance Profile Assignment** (new EntityAssociation kind — entity_id_a = Person, entity_id_b = Clearance Profile; association_id is the shared PK)
- assigned_at, removed_at (nullable)
- expires_at (nullable)
- status (pending_approval, active, removal_pending_confirmation, removed)
- source (sentinel_native, or the adaptor it was ingested from)

**Clearance Assignment Approval Policy** (Settings & Preferences Definition)
- tenant_id/site_id, required_approvers (none, security, admin, both)

**PIAM Adaptor Registration** *(retrofit — Pre-Registration Portal / Access Credential Management)*
- sync_capabilities{} gains `clearance_sync` (bool)

**Access Credential / CredentialAssignment** *(retrofit — Access Credential Management)*
- CredentialAssignment's `clearance_profile_ref` field is removed; access is now resolved via a Person's independent Clearance Profile Assignments, not any one Credential.

## States & Transitions

**Clearance Profile:** `active` → `archived` (native), or mirrors the adaptor's own lifecycle when ingested.

**Clearance Profile Assignment:** `pending_approval` (native, gated) → `active` → `removal_pending_confirmation` (expiration or manual removal, adaptor-confirmed) → `removed`. An ingested assignment skips `pending_approval` entirely and mirrors the adaptor's reported state directly.

## Integrations

- **Access Credential Management** *(retrofit)*: `clearance_profile_ref` removed from CredentialAssignment; this doc's Clearance Profile Assignment is the real mechanism. PIAM Adaptor Registration gains `clearance_sync`.
- **Location Registry**: zone boundaries reference existing Location records and their `HierarchyAssociation` chain directly — no new place concept.
- **Settings & Preferences**: owns Schedule Definition and Clearance Assignment Approval Policy.
- **Background Job Processing**: temporary-clearance expiration is a one-off scheduled job, the same registry Access Credential Management's recertification cadence and SITREP's recurring generation already use.
- **PIAM Adaptor (multi-vendor)**: primary ingestion source for both Clearance Profile definitions and assignment/membership state.
- **Structured Logging & Audit Trails**: every assignment grant, approval/rejection, and removal is audit-tier.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Supervisor | Employee/Contractor (self) |
|---|---|---|---|---|
| Author/archive a native Clearance Profile, Schedule Definition | ✅ | ✅ | ❌ | ❌ |
| Configure PIAM Adaptor `clearance_sync`, Clearance Assignment Approval Policy | ✅ | ❌ | ❌ | ❌ |
| Approve/reject a native assignment | ✅ | ✅ (per resolved policy) | ✅ (if resolved as an approver role) | ❌ |
| Request an assignment for a report | ✅ | ✅ | ✅ | ❌ |
| View own current assignments | ✅ | ✅ | ✅ (own reports) | ✅ (own only) |

## Non-Functional / Constraints

- An ingested Clearance Profile or assignment is never independently edited in Sentinel Suite — the same authoritative-mirror discipline as Access Credential Management, for the same reason.
- Temporary-clearance expiration must execute without approval delay, consistent with the platform's deprovisioning-is-not-gated precedent.
- `include_descendants` defaults `false` — a zone boundary never silently widens beyond what was explicitly configured.

## Acceptance Criteria

- [ ] A person can hold three simultaneous Clearance Profile Assignments (e.g., general access + server room + loading dock), each independently visible and independently expirable.
- [ ] Removing/reissuing a person's Credential does not affect their existing Clearance Profile Assignments — the two are confirmed independent.
- [ ] A native profile assignment held at `pending_approval` under a configured policy cannot become `active` without the required approver(s); an ingested assignment mirroring PACS group membership requires no local approval.
- [ ] Listing a Building as a zone boundary with `include_descendants = false` does not grant access implied for its Rooms; setting it `true` does, verified against Location Registry's existing hierarchy.
- [ ] Editing a shared Schedule Definition's time window updates every Clearance Profile referencing it, with no per-profile duplication needed.
- [ ] An assignment with `expires_at` set is automatically removed at that timestamp with no approval delay, through the same pending-confirmation honesty as a manual revocation where a write-capable adaptor is involved.
- [ ] An ingested Clearance Profile is never editable through Sentinel Suite's own authoring UI.

## Open Questions

- Exact adaptor-side access-group-to-Clearance-Profile field mapping fidelity — technical-spec, vendor-dependent.
- Whether Clearance Profile ever needs its own dedup/duplicate-name detection at scale (many tenants authoring many profiles) — no target customer need identified yet; not built here.
- Whether a future richer GIS-drawn zone (Module 9's Zone Mapping) should retroactively let a Clearance Profile bind to a drawn polygon instead of a discrete Location list — flagged for reconciliation once that module is specified, same deferred-integration posture used throughout the platform.
