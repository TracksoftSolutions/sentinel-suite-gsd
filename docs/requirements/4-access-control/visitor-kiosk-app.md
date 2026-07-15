# Visitor Kiosk App

## Overview

Visitor Kiosk App builds the **execution** half of the plan/execution split Pre-Registration Portal established: **Visit**, the Activity extension for one actual check-in/check-out occurrence, created only here — Pre-Registration Portal deliberately deferred it. It also closes MODULES.md's "Email QR Access Passes" bullet (a light retrofit to Pre-Registration Portal, see Integrations) and reuses that doc's watchlist screening, approval, and pre-arrival-requirement machinery wholesale rather than rebuilding any of it for the walk-in case.

A kiosk is the platform's first **write-capable, unauthenticated physical device** — a materially different shape than Command Center Wallboard View's Display Device, which is read-only signage with no human session. A **Kiosk Device** gets its own registration, mirroring Display Device's no-login/admin-provisioned-pairing-token posture but scoped to a bounded set of visitor check-in/out actions and a declared peripheral set (camera, signature pad, ID scanner, barcode/QR scanner, badge printer) instead of a viewing scope.

## Actors & Roles

- **Visitor** — the non-human-session actor at the kiosk: scans a QR pass, looks themselves up by name, or (if enabled) self-registers as a walk-in.
- **Kiosk Device** (non-human actor) — the provisioned touchscreen itself: no login, acts within its own scoped write capability and declared peripherals.
- **Front Desk / Security Guard** — assists a visitor the kiosk can't resolve (ambiguous name match, missing peripheral, unresolved Watchlist Match Alert), performs manual check-in/check-out for sites with no kiosk.
- **Site/Tenant Admin** — provisions/revokes Kiosk Devices, configures peripherals, walk-in enablement, and the Overdue Visit Policy.
- **Supervisor+** — same Watchlist Match Alert dismiss/escalate authority as Pre-Registration Portal; a kiosk can never resolve one itself.

## User Stories

- As a **Visitor**, I want to scan the QR pass I was emailed and check myself in without waiting for staff.
- As a **Visitor** with no pre-registration, I want to be able to register myself at the kiosk if the site allows walk-ins, rather than being turned away or making a host come to the lobby.
- As a **Visitor**, I want to sign my NDA on the kiosk's signature pad if I didn't do it in advance, and have my badge print right after.
- As a **Security Guard**, I want to see a live roster of everyone currently checked in at my site, without cross-referencing separate kiosk logs.
- As a **Site Admin**, I want to configure whether an overdue visitor just triggers an alert, gets auto-checked-out after a longer threshold, or both.
- As a **Visitor**, I want to check myself out at the kiosk when I leave, the same way I checked in.

## Functional Requirements

### Kiosk Device
1. A Site/Tenant Admin provisions a **Kiosk Device**: a non-interactive-login identity for one physical touchscreen, paired via a one-time admin-generated token — the same no-user-login credential posture as Display Device, but write-scoped rather than viewing-scoped.
2. A Kiosk Device declares its **peripherals** (`has_camera`, `has_signature_pad`, `has_id_scanner`, `has_barcode_scanner`, `has_badge_printer`) — the UI adapts per device; a missing peripheral simply skips or reroutes that step (e.g., no barcode scanner means QR check-in is unavailable at that kiosk, name-lookup/manual only) rather than failing.
3. A Kiosk Device's **write scope** is bounded to visitor check-in/check-out actions at its own site, plus whether **walk-in registration is enabled** at that specific device (a Site Admin may enable it at a lightly-staffed lobby kiosk and disable it at an unattended loading-dock kiosk, for instance).
4. Revoking a Kiosk Device immediately disables it and invalidates its pairing token — same posture as revoking a Display Device.

