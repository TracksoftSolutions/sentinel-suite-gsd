# Blob/File Storage

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Blob/File Storage is the platform's shared file-storage adaptor: the mechanism every feature that stores actual file bytes (Document Registry attachments, Incident evidence photos/video, AI-generated report exports, Tenant Management's data exports and Frozen Engagement Copies) goes through, rather than each feature talking to a storage backend directly. It follows the same provider-adaptor pattern as GIS & Mapping Services and AI/LLM Services: tenant-selectable cloud object storage (S3-compatible) or a self-hostable option for on-prem/air-gapped DOE tenants, with the isolation tier assigned in Tenant Management constraining which adaptor a given tenant can even choose.

This doc owns the storage mechanism, malware scanning, retention/orphan cleanup, and content-hash deduplication. It does not own what any specific feature's files mean, how long *that feature's* records need to keep them, or the metadata record describing a file (Document Registry already owns that — see `document-registry.md`, which explicitly deferred the storage backend to this doc).

## Actors & Roles

- **Platform Super Admin** — configures which storage adaptors are enabled platform-wide and which are restricted to which isolation tiers.
- **Tenant Admin** — selects/confirms their tenant's storage adaptor where more than one is eligible for their isolation tier; sees quarantine/scan events for their tenant.
- **Every platform feature/module** — uploads/retrieves files through this engine's API rather than any direct storage SDK call.
- **Security/Compliance Officer** — reviews quarantine events and malware scan audit history.

## User Stories

- As a **feature developer**, I want to upload a file and get back a durable reference, without knowing or caring whether it lands in S3, Azure Blob, or a self-hosted MinIO instance for that tenant.
- As a **DOE Tenant Admin** on an `on_prem` isolation tier, I want my tenant's files physically stored on our self-hosted object storage with no path to a cloud provider, enforced structurally, not by configuration discipline.
- As a **Security Officer**, I want every uploaded file scanned for malware before it becomes available to any user, and to be notified if something is quarantined, so an infected attachment can never reach another user's device.
- As a **Tenant Admin**, I want the same photo attached to two different Incidents to only cost us storage once, since our guards often reuse the same evidence photo across a linked BOLO and an Incident.
- As a **Platform Super Admin**, I want orphaned files (upload started but never attached to a record, or the referencing record was deleted without cleanup) to eventually get purged automatically, so storage doesn't silently accumulate garbage forever.

## Functional Requirements

### Provider adaptors
1. Blob/File Storage is provider-adaptor architected: at minimum, a cloud S3-compatible adaptor (for SaaS tenants) and a self-hostable adaptor (e.g., MinIO or equivalent, for `dedicated_db`/`on_prem` tenants) at launch, following the same adaptor pattern established by GIS & Mapping Services and AI/LLM Services.
2. A tenant's eligible adaptor set is constrained by its Tenant Management isolation tier: `shared`-tier tenants use the platform's default cloud adaptor; `dedicated_db` tenants may select a dedicated cloud bucket/account; `on_prem` tenants are restricted to self-hostable adaptors only, enforced at configuration time (per the platform's existing adaptor-restriction pattern), never merely recommended.
3. Every stored file is encrypted at rest, consistent with the platform's general sensitive-data encryption standard (matching Document Registry's existing requirement).

### Upload & malware scanning
4. Every uploaded file is scanned for malware before it is marked available for retrieval by any consumer. Scanning is asynchronous relative to the initial upload acknowledgment but blocks availability — a file is in a `scanning` state, not yet fetchable, until the scan clears.
5. A file that fails scanning is moved to `quarantined` — not deleted — with an audit-tier event, and the uploading user/tenant's Security Officer/Admin is notified. A quarantined file is never retrievable by ordinary consumers; only a Platform Super Admin (or designated Security role) can inspect a quarantine record for incident-response purposes.

### Deduplication
6. Files are deduplicated by content hash, reference-counted: a second upload of byte-identical content within the same tenant's isolation boundary stores no new physical copy, instead incrementing a reference count against the existing physical blob and returning a new logical reference for the new record association.
7. Deduplication is scoped **within a tenant's isolation boundary, never across tenants** — even for `shared`-tier tenants that could technically share physical storage, content hashes are namespaced per tenant. This is a deliberate constraint, not an optimization left on the table: cross-tenant dedup would create a storage-layer side channel (two tenants' upload timing/reference-count behavior could leak information about each other) and is structurally impossible anyway for `dedicated_db`/`on_prem` tenants, whose physical storage doesn't overlap with any other tenant's.
8. Deleting the last reference to a physical blob (reference count reaches zero) marks it for orphan cleanup (item 10), not immediate physical deletion — giving the retention/undo window a record-owning feature may still need.

### Retention & cleanup
9. Blob/File Storage executes retention/deletion decisions made by the referencing record's owning feature (Document Registry's retention rules, Tenant Management's offboarding export/archive/purge) — it has no opinion on how long a *referenced* file should live.
10. Independently, Blob/File Storage owns a **baseline orphan-cleanup sweep**: a file with zero references (never attached to a record, or its last reference was removed) that stays orphaned past a platform-configured grace period (default 30 days) is automatically purged, as a safety net against storage leaks from failed or incomplete cleanup elsewhere — this is a backstop, not a substitute for a feature's own retention logic.

