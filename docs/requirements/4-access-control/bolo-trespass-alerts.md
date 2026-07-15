# BOLO & Trespass Alerts

## Overview

Most of what MODULES.md asks for here is **already built** — worth stating plainly rather than re-specifying it. "BOLO Photo Logs" (file records of flagged individuals) is Entity Registry Core's generic **BOLO Flag** mechanism plus Person Registry's canonical Photo field and `supporting_document_ref`, both already fully governed (Supervisor+, mandatory justification/expiration, step-up authentication). This doc adds no new identity/flag mechanism — it closes the two real gaps MODULES.md's remaining bullets point at: **detection at the moment it actually matters** (a visitor arriving, not just registering) and **the alert behaving safely when a match fires** (silent, never tipping off the flagged individual). It also closes an explicitly-flagged open question left in Person Registry ("exact tenant-configurable default maximum expiration for BOLO/Trespass flags... to be set via Settings & Preferences during technical spec") with a real, jurisdiction-aware Trespass Notice default.

## Actors & Roles

- **Lobby Guard / Dispatcher** — receives the silent alarm on a match; may run an ad hoc watchlist check on anyone.
- **Security/Access Admin** — configures Trespass Notice Default Expiration Policy.
- **Supervisor+** — creates/clears BOLO Flags exactly as Entity Registry Core already governs, unmodified by this doc.

## Functional Requirements

### Re-screening at arrival, not just registration
1. *(Retrofit — Visitor Kiosk App)* Every check-in — kiosk or front-desk manual — re-runs the BOLO Flag check against the resolved visitor **at the moment of arrival**, regardless of what Pre-Registration Portal's original submission-time screening found. Closes a real staleness gap: a multi-day approved window can span the creation of a new BOLO Flag that didn't exist at submission time. A match at this point raises the identical Watchlist Match Alert mechanism Pre-Registration Portal already established, triggered a second time at a different point in the lifecycle.