### Check-in resolution
5. Three resolution modes, each contingent on the device's declared peripherals: **QR scan** (fastest, unambiguous — decodes the Pre-Registration's QR pass, see #11), **name lookup** (searches today's `approved` Pre-Registrations at this site by name; an ambiguous match — more than one plausible result — prompts the visitor/kiosk to disambiguate rather than guessing, the same "never let the system silently disambiguate a real-world decision" discipline Guard Tour established), and **manual entry** (front desk types details in on the visitor's behalf, or feeds a walk-in registration — #6).
6. **Walk-in support**: a visitor with no existing Pre-Registration, at a kiosk where walk-ins are enabled, self-registers on the spot — this creates a real Pre-Registration through a new `kiosk_walk_in` initiation channel *(retrofit to Pre-Registration Portal's `initiation_channel` enum)*, running through that doc's **exact same** watchlist screening and approval mechanism, no parallel path. A resulting Watchlist Match Alert halts the kiosk at a "please wait, security has been notified" holding screen — the kiosk itself can never dismiss or escalate one, same as any other channel. A walk-in Pre-Registration only proceeds to check-in (#8) once it reaches `approved`.
6a. A tenant may optionally set a **stricter walk-in-specific approval requirement** (`walk_in_required_approvers`, nullable on Pre-Registration Approval Policy, falls back to the ordinary `required_approvers` when unset) — walk-ins are inherently less vetted than a hosted invitation, and a tenant may reasonably want mandatory security approval for a walk-in even when a hosted pre-registration auto-approves.

### Pre-arrival requirements at the kiosk
7. Any Pre-Arrival Requirement Definition not already satisfied in advance (an NDA not yet signed, no photo on file) can be completed **at the kiosk itself** if the device has the needed peripheral (`has_signature_pad` for a signature, `has_camera` for a photo) — the same Document Registry/`extended_fields` mechanism Pre-Registration Portal already established, executed at a different point in time and place, not a second mechanism. A kiosk lacking a required peripheral routes the visitor to staff assistance rather than silently skipping the requirement.
8. A photo captured at the kiosk updates the visitor's canonical Person `photo` field — resolving Person Registry's own forward reference that "badge printing and kiosk display (Access Control) consume it."

### QR pass *(retrofit to Pre-Registration Portal)*
9. Pre-Registration Portal is retrofitted: reaching `approved` generates a **QR pass token** and emails it to the visitor — closing MODULES.md's "Email QR Access Passes" bullet, which belongs at approval time (Pre-Registration Portal), not kiosk time. The token remains valid for the Pre-Registration's entire window (a multi-day window's QR pass works for every day in it, not single-use-overall) but is consumed as **one check-in per calendar day** — scanning it a second time the same day after an active Visit already exists is treated as a duplicate-scan no-op, not a second Visit.

### Visit lifecycle
10. **Visit** registers as a new Activity extension — the execution record Pre-Registration Portal deliberately deferred — created only at successful check-in (#5/#6), never in advance. It carries `source_pre_registration_ref` (set even for a walk-in, whose Pre-Registration was created moments earlier) and inherits `escort_required` from the visitor's category/tenant default (or an explicit override recorded at check-in).
11. Checking in resolves a badge template render (name, photo, host, expiration/date, escort-required indicator) and, on a device with `has_badge_printer`, prints it directly. **The printed badge is visual-only and grants no physical access by itself** — same integrate-don't-replace boundary as every other hardware-protocol touchpoint in the platform: a tenant needing the badge to actually open doors does so through the PIAM/PACS adaptor's own credential issuance (Access Credential Management, not yet specified), which this doc triggers a sync toward but does not perform itself.
12. Check-out follows the same three resolution modes as check-in (QR/name/manual) at any kiosk enabled for it, or a manual action by Front Desk/Security — `checkout_method` records which (`self_service_kiosk`, `security_manual`, `auto_system` — see #13).

### Overdue visits
13. A tenant-configurable **Overdue Visit Policy** (Settings & Preferences Definition) independently sets an **alert threshold** (registers a new Duration Watchdog instance — `(visit, status, checked_in)` → duration — feeding the existing Critical Event Escalation Policy, never blocking any action, per the platform's standing quota/compliance rule) and an **auto-checkout threshold** (system-transitions the Visit to `checked_out` with `checkout_method = auto_system` once exceeded) — either, both, or neither may be configured, and the auto-checkout threshold is typically set longer than the alert threshold so security gets a warning before the system acts. An auto-checked-out Visit is an honest, distinguishable record (`checkout_method = auto_system`), never indistinguishable from an observed departure — the same negative-outcome-gets-a-real-row discipline used throughout the platform.

### Live roster
14. Visit registers as a `card` Queue Role *(retrofit to Active Incident Queue's day-one type list)* — every currently `checked_in` Visit is visible on the platform's existing live queue/Kanban surfaces with zero new board mechanism, giving Security a live "who's in the building" roster for free.

## Data Model / Fields

**Kiosk Device**
- device_id, tenant_id, site_ref
- peripherals{} (has_camera, has_signature_pad, has_id_scanner, has_barcode_scanner, has_badge_printer)
- walk_in_enabled (bool)
- status (provisioned, active, revoked)

**Visit** (Activity extension; entity_id is the shared PK, FK → Activity)
- source_pre_registration_ref (FK → Pre-Registration)
- checked_in_at, checked_in_via (qr_scan, name_lookup, manual_entry), checked_in_device_ref (nullable, FK → Kiosk Device — null for a front-desk manual check-in with no kiosk)
- checked_out_at (nullable), checkout_method (nullable — self_service_kiosk, security_manual, auto_system)
- escort_required (bool)
- badge_printed_at (nullable)
- status (checked_in, checked_out)

**Pre-Registration** *(retrofit — Pre-Registration Portal)*
- initiation_channel gains `kiosk_walk_in`
- qr_pass_token (generated and emailed on reaching `approved`)

**Pre-Registration Approval Policy** *(retrofit — Pre-Registration Portal)*
- walk_in_required_approvers (nullable — falls back to required_approvers)

**Overdue Visit Policy** (Settings & Preferences Definition)
- tenant_id/site_id, alert_threshold_hours (nullable), auto_checkout_threshold_hours (nullable)

## States & Transitions

**Kiosk Device:** `provisioned` (token issued, unpaired) → `active` (paired) → `revoked` — identical shape to Display Device.

**Visit:** created directly into `checked_in` (no pre-state — Pre-Registration owns everything before check-in) → `checked_out` (self-service, security-manual, or system-auto per the Overdue Visit Policy's auto-checkout threshold). No `cancelled`/`no_show` state exists on Visit itself — a Pre-Registration that's never checked into simply expires at its own layer (Pre-Registration Portal's `expired` state), since a Visit that never happened was never created here in the first place.

## Integrations

- **Pre-Registration Portal**: source of the `approved` Pre-Registration a check-in resolves against; retrofitted with `kiosk_walk_in` as a fourth initiation channel, the QR pass token, and `walk_in_required_approvers`. Watchlist screening, approval, and pre-arrival-requirement mechanisms are all consumed unmodified, never reimplemented here.
- **Person Registry**: a kiosk-captured photo updates the visitor's canonical Person `photo` field.
- **Document Registry**: an at-kiosk NDA signature becomes a real Document exactly as Pre-Registration Portal's in-advance path does.
- **Entity Registry Core**: a walk-in's Watchlist Match Alert follows Pre-Registration Portal's existing dismiss/escalate mechanism unmodified — the kiosk has no authority over it.
- **Active Incident Queue (CAD Console)** *(retrofit)*: Visit registers as a `card` Queue Role, giving a live checked-in roster with no new mechanism.
- **Active Call Alerts & Timers (Duration Watchdog)**: the Overdue Visit alert threshold is a new registered instance of the existing generalized mechanism.
- **Command Center Wallboard View (Display Device)**: precedent this doc's Kiosk Device deliberately mirrors for pairing/revocation posture, while diverging on write-vs-read scope.
- **PIAM Adaptor (HID SAFE)**: a checked-in Visit's badge/credential intent syncs out for real PACS credential issuance when the adaptor is configured; this doc never encodes access credentials natively (#11).
- **Structured Logging & Audit Trails**: every kiosk action (check-in, check-out, badge print, walk-in registration) is audit-tier, attributed to the Kiosk Device plus the resolved visitor Person — never a staff user, since none is authenticated at the device.

## Permissions

| Action | Site/Tenant Admin | Kiosk Device (self-service) | Front Desk/Security | Supervisor+ |
|---|---|---|---|---|
| Provision/revoke a Kiosk Device, configure peripherals/walk-in/Overdue Visit Policy | ✅ | ❌ | ❌ | ❌ |
| Check in/out via kiosk (visitor self-service) | — | ✅ (own device write scope only) | — | — |
| Manual check-in/out (no kiosk, or kiosk-assist) | ✅ | ❌ | ✅ | ✅ |
| View live checked-in roster | ✅ | ❌ | ✅ | ✅ |
| Dismiss/escalate a Watchlist Match Alert raised by a walk-in | ❌ | ❌ | ❌ | ✅, step-up required (inherited, unmodified) |

## Non-Functional / Constraints

- A Kiosk Device's pairing token follows the same single-use, admin-generated credential hygiene as Display Device's.
- Every kiosk-originated action is audit-tier regardless of the absence of a human session, attributed to the device plus the resolved visitor.
- Badge printing is a local peripheral print job, not held to any network SLA — a failed print simply produces no badge; front desk can reprint manually. No retry/queue infrastructure is specified here (technical-spec level).
- The Overdue Visit alert never blocks check-in/check-out anywhere else in the platform, consistent with the standing quota/compliance rule that no compliance-tracking mechanism may become an operational blocker.
- QR pass token security (expiry, rotation, anti-forgery) is a technical-spec concern; this doc only fixes its validity/consumption semantics (#9).

## Acceptance Criteria

- [ ] Scanning a valid QR pass at a kiosk with `has_barcode_scanner` creates a Visit immediately, without a name search step.
- [ ] A kiosk with no barcode scanner offers name lookup and manual entry only, never a broken/absent QR option.
- [ ] An ambiguous name-lookup match (more than one plausible Pre-Registration) prompts for disambiguation rather than guessing.
- [ ] At a walk-in-enabled kiosk, a visitor with no Pre-Registration can self-register, and the resulting record runs through the identical watchlist/approval path as a hosted Pre-Registration; at a walk-in-disabled kiosk, no such option is offered.
- [ ] A walk-in whose watchlist screening raises a Watchlist Match Alert is held at a holding screen, never silently checked in, and cannot be resolved by any kiosk action.
- [ ] An NDA signed at the kiosk (device with `has_signature_pad`) produces a real Document identical in shape to one signed in advance.
- [ ] A checked-in Visit shows up as a `card` on Active Incident Queue's existing live board with no additional configuration.
- [ ] With both an alert threshold and a longer auto-checkout threshold configured, an overdue Visit first triggers an escalation notification, then is auto-checked-out (marked `checkout_method = auto_system`) if it's still open past the second threshold.
- [ ] Reprinting a badge or re-scanning an already-checked-in visitor's QR pass the same day does not create a second Visit.
- [ ] Revoking a Kiosk Device immediately prevents further check-ins/check-outs at that device and invalidates its pairing token.

## Open Questions

- Exact ID-scanner data-extraction fidelity (driver's license/passport parsing for auto-fill) — technical-spec, adaptor-dependent on the specific scanner hardware.
- Exact QR token expiry/rotation and anti-forgery mechanics — technical-spec.
- Whether a multi-kiosk site ever needs kiosk-to-kiosk load/queue balancing (e.g., a busy lobby routing overflow to a second kiosk) — no target customer need identified; not built here.
- Whether Visit ever needs its own "visitor currently on-site but not at their expected department" wayfinding/notification beyond the badge's printed indicator — out of scope, flagged only.
