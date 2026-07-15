# Pre-Registration Portal

## Overview

Pre-Registration Portal opens Module 4 (Access Control) and is the platform's first "PIAM Lite" feature — see `_DECISIONS.md`'s Module 4 framing entry for the full strategic rationale. It covers the **invitation-and-approval stage only**: a Host, Front Desk/Security, or the visitor themselves records an expected visit ahead of time, it's screened against the platform's watchlist mechanism, and (per tenant policy) approved. Actual physical arrival — kiosk check-in, badge printing, PACS credential issuance — is entirely the next doc's job, **Visitor Kiosk App**, which this doc explicitly does not build.

Structurally, this is a genuine two-tier plan/execution split, per explicit user direction: **Pre-Registration** is the plan (an Activity extension in its own right, since — like Route Assignment before it — it has a real lifecycle, not just a bounding time window: submitted, screened, approved, rejected, expired); **Visit** (defined by Visitor Kiosk App, forward-referenced only here) is the execution, one row per actual check-in, referencing back to the Pre-Registration that authorized it. A single approved Pre-Registration can authorize more than one Visit occurrence over a date-range window (e.g., a contractor visiting daily for a week) — check-in creates the Visit each time; the Pre-Registration itself isn't consumed by a single use.

This doc also introduces the **PIAM Adaptor Registration** — the base of the new adaptor family Module 4's framing decision calls for (HID SAFE first, other PIAM vendors pluggable later) — since visitor sync is its first concrete consumer. Access Credential Management and BOLO & Trespass Alerts will extend this same registration with their own sync capabilities when they're specified.

## Actors & Roles

