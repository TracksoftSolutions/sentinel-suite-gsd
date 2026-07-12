# Offline Data Sync

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Offline Data Sync lets the mobile app and fixed client terminals (e.g., hardened tablets/laptops at DOE facility checkpoints) keep working when disconnected — underground, in dead zones, or in air-gapped environments — by caching a role-relevant working set locally, queuing transactions performed while offline, and reconciling them with the server once connectivity returns. It resolves the PDD's open architectural question ("offline sync strategy: CRDTs, operational transforms, or custom") with a hybrid approach: CRDTs for naturally append-only data, field-level last-write-wins with full version history for mutable structured records.

## Actors & Roles

- **Guard / Officer (mobile)** — primary offline user; performs field work (DAR, checkpoint scans, incident drafting, citations) with or without connectivity.
- **Fixed Terminal User** — uses a site-installed terminal (e.g., DOE checkpoint station) with the same offline capability.
- **Supervisor** — reviews the conflict reconciliation queue when a sync produces a flagged conflict.
- **IT / Systems Administrator** — configures Network Profiles, triggers remote wipe, sets local data expiry windows.
- **Sync Engine** (system) — background process on each client managing the local store-and-forward queue and reconciliation.

## User Stories

- As a **Guard** patrolling an underground parking structure with no signal, I want to scan checkpoints and log DAR entries normally, so my shift record is complete without needing connectivity.
- As a **Guard**, I want my offline-drafted incident report to sync automatically and get its official incident number the moment I'm back in coverage, so I don't have to remember to manually sync.
- As a **Supervisor**, I want to see a flagged reconciliation queue when two conflicting edits land on the same record, so I can confirm nothing important was silently overwritten.
- As an **IT Admin**, I want large photo/video attachments to defer syncing until the device is on WiFi, so field officers on cellular data don't burn their data plan or degrade sync of critical text records.
- As an **IT Admin**, I want to remote-wipe a lost device's local cache, so a stolen tablet doesn't expose cached incident and person data.
- As a **DOE facility Guard** working a fixed checkpoint terminal in an air-gapped zone, I want the terminal to keep functioning fully offline for extended periods, so operations don't stop when the network link is down.

## Functional Requirements

### Local storage & data scope
1. Mobile app and fixed client terminals both support full offline operation with local storage (SQLite/IndexedDB).
2. Each client caches a role-relevant working set: the user's current shift assignment, active post orders, assigned checkpoints/routes, open incidents assigned to or created by the user, recent DAR entries, and reference data needed for those workflows (e.g., relevant Master Records entries) — not a full database mirror.
3. Cached data is encrypted at rest on-device, keyed to the authenticated session.
4. Cached data older than a configurable rolling window is automatically purged from the device even absent an explicit wipe.
5. An admin can trigger a remote wipe targeting a specific device's local cache; the wipe executes on that device's next network contact.

### Offline-capable actions
6. The following can be performed fully offline and queued for sync: DAR entries, checkpoint/tour scans, patrol checklist completion, courtesy patrol logs, incident report drafting (including photo/evidence attachment capture), citation/ticket issuance, shift passdown read-acknowledgment.
7. The following require live connectivity and are not queuable offline: dispatch assignment/acceptance, real-time dispatcher chat, live camera stream viewing, remote gate/barrier control. A panic-button trigger is captured locally instantly and transmitted the moment connectivity returns, but its EOC routing cannot occur while offline.

### Sync mechanics
8. The client maintains a store-and-forward queue of offline transactions, ordered per record.
9. On connectivity detection, sync starts automatically in the background; the user can also force a manual sync.
10. Failed sync attempts retry with exponential backoff.
11. Sync is tiered by priority: small text/structured records (DAR entries, checkpoint scans, incident text, citations) sync first on any connection quality; large attachments (photos, video, audio) are deferred to sync only when the active Network Profile (Settings & Preferences) permits — e.g., WiFi-only — configurable per tenant/site.