### Silent alarm behavior
2. A match — from either submission-time or arrival-time screening — never surfaces to the flagged individual. A kiosk shows a neutral, generic message ("please see the front desk," never "BOLO match" or any denial language) while a **persistent Alarm State** (Real-Time Delivery's existing mechanism, the same one SOS Alert uses) immediately and unmissably notifies Dispatch and Lobby Guard consoles — matching MODULES.md's own "Instant Silent Alarm" framing exactly. This is a deliberate safety design, not a UX nicety: avoiding a public confrontation or tipping off a potentially dangerous individual before security is actually positioned to respond.
3. A confirmed match at arrival **auto-launches an Incident** via the platform's established launch-point pattern, context pre-filled (the matched Person, the site/Location, the triggering BOLO Flag reference) — the same "automatically log what already deserves a real record" instinct SOS Alert applied to a panic trigger.

### Ad hoc watchlist check
4. **Check Watchlist** registers as a Command/Action Bus action, available to any Guard — a first-class invocation of Person Registry's existing search/dedup and BOLO Flag visibility, for the case that doesn't funnel through Pre-Registration/kiosk at all (someone loitering, being questioned at a fixed post). No new screening mechanism — this exposes what already exists as a real, invokable action rather than requiring a Guard to know to manually search a person's profile.

### Trespass Notice
5. A BOLO Flag created in a trespass context carries `bolo_context = trespass_notice` (a light context marker, per Entity Registry Core's own established allowance for "consuming entity types add only their own context field") and a `supporting_document_ref` pointing to a real Trespass Notice Document.
6. A tenant-configurable **Trespass Notice Default Expiration Policy** (Settings & Preferences Definition) pre-fills BOLO Flag's already-mandatory expiration field when `bolo_context = trespass_notice` — a jurisdiction-aware default (e.g., a common one-year trespass-notice statute), never a hard override of the Supervisor's own ability to set a different date on that specific flag. Closes Person Registry's own explicitly-flagged open question.

## Data Model / Fields

**BOLO Flag** *(retrofit — Entity Registry Core; consumed via Person Registry's BOLO/Trespass Subject application)*
- bolo_context (nullable — trespass_notice, other)

**Trespass Notice Default Expiration Policy** (Settings & Preferences Definition)
- tenant_id/site_id, default_expiration_period

**Check Watchlist** (Command/Action Bus action — no new storage)

## States & Transitions

No new record lifecycle — BOLO Flag follows Entity Registry Core's existing `active` → `cleared`/`expired` shape unmodified; Watchlist Match Alert follows Pre-Registration Portal's existing `open` → `dismissed`/`escalated_to_bolo` shape unmodified, now triggered from a second point (arrival, not just submission).

## Integrations

- **Entity Registry Core**: BOLO Flag's existing governance (step-up, justification, expiration) is entirely unmodified and unmodifiable by this doc — only a `bolo_context` field is added, per that doc's own established allowance.
- **Person Registry**: source of BOLO/Trespass Subject, the canonical Photo, and the physical-description fields "BOLO Photo Logs" already relies on.
- **Visitor Kiosk App** *(retrofit)*: check-in gains arrival-time re-screening.
- **Pre-Registration Portal**: Watchlist Match Alert is reused unmodified, triggered a second time.
- **Real-Time Delivery & Server-Side Timers**: source of the persistent Alarm State the silent alarm uses, the same mechanism SOS Alert established.
- **Notifications Engine / Command Center Wallboard View**: Dispatch/Lobby Guard consoles and any subscribed wallboard both receive the alarm through the existing Live Update Channel.
- **Incident Reporting & Management**: target of the auto-launched Incident, via the platform's standard launch-point/context-seeding mechanism.
- **Document Registry**: Trespass Notice is a real Document, following the platform's existing hash/integrity/version treatment.
- **Settings & Preferences**: owns Trespass Notice Default Expiration Policy.
- **Command/Action Bus**: Check Watchlist registers as a new action.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Supervisor+ | Guard/Dispatcher |
|---|---|---|---|---|
| Configure Trespass Notice Default Expiration Policy | ✅ | ✅ | ❌ | ❌ |
| Create/clear a BOLO Flag *(inherited, unmodified)* | ✅ | ❌ (unless also granted) | ✅, step-up required | ❌ |
| Invoke Check Watchlist | ✅ | ✅ | ✅ | ✅ |
| Receive silent alarm notification | ✅ | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- A kiosk's neutral message on a match is enforced server-side (the client never receives the actual match reason) — never a client-side conditional that could be inspected or bypassed.
- The silent alarm's delivery latency inherits Real-Time Delivery's ≤2s safety-relevant target, the same baseline SOS Alert and every other persistent Alarm State consumer already meets.
- Check Watchlist's search never bypasses the requesting Guard's own RBAC/ABAC scope — it surfaces only what that Guard could already see through ordinary Person search.

## Acceptance Criteria

- [ ] A Pre-Registration approved before a BOLO Flag existed is caught by re-screening at actual kiosk check-in, not silently let through on the strength of its original clean submission-time result.
- [ ] A match at check-in shows the visitor a neutral message with no indication a match occurred, while Dispatch/Lobby Guard consoles receive an unmissable, persistent alarm within the platform's standard safety-relevant latency target.
- [ ] A confirmed match auto-creates an Incident with the matched Person, Location, and triggering BOLO Flag pre-filled.
- [ ] Invoking Check Watchlist on a person with an active BOLO Flag surfaces it immediately; invoking it on a person with none returns a clean result.
- [ ] Creating a BOLO Flag with `bolo_context = trespass_notice` pre-fills its expiration from the configured Trespass Notice Default Expiration Policy, while the creating Supervisor can still override the specific date before confirming.
- [ ] Every existing BOLO Flag acceptance criterion from Entity Registry Core and Person Registry (step-up gating, mandatory justification, audit-tier logging) is confirmed unaffected by this doc's additions.

## Open Questions

- Exact neutral-message copy/UX for a kiosk-detected match — a technical-spec/UX concern, not decided here.
- Whether Check Watchlist should itself be audit-tier logged even on a clean (no-match) result, for accountability against misuse — leaning yes given the platform's general audit-everything-sensitive posture, but not explicitly committed here.
- Whether a non-visitor context (e.g., a vehicle's plate matched against a BOLO/Violation Flag at a vehicle gate) should reuse this same silent-alarm shape once Remote Gate & Barrier Controls (next doc) is specified — flagged for that doc to pick up if relevant, not resolved here.