- **Host** — any Employee expecting a visitor; self-service pre-registers their own visitor, sees only their own invitees by default.
- **Front Desk / Security Guard** — creates a pre-registration on behalf of a host who called ahead; sees the full site-wide pending/approved queue, not just their own.
- **Visitor** — may self-register via a host-issued link, supplying their own identifying details ahead of arrival.
- **Supervisor+** — resolves a Watchlist Match Alert; the only role that can escalate a match into a real, step-up-authenticated BOLO Flag (Entity Registry Core's existing mechanism, unmodified).
- **Site/Tenant Admin** — configures the Pre-Registration Approval Policy, Pre-Arrival Requirement Definitions, and the PIAM Adaptor Registration (HID SAFE connection).

## User Stories

- As a **Host**, I want to pre-register an expected visitor with their name, company, and purpose ahead of time, so front desk and security already know who's coming.
- As a **Host**, I want to send my visitor a self-service link so they can fill in their own details, instead of me typing everything myself.
- As a **Front Desk Guard**, I want to create a pre-registration for a host who called ahead but hasn't done it themselves, and see every pending/approved pre-registration for my site in one queue.
- As a **Supervisor**, I want to be alerted immediately if an incoming pre-registered visitor matches an active BOLO/Trespass flag, rather than finding out when they show up at the door.
- As a **Tenant Admin**, I want to require a signed NDA before a contractor's pre-registration is approved, and have that signature become a real, retrievable document.
- As a **Tenant Admin at a site running HID SAFE**, I want pre-registrations created here to sync out to HID SAFE so it can prepare the actual badge/PACS provisioning it already owns, and I want HID SAFE's own watchlist result to be the one that counts.
- As a **Host**, I want a recurring contractor's single pre-registration to cover their whole week on-site, instead of re-registering them every single day.

## Functional Requirements

### Pre-Registration as an Activity extension
1. **Pre-Registration** registers as an Activity extension (fulfilling Activity Registry's registration discipline), because — like Route Assignment before it — it has a real lifecycle and isn't merely a bounding time window. It is deliberately **not** the Visit itself: Visitor Kiosk App defines Visit as a separate Activity extension, one row per actual check-in, carrying `source_pre_registration_ref` back to this record. A Pre-Registration is never directly promoted into a Visit by this doc's own mechanics — check-in (Visitor Kiosk App) is what creates one, and only against an `approved` Pre-Registration whose window covers the current time.
2. The **visitor** and **host** are both ordinary Activity Registry Participant associations (role `visitor`, role `host`) — no bespoke reference fields, reusing the platform's existing generic mechanism.
3. The visitor **always resolves to a real Person entity carrying (or gaining) a Visitor extension** — reusing Person Registry's existing dedup search, inline-creating a new Person+Visitor if no match — the same "durable, dedup-worthy relationship" treatment as Agency Handoff Log and ICS Role Assignment, not Call's free-text-caller pattern, since watchlist screening and repeat-visitor recognition both depend on a real identity. A Visitor self-service submission (#5c) still resolves through this same dedup path once received.
4. A Pre-Registration carries a window (`valid_from`/`valid_until`) rather than a single timestamp — a single-day visit sets both to the same date; a recurring/multi-day window (e.g., a week-long contractor engagement) authorizes any number of Visits within it, each created independently at its own check-in by Visitor Kiosk App.

### Initiation
5. Four initiation channels, all tenant-enabled independently (a Settings & Preferences channel catalog, matching the platform's established multi-channel-catalog pattern from self-service password reset):
   - **5a. Host self-service** — any Employee with the permission creates a pre-registration for their own expected visitor.
   - **5b. Front Desk/Security on-behalf** — a Guard creates it for a host who requested it another way (phone, in person), reusing the platform's established on-behalf-of posture.
   - **5c. Visitor self-service link** — a host-issued, single-use link lets the visitor supply their own details (name, company, photo) ahead of arrival; the submission still requires host confirmation before it's treated as a real pre-registration (a raw external submission is never auto-trusted as complete).
   - **5d. Kiosk walk-in** *(retrofit — Visitor Kiosk App)* — a visitor with no existing Pre-Registration self-registers at a walk-in-enabled kiosk; runs through this doc's screening/approval mechanism identically to every other channel, with an optional stricter walk-in-specific required-approver override (`walk_in_required_approvers` on Pre-Registration Approval Policy).

### QR pass
5e. Reaching `approved` generates a **QR pass token** and emails it to the visitor — the token stays valid for the record's entire window but is consumed as one check-in per calendar day (Visitor Kiosk App owns the scan/consumption mechanics; this doc only owns issuance).

### Watchlist screening
6. Screening runs automatically at submission — never deferred to arrival. It checks the resolved visitor Person against Entity Registry Core's existing BOLO Flag mechanism, unmodified.
7. **When the PIAM Adaptor (HID SAFE) is configured and its `watchlist_authority` is set to external, HID SAFE's own watchlist match result is authoritative** instead of Sentinel Suite's native BOLO check — per explicit user direction, since a tenant's real watchlist-of-record (e.g., a corporate-wide exclusion list) may already live there. When no adaptor is configured, or `watchlist_authority` is `sentinel_native`, the platform's own BOLO Flag check is authoritative.
8. **A watchlist match — from either source — never directly creates a governed BOLO Flag.** Entity Registry Core requires step-up authentication from a human to create one (#18 in that doc); an automated match can't satisfy that by construction. Instead, a match raises a lightweight, feature-local **Watchlist Match Alert** and holds the Pre-Registration at `flagged_for_review` — a Supervisor+ must explicitly dismiss it (false match, no flag created) or escalate it into a real BOLO Flag through the standard step-up-gated creation flow. The same "AI/automation proposes, a human confirms" discipline already established for AI-generated content, applied here to an automated watchlist hit.
9. A Pre-Registration cannot reach `approved` while a Watchlist Match Alert is open and unresolved.

### Approval
10. A **Pre-Registration Approval Policy** (Settings & Preferences Definition, tenant/site-scoped) declares required approver(s): none (auto-approve on clean screening), host, security, or both. The resolved policy is snapshotted onto the Pre-Registration at submission time so a later policy change never retroactively alters an in-flight request.
11. Where both are required, host and security approvals are independent — either can be recorded first, and the Pre-Registration only reaches `approved` once both are in.

### Pre-arrival requirements
12. A tenant/site can configure one or more **Pre-Arrival Requirement Definitions** (Settings & Preferences) — NDA e-signature, ID/photo upload, or a custom step — that must complete before a Pre-Registration can reach `approved`, layered on top of (not instead of) the approval gate. An NDA e-signature becomes a real Document via Document Registry, authored by the visitor (`DocumentAuthorAssociation`), not a free-floating file.

### PIAM Adaptor sync (HID SAFE, first-class)
13. A new **PIAM Adaptor Registration** (base of Module 4's adaptor family, per the platform's established provider-adaptor pattern) declares `adaptor_type` (`hid_safe` at launch, `none` default), connection config, and per-capability sync flags — this doc turns on `visitor_sync` and, when `watchlist_authority = external`, the watchlist read path (#7). Access Credential Management and BOLO & Trespass Alerts extend this same registration with their own capabilities when specified; this doc does not speculate their shape.
14. When `visitor_sync` is enabled, a Pre-Registration that reaches `approved` pushes out to HID SAFE so its own badge/PACS provisioning workflow can prepare ahead of arrival. **Sentinel Suite stays authoritative for the Pre-Registration record's own core fields (visitor/host/purpose/window)** — HID SAFE receives a push of the finalized record, it is not a co-editable shared record; only watchlist authority (#7) is a genuine two-way authority split at this stage. The full general conflict/authority-of-record rule for a truly bidirectionally-editable record is explicitly deferred to Access Credential Management, where credential state can plausibly change on either side.
15. If the adaptor is configured but unreachable at submission time, screening falls back to the Sentinel-native BOLO check (#7's fallback path) rather than blocking submission — the Pre-Registration is flagged `external_screening_unavailable` for visibility, never silently treated as clean.

## Data Model / Fields

**Pre-Registration** (Activity extension; entity_id is the shared PK, FK → Activity)
- purpose (free text; may default from the visitor's own Visitor.default_visit_purpose)
- site_ref (destination Location)
- valid_from, valid_until
- initiation_channel (host_self_service, front_desk_on_behalf, visitor_self_service_link)
- status (draft, submitted, flagged_for_review, pending_approval, approved, rejected, expired, cancelled)
- required_approvers (snapshotted from Pre-Registration Approval Policy at submission: none, host, security, both)
- host_approved_by/at (nullable), security_approved_by/at (nullable)
- external_screening_unavailable (bool, set only per #15's fallback path)
- external_piam_ref (nullable — HID SAFE's own record identifier, once synced)
- initiation_channel gains `kiosk_walk_in` *(retrofit — Visitor Kiosk App, #5d)*
- qr_pass_token (generated on reaching `approved`, emailed to the visitor — retrofit, #5e)
- explicit_backup_host_ref (nullable, FK → Person/Employee — retrofit, Host Arrival Notifications: overrides the host's standing default backup for this specific invitation)

**Watchlist Match Alert** (feature-local, deliberately not a BOLO Flag)
- alert_id, pre_registration_ref, source (sentinel_native, hid_safe), match_details
- status (open, dismissed, escalated_to_bolo)
- reviewed_by (nullable), reviewed_at (nullable), resulting_bolo_flag_ref (nullable, set only on escalation)

**Pre-Registration Approval Policy** (Settings & Preferences Definition)
- tenant_id/site_id, required_approvers (none, host, security, both)
- walk_in_required_approvers (nullable — falls back to required_approvers; retrofit, #5d)

**Pre-Arrival Requirement Definition** (Settings & Preferences Definition)
- requirement_id, type (nda_signature, id_upload, photo_capture, custom), required (bool), document_template_ref (nullable)

**PIAM Adaptor Registration** (new — base of Module 4's adaptor family)
- adaptor_id, tenant_id, adaptor_type (hid_safe, none), enabled (bool)
- sync_capabilities{} (visitor_sync: bool — this doc; credential_sync/watchlist_sync — reserved for Access Credential Management/BOLO & Trespass Alerts to declare)
- watchlist_authority (sentinel_native, external)
- connection_config (adaptor-specific, opaque — technical-spec)

## States & Transitions

**Pre-Registration:** `draft` (visitor self-service link issued, not yet submitted) → `submitted` → `flagged_for_review` (open Watchlist Match Alert; blocks further progress until resolved) → `pending_approval` (required_approvers ≠ none) → `approved` (visit-eligible for the remainder of its window) → `expired` (valid_until passed). `submitted`/`pending_approval` may also transition to `rejected` (an approver denies) or `cancelled` (host/initiator withdraws) at any point before `approved`. A resolved Watchlist Match Alert returns the Pre-Registration to `submitted`'s next step (`pending_approval` or straight to `approved` if no approvers required).

**Watchlist Match Alert:** `open` → `dismissed` (Supervisor+ judges it a false match, no BOLO Flag created) | `escalated_to_bolo` (Supervisor+ creates a real BOLO Flag through Entity Registry Core's standard step-up flow).

## Integrations

- **Person Registry**: visitor and host both resolve to real Person entities (Visitor/Employee extensions respectively); visitor resolution reuses Person Registry's existing dedup search and inline-creation.
- **Activity Registry**: Pre-Registration is a new Activity extension; visitor/host both use the existing generic Participant association mechanism.
- **Entity Registry Core**: BOLO Flag is the terminal governed record a Watchlist Match Alert may escalate into, unmodified — step-up auth, justification, expiration, all inherited as-is.
- **Document Registry**: an NDA e-signature (or other document-producing pre-arrival requirement) becomes a real Document, authored by the visitor.
- **Settings & Preferences**: owns Pre-Registration Approval Policy, Pre-Arrival Requirement Definitions, and the initiation-channel catalog (#5).
- **Notifications Engine**: submission triggers an approval-request notification to the required approver(s); approval/rejection notifies the initiator. This is distinct from — and does not replace — **Host Arrival Notifications** (next doc), which fires on actual check-in, not submission.
- **Visitor Kiosk App** *(retrofit — built after this doc)*: consumes an `approved` Pre-Registration at check-in to create the Visit Activity this doc deliberately does not build; contributes the `kiosk_walk_in` initiation channel and consumes the QR pass token this doc issues.
- **Tenant Management (Client Engagement)**: a visitor pre-registering for a Contractor-staffed Client site follows the existing rule unmodified — the record's `tenant_id` is the Client's, regardless of which Contractor employee acted as host.
- **PIAM Adaptor (HID SAFE)**: this doc's first concrete consumer of the new PIAM Adaptor Registration — pushes finalized/approved records out (#14), optionally supplies authoritative watchlist results in (#7).
- **Command/Action Bus**: Approve/Reject/Cancel Pre-Registration, and Dismiss/Escalate Watchlist Match Alert, all register as actions; approving is blocked at the platform level while a Watchlist Match Alert is open (#9), not just a UI-level discouragement.

## Permissions

| Action | Site/Tenant Admin | Host | Front Desk/Security | Supervisor+ |
|---|---|---|---|---|
| Create own pre-registration | ✅ | ✅ | ✅ | ✅ |
| Create on behalf of a host | ✅ | ❌ | ✅ | ✅ |
| View own pre-registrations only | ✅ | ✅ (default) | — | — |
| View all site pre-registrations | ✅ | ❌ | ✅ | ✅ |
| Approve/reject (per resolved policy role) | ✅ | ✅ (if host required) | ✅ (if security required) | ✅ |
| Dismiss/escalate a Watchlist Match Alert | ❌ | ❌ | ❌ | ✅, escalation step-up required |
| Configure Approval Policy / Pre-Arrival Requirements / PIAM Adaptor | ✅ | ❌ | ❌ | ❌ |

## Non-Functional / Constraints

- Watchlist screening must complete (from whichever source is authoritative) before a Pre-Registration can reach `approved` — never silently skipped or bypassed by adaptor downtime; downtime degrades to the native fallback (#15), it never waives screening entirely.
- PIAM Adaptor sync is not held to Real-Time Delivery's ≤2s live-console latency target — this is a request/response or async sync operation, not a live channel subscription; exact target is technical-spec-level.
- An NDA or other pre-arrival document follows Document Registry's and Blob/File Storage's existing hash/integrity and retention treatment — no new storage mechanism introduced here.
- A Watchlist Match Alert's escalation to a real BOLO Flag is audit-tier, inheriting Entity Registry Core's existing requirement wholesale; the alert's own open/dismissed/escalated transitions are audit-tier as well, given the liability weight of a screening decision.

## Acceptance Criteria

- [ ] A Host pre-registering a visitor for tomorrow only, with no approval policy configured, results in an auto-approved Pre-Registration once screening clears.
- [ ] A Front Desk Guard sees every site's pending/approved Pre-Registration, not just their own; a Host's default view shows only their own.
- [ ] A visitor self-service link submission requires host confirmation before the Pre-Registration is treated as submitted.
- [ ] A visitor matching an active BOLO Flag halts the Pre-Registration at `flagged_for_review` and raises a Watchlist Match Alert; the record cannot reach `approved` while the alert is open.
- [ ] Dismissing a Watchlist Match Alert as a false match creates no BOLO Flag; escalating one requires step-up authentication and produces a real, standard BOLO Flag on the visitor's Person record.
- [ ] With Pre-Registration Approval Policy set to `both`, the record only reaches `approved` after independent host and security approvals are both recorded.
- [ ] A configured NDA pre-arrival requirement blocks `approved` until a signed Document exists, authored by the visitor.
- [ ] A date-range Pre-Registration (e.g., a 5-day window) remains `approved` and visit-eligible across its whole window, not consumed by a single check-in.
- [ ] With a PIAM Adaptor configured and `watchlist_authority = external`, a screening decision reflects HID SAFE's match result, not Sentinel Suite's own BOLO check; with the adaptor unreachable at submission, the record is flagged `external_screening_unavailable` and screening falls back to the native BOLO check rather than being skipped.
- [ ] An approved Pre-Registration with `visitor_sync` enabled is confirmed pushed to the configured PIAM adaptor; a later edit to the Pre-Registration's core fields (before it expires) is confirmed to originate from Sentinel Suite, never accepted as an incoming edit from the adaptor.
- [ ] A Contractor-staffed Client Engagement site's Pre-Registration carries the Client's `tenant_id`, regardless of which Contractor employee hosted it.

## Open Questions

- Exact PIAM Adaptor Registration connection-config schema (auth mechanism, endpoint shape) — technical-spec, same posture as every other provider adaptor's config.
- Whether a multi-day Pre-Registration window ever needs its own per-day expected-vs-actual reconciliation (e.g., flagging that Tuesday's expected visit didn't happen) — not built now; a missed day currently leaves no record at all (no Visit is ever created for a day that didn't happen), which is an honest gap, not a silent one, but may warrant its own mechanism if a target customer's compliance posture needs it.
- Whether Watchlist Match Alert should ever support tenant-configured auto-escalation (skipping the manual dismiss/escalate decision) for a `mandatory` screening posture — no target customer need identified yet, not built here.
- Exact NDA re-signature policy when a document template changes mid-window for an already-approved recurring Pre-Registration — deferred to technical spec.
- Whether HID SAFE's own approval workflow (if a tenant also runs one there) should be able to drive Sentinel Suite's approval state, or whether approval stays Sentinel-native-only like the rest of the record's core fields (#14) — leaning native-only per #14's reasoning, but not conclusively settled; flagged for Access Credential Management to revisit if a real bidirectional-approval need surfaces there.