## Data Model / Fields

**Stored Blob** (physical, one per unique content hash per tenant)
- blob_id, tenant_id, content_hash, storage_adaptor, storage_key, size_bytes
- scan_status (scanning, clean, quarantined), scanned_at
- reference_count, first_uploaded_at, last_orphaned_at (nullable — set when reference_count hits zero)

**Blob Reference** (logical, one per record association)
- reference_id, blob_id, owning_feature, owning_record_ref, created_at, created_by

**Quarantine Record**
- blob_id, tenant_id, detected_at, scan_engine_result, uploading_account_ref, reviewed_by (nullable), reviewed_at (nullable), disposition (nullable — confirmed_malicious, false_positive_released)

## States & Transitions

**Stored Blob (scan):** `scanning` → `clean` (available to consumers) or `quarantined` (blocked, audit event, notification).

**Stored Blob (lifecycle):** `referenced` (reference_count > 0) → `orphaned` (reference_count = 0, grace period clock starts) → `purged` (grace period elapsed, physically deleted) or back to `referenced` (a new reference is added before purge).

## Integrations

- **Document Registry**: primary consumer — `file_ref` (deferred to this doc in `document-registry.md`) is a Blob Reference; Document Registry's own retention rules drive when a reference is removed.
- **Tenant Management**: offboarding data export and Frozen Engagement Copy snapshots both produce/reference files through this engine; isolation tier assignment constrains adaptor eligibility (item 2).
- **Background Job Processing**: malware scanning, orphan-cleanup sweeps, and dedup reference-count maintenance are all background jobs, not synchronous request-path work.
- **Structured Logging & Audit Trails**: quarantine events and adaptor configuration changes are audit-tier events.
- **Notifications Engine**: quarantine detection notifies the relevant Security Officer/Tenant Admin.
- **AI/LLM Services, Incident Reporting & Management, AI-Assisted Incident Report Writing**: evidence photos/video and AI-generated report exports all store through this engine.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Security/Compliance Officer | Any authenticated user |
|---|---|---|---|---|
| Configure enabled adaptors platform-wide | ✅ | ❌ | ❌ | ❌ |
| Select/confirm tenant's adaptor (where eligible) | ✅ | ✅ (own tenant) | ❌ | ❌ |
| Upload a file (through owning feature) | ✅ | ✅ | ✅ | ✅ (per owning feature's own permission gate) |
| View/inspect a quarantine record | ✅ | ❌ | ✅ (own tenant) | ❌ |
| Release a false-positive quarantine | ✅ | ❌ | ✅ (own tenant, subject to review) | ❌ |

## Non-Functional / Constraints

- `on_prem` tenant file storage must be structurally incapable of touching a cloud adaptor — enforced at configuration/deployment level, matching Tenant Management's isolation tier constraint.
- Malware scanning must not introduce an unacceptable delay to normal feature workflows (e.g., attaching an evidence photo mid-Incident) — target scan turnaround is a technical-spec NFR, but the `scanning` intermediate state must be visibly communicated to the uploading user, not silently pending.
- Dedup reference-counting must be race-safe under concurrent uploads of identical content (two users uploading the same photo simultaneously must not create two physical blobs nor under-count references).
- Orphan cleanup must never purge a blob with any live reference, and must be safely re-runnable (idempotent) if interrupted mid-sweep.

## Acceptance Criteria

- [ ] A file uploaded by a `shared`-tier tenant lands on the platform's default cloud adaptor; a file uploaded by an `on_prem` tenant lands only on that tenant's self-hosted adaptor, never a cloud one.
- [ ] An uploaded file is unavailable to any consumer while `scanning`, becomes retrievable once `clean`, and is blocked with an audit event and admin notification if `quarantined`.
- [ ] Uploading byte-identical content twice within the same tenant results in one physical blob with reference_count = 2, not two physical blobs.
- [ ] The same content hash uploaded by two different tenants results in two separate physical blobs (no cross-tenant dedup).
- [ ] Removing the last reference to a blob marks it `orphaned`; it is not physically deleted until the grace period elapses, and adding a new reference before then returns it to `referenced` without data loss.
- [ ] An orphaned blob past its grace period is purged by the cleanup sweep; a blob that gained a new reference just before the sweep runs is not purged.
- [ ] A quarantined file reviewed and marked `false_positive_released` becomes retrievable again with a full audit trail of the review decision.

## Open Questions

- Exact malware-scan engine/provider choice (self-hosted ClamAV-class vs. a cloud scanning API) — likely itself needs the adaptor treatment for air-gapped tenants (a cloud scanning API is unusable on-prem); to be confirmed during technical spec.
- Default orphan grace period (30 days assumed above) and whether it should be tenant-configurable via Settings & Preferences rather than a single platform default — plausible, not yet confirmed.
- Whether large media (video) needs a distinct storage/CDN delivery path (e.g., signed URLs, streaming) versus small document attachments, or whether this doc's single model covers both adequately — flagged for technical spec rather than resolved here.
