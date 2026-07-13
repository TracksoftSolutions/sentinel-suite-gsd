# Offline Data Sync

**Module:** 0. Platform Core
**Status:** Draft — elicited; revised during platform design review (offline scope deliberately narrowed — see `_DECISIONS.md`)

## Overview

Offline Data Sync lets the mobile app and fixed client terminals keep **capturing field work** when disconnected — dead zones, coverage gaps, and areas a connected device can't reach — by caching a small read-only working set locally and queuing new records in an idempotent, append-only outbox that replays when connectivity returns.

It deliberately does **not** aim for offline parity with the online system. Offline capability is a **minimal capture subset with accepted degraded performance**. The platform's primary disconnected workflow for everything outside that subset is **radio relay to a connected Dispatcher**, using the on-behalf-of logging capability that already pervades the platform (Patrol starts, Dispatch phases, Safety Check-ins). Two grounding realities drive this scope:

- Most facilities — including DOE underground areas — have working WiFi; genuine dead zones are the exception, and offline windows are expected to be **shift-length at most (hours), not multi-day**.
- The most restricted zones (e.g., SCIFs) are typically places a mobile device **cannot enter at all** — there, device-side offline capability is moot by definition and radio relay to a Dispatcher is the *only* mechanism. This is precisely why a Dispatcher can perform the large majority of an officer's system actions on their behalf.
- Air-gapped facilities are a **deployment-model concern, not a client-sync concern**: they run a self-hosted local instance (per the PDD's deployment model), and client devices there are *online* to that local server.

This resolves the PDD's open architectural question ("offline sync strategy: CRDTs, operational transforms, or custom") with a **store-and-forward append-only outbox**, superseding the earlier hybrid CRDT + field-level last-write-wins design. Under this contract, offline writes are single-writer, insert-only, and unshared until sync — so write conflicts are impossible **by construction**, and no conflict-resolution engine exists in this feature at all.

## The Offline Contract

Every offline write falls into exactly one of three classes. This contract is also a forcing function on every future feature spec: the question is never "how does this feature work offline," it is "**which of its Activities are append-capturable offline, if any**" — and the default answer is *none*.

1. **Create new records offline.** New Activity rows (a DAR entry, a checkpoint scan, a patrol finding, a courtesy patrol log, a citation, a new incident and its updates, photos/attachments) are created with a client-generated UUID. A record created offline is **freely editable locally until its first successful sync** — it is single-writer and unshared, so local edits are safe. After first sync it is an ordinary server record, subject to the same rules as any other.
2. **Append rows to existing records.** Notes/updates to an existing Activity's timeline, read-acknowledgments, and **state-transition events on execution records the user is the sole assigned actor of** (e.g., completing their own in-progress Patrol) queue as appended events. Multiple authors appending to the same parent record — including a mix of offline and online officers on one incident — merge trivially: each row is independent, keyed by its own UUID.
3. **Never mutate shared server-side records offline.** Status/severity/category/assignment changes on shared records (an Incident, a Call), plan definitions, settings, and anything another actor can concurrently touch are online-only — or relayed by radio to a Dispatcher who performs them on-behalf-of. If a field officer needs to *signal* such a change while offline, it is captured as an appended timeline event (e.g., "requested status: closed") and the actual mutation is confirmed once connected.

**Tripwire (recorded in `_DECISIONS.md`):** if a future feature proposes offline *co-editing of shared mutable state* (e.g., two people revising the same draft narrative while both offline), that proposal reopens the sync-strategy question and must be flagged against `_DECISIONS.md` — it is the one shape this architecture deliberately does not support. The default answer is "radio it in."

**Stale context is accepted, not fixed.** An offline officer may append a note without having seen entries added concurrently by others. That is radio-era operational reality: the record honestly reflects what each author knew and when. No merge/reconciliation complexity exists to "fix" this, deliberately.

## Actors & Roles

- **Guard / Officer (mobile)** — primary offline user; captures field work (DAR entries, checkpoint scans, incident creation/updates, citations) with or without connectivity.
- **Dispatcher** — the primary disconnected-workflow actor: performs actions on behalf of radio-relayed officers using the platform's standard on-behalf-of capability. Not a consumer of this feature's sync mechanics — a Dispatcher is always connected.
- **Fixed Terminal User** — uses a site-installed terminal with the same capture capability for local coverage gaps.
- **IT / Systems Administrator** — configures Network Profiles, triggers remote wipe, sets local data expiry windows.
- **Sync Engine** (system) — background process on each client managing the local outbox and replay.

## User Stories

- As a **Guard** patrolling a dead-zone area, I want to scan checkpoints and log DAR entries normally, so my shift record is complete without needing connectivity.
- As a **Guard**, I want my offline-created incident to sync automatically and get its official incident number the moment I'm back in coverage, so I don't have to remember to manually sync.
- As a **Guard** posted where devices aren't permitted, I want to radio my activity to Dispatch and have it logged on my behalf, so the system record is complete even though I never touched a device.
- As an **Officer** adding a note to an incident while offline, I want my note to land on the incident's timeline in the right chronological position when I reconnect, alongside notes other officers added while I was out.
- As an **IT Admin**, I want large photo/video attachments to defer syncing until the device is on WiFi, so field officers on cellular data don't burn their data plan or delay sync of critical text records.
- As an **IT Admin**, I want to remote-wipe a lost device's local cache, so a stolen tablet doesn't expose cached data.

## Functional Requirements

### Local storage & data scope
1. Mobile app and fixed client terminals support offline capture with local storage (SQLite/IndexedDB).
2. Each client caches a small, role-relevant **read-only working set**: the user's current shift assignment, active post orders, assigned checkpoints/routes (including checkpoint inspection instructions), open incidents assigned to or created by the user, recent own DAR entries, and the reference data needed for those workflows — not a full database mirror, and sized for shift-length offline windows, not extended operation.
3. Cached reads are accepted as possibly stale; staleness is surfaced by age (same posture as Patrol Management's `last_known_location`), never silently presented as current.
4. Cached data is encrypted at rest on-device, keyed to the authenticated session.
5. Cached data older than a configurable rolling window is automatically purged from the device even absent an explicit wipe. Because offline windows are hours rather than days, retention defaults are short.
6. An admin can trigger a remote wipe targeting a specific device's local cache; the wipe executes on that device's next network contact.

### Offline-capable actions (the capture subset)
7. **Class 1 — create offline**: DAR entries, checkpoint/tour scans, patrol findings and checklist completion, courtesy patrol logs, citation/ticket issuance, incident creation with updates and photo/evidence attachment capture. All receive client UUIDs and are freely editable locally until first sync.
8. **Class 2 — append offline**: timeline notes/updates on existing Activities, shift passdown read-acknowledgment, and state-transition events on execution records the user is the sole assigned actor of (e.g., their own Patrol's start/complete). Hard gates that *consume* these appends (e.g., passdown acknowledgment gating shift start) are evaluated online — clock-in occurs at a connected post.
9. **Class 3 — connectivity-required**: dispatch assignment/acceptance, real-time dispatcher chat, live camera stream viewing, remote gate/barrier control, and any mutation of a shared server-side record's own fields (status, severity, assignment, etc.). Blocked with a clear "requires connectivity" state — never silently queued — with the standing operational alternative being radio relay to Dispatch. A panic-button trigger is captured locally instantly and transmitted the moment connectivity returns, but its EOC routing cannot occur while offline.

### Outbox mechanics
10. The client maintains a durable outbox of offline writes that survives app restarts and crashes.
11. Each outbox entry carries a **per-device monotonic sequence number**, so one officer's scan → note → escalation replays in the order it happened. Single-writer per device means a simple counter suffices — no vector clocks or causality machinery.
12. Every entry records **two timestamps**: client-claimed time and (on arrival) server-received time. Timelines display by client-claimed time with server-received time as tiebreaker; both are always retained, because field-device clocks drift.
13. Replay is **idempotent, keyed by client UUID**: a retry after a partial flush cannot double-insert. Sync completing, failing, or being interrupted mid-batch never corrupts the outbox or duplicates records.
14. **Batch-internal references resolve on sync**: an offline note referencing an offline-created scan (both still on client UUIDs) syncs correctly in one batch; the server accepts and resolves UUID references between rows arriving together.
15. On connectivity detection, sync starts automatically in the background; the user can also force a manual sync. Failed attempts retry with exponential backoff; exhausted retries alert via the Notifications Engine.
16. Sync is tiered by priority: small text/structured records sync first on any connection quality; large attachments (photos, video, audio) defer until the active Network Profile (Settings & Preferences) permits — e.g., WiFi-only — configurable per tenant/site. Deferred items remain visibly "pending" to the user until synced.

### ID generation
17. Records created offline receive a client-generated UUID immediately, making them fully usable and cross-referenceable offline.
18. On sync, the server centrally allocates the record's human-readable sequential number (e.g., citation #, incident #), preventing numbering collisions across devices.

## Data Model / Fields

**Outbox Entry** (client-local)
- outbox_id (client UUID)
- device_sequence (per-device monotonic counter)
- record_type, record_client_id (UUID)
- write_class (create | append)
- payload
- client_claimed_at (client clock), priority_tier (text/structured vs attachment)
- sync_status (pending, syncing, synced, failed)
- retry_count, last_attempt_at

**Synced Record** (server)
- server_id (authoritative), client_id (UUID, for offline-origin traceability)
- sequential_display_number (nullable until server-assigned, for record types that need one)
- client_claimed_at, server_received_at (both always retained)
- source (online | offline_sync | on_behalf_of)

**Device Cache Policy**
- tenant_id / site_id
- local_retention_window (rolling purge period, short by default)
- network_profile_ref (FK → Settings & Preferences Network Profile)

**Remote Wipe Command**
- device_id, issued_by, issued_at, executed_at (nullable), status

## States & Transitions

**Outbox Entry:** `pending` → `syncing` → `synced` (terminal success) | `failed` (retries with backoff; remains `failed` with alert if retries exhausted). There is no conflict state — conflicts are impossible under the offline contract.

**Device Cache:** `active` → `wipe-pending` (command issued, awaiting device contact) → `wiped`. Independently, cached entries individually transition `cached` → `expired-purged` per retention window.

## Integrations

- **Authentication & Authorization**: offline session token validity and refresh-on-reconnect behavior is owned by that feature; this feature consumes it to gate what remains accessible while offline. Offline windows being shift-length (not multi-day) should inform that feature's offline-token tolerance.
- **Structured Logging & Audit Trails**: every sync operation and remote wipe is an audit-tier event; offline-originated audit entries queue and sync using the same outbox mechanics.
- **Notifications Engine**: alerts for exhausted retry failures and remote wipe completion.
- **Settings & Preferences**: Network Profiles drive attachment sync deferral rules.
- **Master Records**: reference data cached locally (person/vehicle/location lookups relevant to the user's working set) originates here. Offline capture never creates new Person/Vehicle entities directly — an unknown subject is captured as narrative text plus photos and formalized into a registry entity once online (by the officer, or by a Dispatcher on relay).
- **Security Operations features** (DAR, Guard Tour, Incident Reporting, Courtesy Patrol, Tickets/Citations): primary consumers of the capture subset (#7–8) — all their offline writes are Class 1/Class 2 by design.
- **Dispatch/CAD**: defines the connectivity-required boundary (#9) and owns the on-behalf-of workflow that serves as the platform's primary disconnected path.

## Permissions

| Action | Guard/Officer | Supervisor | IT Admin | Platform Super Admin |
|---|---|---|---|---|
| Perform offline capture (Class 1/2) | ✅ (own device) | ✅ | ✅ | ✅ |
| Force manual sync | ✅ (own device) | ✅ | ✅ | ✅ |
| Configure local retention window | ❌ | ❌ | ✅ (own tenant) | ✅ |
| Trigger remote wipe | ❌ | ❌ | ✅ (own tenant) | ✅ |
| Configure Network Profile sync tiering | ❌ | ❌ | ✅ (own tenant) | ✅ |

## Non-Functional / Constraints

- Offline windows are expected to be shift-length at most; cache scope, retention defaults, and security exposure analysis are all sized to that assumption. Extended-offline fixed-terminal operation is out of scope — an air-gapped facility runs a self-hosted local instance and its clients are online to it (deployment-model concern, per the PDD).
- Local storage encryption and remote wipe are mandatory regardless of tenant isolation tier — treated as a security-critical control, not an optional feature.
- The outbox must be resilient to abrupt connectivity loss mid-sync: partial sync must not corrupt the local queue or duplicate records on retry (idempotent operations keyed by client UUID).
- Idempotent-replay and ordering behavior must be extensively tested (kill-mid-sync, replay-after-partial-flush, out-of-order arrival) — this replaces the prior design's CRDT/LWW edge-case testing burden with a much smaller, boring, verifiable surface.
- WCAG 2.1 / Section 508 accessible offline/sync-status indicators (e.g., pending-sync badge) in the mobile UI.
- Attachment deferral must not silently drop data — deferred items remain visibly "pending" to the user until synced.

## Acceptance Criteria

- [ ] A guard can complete a full patrol (checkpoint scans, DAR entries, a courtesy patrol checklist) fully offline, then have all of it sync correctly and **in per-device order** once reconnected.
- [ ] Multiple officers — some offline, some online — adding notes to the same incident all have every note preserved as its own timeline row, interleaved for display by client-claimed time with server-received tiebreak; no note is lost or overwritten under any sync ordering.
- [ ] A record created offline is fully usable (referenceable, attachable, locally editable) before sync, and receives its official sequential number immediately upon successful sync; after first sync, further offline edits to it are no longer possible — only appends.
- [ ] An offline note referencing an offline-created record (both on client UUIDs) syncs correctly in a single batch with the reference resolved.
- [ ] Killing connectivity mid-sync and reconnecting does not duplicate or corrupt any record.
- [ ] Attempting a connectivity-required action (accepting a dispatch, changing an incident's status) while offline is blocked with a clear "requires connectivity" state, not silently queued.
- [ ] A large photo attachment captured offline on cellular data queues and does not sync until the device is on WiFi (per Network Profile), while the accompanying incident text syncs immediately on any connection.
- [ ] An admin-issued remote wipe clears a target device's local cache on its next network contact.
- [ ] Cached data older than the configured retention window is purged from a device even with no wipe command issued.
- [ ] A panic-button trigger fired while offline is captured immediately and transmitted to EOC the moment connectivity returns.

## Open Questions

- Exact local retention window default (hours vs. a small number of days) — to be set during technical spec, sized to shift-length offline windows.
- **Lone-officer commercial sites with no dispatcher**: contract accounts running a single officer with no connected Dispatcher have no on-behalf-of path. Does the cached read-only working set need to grow (still strictly within the append-only contract — a bigger *capture* subset, never mutation) so a solo officer is self-sufficient for a full shift? Needs a market/persona decision before the mobile working set is finalized.
- Specific UX for the "requires connectivity" blocked state — e.g., whether it offers a one-tap "log that I radioed this to Dispatch" append.
- Interaction with future FedRAMP/air-gapped SaaS tiers regarding remote wipe reachability when a device may never re-contact a cloud control plane — deferred to deployment-model technical spec.