### Conflict resolution
12. Naturally append-only data (activity log entries, checkpoint scan events, passdown/chat messages) is modeled as CRDTs: concurrent offline entries from multiple actors merge without conflict since they simply accumulate.
13. Mutable structured records (e.g., an incident report's status or narrative field) use field-level last-write-wins by server-received timestamp, with full version history retained — no edit is silently discarded even when overwritten.
14. When a last-write-wins resolution occurs on a record with genuinely conflicting concurrent edits, the record is flagged and both conflicting versions remain visible in a supervisor-facing reconciliation queue. Sync completes without blocking; the flag is for awareness/correction, not a hard gate.

### ID generation
15. Records created offline receive a client-generated UUID immediately, making them fully usable and cross-referenceable offline.
16. On sync, the server centrally allocates the record's human-readable sequential number (e.g., citation #, incident #), preventing numbering collisions across devices.

## Data Model / Fields

**Sync Queue Entry** (client-local)
- queue_id (client UUID)
- record_type, record_client_id (UUID)
- operation (create, update, append)
- payload, created_at (client clock), priority_tier (text/structured vs attachment)
- sync_status (pending, syncing, synced, failed, conflict)
- retry_count, last_attempt_at

**Synced Record** (server)
- server_id (authoritative), client_id (UUID, for offline-origin traceability)
- sequential_display_number (nullable until server-assigned, for record types that need one)
- version_history[] (value, actor, timestamp, source: online/offline)
- conflict_flag (bool), conflict_versions[] (nullable)

**Device Cache Policy**
- tenant_id / site_id
- local_retention_window (rolling purge period)
- network_profile_ref (FK → Settings & Preferences Network Profile)

**Remote Wipe Command**
- device_id, issued_by, issued_at, executed_at (nullable), status

## States & Transitions

**Sync Queue Entry:** `pending` → `syncing` → `synced` (terminal success) | `failed` (retries, then remains `failed` with alert if retries exhausted) | `conflict` (resolved automatically per #13/14, still reaches `synced` but with `conflict_flag` set on the resulting record).

**Device Cache:** `active` → `wipe-pending` (command issued, awaiting device contact) → `wiped`. Independently, cached entries individually transition `cached` → `expired-purged` per retention window.

## Integrations

- **Authentication & Authorization**: offline session token validity and refresh-on-reconnect behavior is owned by that feature; this feature consumes it to gate what remains accessible while offline.
- **Structured Logging & Audit Trails**: every sync operation, conflict, and remote wipe is an audit-tier event; offline-originated audit entries queue and sync using the same mechanics as other data.
- **Notifications Engine**: alerts for exhausted retry failures, flagged conflicts (to supervisors), and remote wipe completion.
- **Settings & Preferences**: Network Profiles drive attachment sync deferral rules.
- **Master Records**: reference data cached locally (person/vehicle/location lookups relevant to the user's working set) originates here.
- **Security Operations features** (DAR, Guard Tour, Incident Reporting, Courtesy Patrol, Tickets/Citations): primary consumers of offline-capable actions defined in #6.
- **Dispatch/CAD**: defines the connectivity-required boundary for dispatch assignment/chat (#7).

## Permissions

| Action | Guard/Officer | Supervisor | IT Admin | Platform Super Admin |
|---|---|---|---|---|
| Perform offline-capable actions | ✅ (own device) | ✅ | ✅ | ✅ |
| Force manual sync | ✅ (own device) | ✅ | ✅ | ✅ |
| View conflict reconciliation queue | ❌ | ✅ (own scope) | ✅ | ✅ |
| Resolve/annotate a flagged conflict | ❌ | ✅ (own scope) | ✅ | ✅ |
| Configure local retention window | ❌ | ❌ | ✅ (own tenant) | ✅ |
| Trigger remote wipe | ❌ | ❌ | ✅ (own tenant) | ✅ |
| Configure Network Profile sync tiering | ❌ | ❌ | ✅ (own tenant) | ✅ |

## Non-Functional / Constraints

- Must support extended offline operation (multi-day) for DOE/air-gapped fixed terminals, not just brief dead-zone gaps.
- Local storage encryption and remote wipe are mandatory regardless of tenant isolation tier — treated as a security-critical control, not an optional feature.
- Sync engine must be resilient to abrupt connectivity loss mid-sync (partial sync must not corrupt the local queue or duplicate records on retry — idempotent operations keyed by client UUID).
- CRDT and last-write-wins mechanics must be extensively edge-case tested per the PDD's flagged high-severity risk on offline sync complexity.
- WCAG 2.1 / Section 508 accessible offline/sync-status indicators (e.g., pending-sync badge) in the mobile UI.
- Attachment deferral must not silently drop data — deferred items remain visibly "pending" to the user until synced.

## Acceptance Criteria

- [ ] A guard can complete a full patrol (checkpoint scans, DAR entries, a courtesy patrol checklist) fully offline, then have all of it sync correctly and in order once reconnected.
- [ ] Two guards editing the same append-only log (e.g., a shared passdown thread) offline simultaneously both see all entries merged with no data loss after sync.
- [ ] Two supervisors editing the same mutable field on the same incident report while both offline produce a flagged conflict visible in the reconciliation queue after sync, with both versions preserved in history.
- [ ] An incident report created offline is fully usable (referenceable, attachable) before sync, and receives its official sequential incident number immediately upon successful sync.
- [ ] A large photo attachment captured offline on cellular data queues and does not sync until the device is on WiFi (per Network Profile), while the accompanying incident text syncs immediately on any connection.
- [ ] Killing connectivity mid-sync and reconnecting does not duplicate or corrupt any record.
- [ ] An admin-issued remote wipe clears a target device's local cache on its next network contact.
- [ ] Cached data older than the configured retention window is purged from a device even with no wipe command issued.
- [ ] Attempting a connectivity-required action (e.g., accepting a dispatch) while offline is blocked with a clear "requires connectivity" state, not silently queued.
- [ ] A panic-button trigger fired while offline is captured immediately and transmitted to EOC the moment connectivity returns.

## Open Questions

- Exact local retention window default (e.g., 7 vs 14 days) and whether it should vary between mobile (shorter, more exposure risk) and fixed DOE terminals (potentially longer given extended offline operation needs) — to be set during technical spec.
- Whether fixed DOE terminals need a distinct, larger local data scope than mobile (e.g., full site mirror vs role-scoped working set) given extended air-gapped operation — flagged for deeper review with DOE-facility stakeholders.
- Specific reconciliation queue UX (e.g., does resolving a flag require an action, or is it purely informational/dismissible) — to be detailed when Supervisor-facing UI is specced.
- Interaction with future FedRAMP/air-gapped SaaS tiers regarding remote wipe reachability when a device may never re-contact a cloud control plane — deferred to deployment-model technical spec.
