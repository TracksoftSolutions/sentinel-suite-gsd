# Key Custody & Auditing

## Overview

Key Custody & Auditing layers three things on top of Item Registry's already-existing custody mechanism (current holder + transfer history, atomic, audit-tier) — it never reinvents custody transfer itself, only adds signature capture, overdue detection, and electronic-cabinet ingestion around it. Two mechanisms get real, explicitly-flagged generalizations here, both well-motivated by a genuine second consumer rather than built speculatively:

- **Duration Watchdog broadens beyond Activity-only to also watch EntityAssociation state** — Key custody sitting "active" too long is exactly the same shape as a Dispatch sitting "on_scene" too long, just on a relationship record instead of an occurrence record.
- **Duration Watchdog's threshold gains a second resolution mode**: not just a flat configured duration, but an optional **dynamic resolved target** — "the holder's current DAR Shift Window end time" — with a flat-duration fallback when no Shift Window applies, per explicit user direction and MODULES.md's own "before shift end" framing.

**Lockbox Access Logs** (electronic key cabinets — Traka and similar) get a new, dedicated **Key Cabinet Adaptor** family, deliberately separate from PIAM Adaptor Registration since a dispensing cabinet is physically different from identity/credential governance. Cabinet dispense/return events flow through Activity Registry's existing **Signal Disposition** valve — the same mechanism every other hardware-signal producer in the platform already uses — rather than a new ingestion pipeline; a promoted dispense/return signal auto-opens/closes the corresponding Key/Key Ring `CustodyAssociation`.

## Actors & Roles

- **Guard/Officer** — the routine custody holder; hands out/receives keys, signs when required.
- **Security/Access Admin** — configures Key Custody Signature Policy, Overdue Key Watchdog, and the Key Cabinet Adaptor.
- **Front Desk/Security (issuing party)** — records a manual (non-cabinet) handout/return.
- **Supervisor+** — receives overdue-key escalation.

## Functional Requirements

### Custody transfer (reused, not reinvented)
1. Handing out or returning a Key or Key Ring is an ordinary Item Registry `CustodyAssociation` transfer — this doc introduces no new relationship type, only what wraps around it.

### Digital custody signatures
2. A tenant-configurable **Key Custody Signature Policy** (Settings & Preferences Definition) declares which `key_type`s require a captured signature at handout (e.g., required for `master`/`grand_master`, skipped for a routine `change_key`) — the module's recurring "configurable policy, not fixed rule" shape applied again. A captured signature becomes a real Document via Document Registry (`DocumentAuthorAssociation` to the recipient), the identical mechanism Pre-Registration Portal's NDA signing already established — no new signature-storage mechanism.

### Overdue key detection
3. **Overdue Key Watchdog** is a Duration Watchdog instance over `(custody_association, status, active)` — the first Duration Watchdog to watch an EntityAssociation's state rather than an Activity's, an explicit, generalized broadening of that mechanism (retrofit — Active Call Alerts & Timers) rather than a parallel one-off mechanism, since "a record sitting in a given state too long" is exactly the same shape regardless of whether the record is an Activity or an Association.
4. The watchdog's threshold resolves via a new **dynamic target mode**, also a retrofit to Duration Watchdog: where the current holder has an active DAR Shift Window, the threshold is that Shift Window's own end time (matching MODULES.md's "before shift end" framing exactly); where none applies (a site not tracking DAR shifts, or a multi-day checkout), a flat tenant-configured duration is the fallback — the platform's explicit-beats-default resolution chain applied to a genuinely new case (a resolved timestamp instead of a fixed duration).
5. Crossing the threshold feeds the existing Critical Event Escalation Policy — no new alerting mechanism — notifying Supervisor+.

### Electronic key cabinet ingestion (Lockbox Access Logs)
6. A **Key Cabinet Adaptor** (new, separate from PIAM Adaptor Registration — a dispensing cabinet is physically different hardware, not identity/credential governance) declares `adaptor_type` (`traka`, other) per site. The platform never speaks the cabinet's own protocol directly, the same integrate-don't-replace boundary every other hardware touchpoint in the platform observes.
7. A cabinet's dispense/return events flow through Activity Registry's existing **Signal Disposition** valve, exactly like any other hardware-signal producer (Alarm Zones, Environmental Sensors) — no new ingestion pipeline. A signal promoted to `activity` disposition automatically opens (dispense) or closes (return) the corresponding Key/Key Ring's `CustodyAssociation`; a `display_only`/`telemetry`-disposed signal is visible/retained per that valve's existing rules without driving custody state.

## Data Model / Fields

**Key Custody Signature Policy** (Settings & Preferences Definition)
- tenant_id/site_id, required_key_types[] (values from Key's `key_type`)

**Duration Watchdog** *(retrofit — Active Call Alerts & Timers)*
- watched_record_kind gains `entity_association` alongside the existing `activity` kind
- threshold gains `resolution_mode` (flat_duration, shift_window_relative_with_fallback) and, when the latter, a fallback flat duration

**Key Cabinet Adaptor** (new)
- adaptor_id, tenant_id, site_ref, adaptor_type (traka, other), enabled (bool)

## States & Transitions

No new record lifecycle — `CustodyAssociation` follows its existing active/removed shape; Duration Watchdog and Signal Disposition both follow their existing established transitions unmodified.

## Integrations

- **Item Registry**: base custody mechanism, reused wholesale — the doc this feature was always anticipated by name in.
- **Document Registry**: a required custody signature becomes a real Document, identical to Pre-Registration Portal's NDA mechanism.
- **Active Call Alerts & Timers (Duration Watchdog)** *(retrofit)*: gains EntityAssociation-watching and dynamic Shift-Window-relative threshold resolution — both generalizations this doc is the first real consumer of.
- **Daily Activity Reports (DAR Shift Window)**: source of the dynamic overdue threshold when one applies — the interim scoping record established there gets a genuine new consumer.
- **Activity Registry (Signal Disposition)**: the ingestion path for Key Cabinet Adaptor events — no new pipeline.
- **Settings & Preferences**: owns Key Custody Signature Policy.
- **Structured Logging & Audit Trails**: every custody transfer (manual or cabinet-ingested), signature capture, and overdue escalation is audit-tier, inherited from the underlying mechanisms unmodified.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Front Desk/Security | Guard/Officer |
|---|---|---|---|---|
| Configure Key Custody Signature Policy, Overdue Key Watchdog threshold, Key Cabinet Adaptor | ✅ | ✅ | ❌ | ❌ |
| Record a manual handout/return | ✅ | ✅ | ✅ | ✅ (self) |
| View overdue-key escalations | ✅ | ✅ | ✅ | ❌ |

## Non-Functional / Constraints

- A cabinet-ingested custody transfer must carry the same audit fidelity as a manually recorded one — no reduced trust for automated ingestion.
- Overdue Key Watchdog's dynamic threshold resolution must not silently default to "never overdue" when a holder has no Shift Window and no flat duration is configured — an unconfigured policy should be an honest no-op (no watchdog instance active), never an implicit infinite grace period assumed correct.
- Digital signature capture reuses Document Registry's/Blob Storage's existing hash-integrity and retention treatment — no new storage mechanism.

## Acceptance Criteria

- [ ] Handing out a `master`-type key under a policy requiring signatures for that type blocks completion until a signature is captured and stored as a real Document; a `change_key` handout under the same policy completes without one.
- [ ] A key held past its holder's own DAR Shift Window end time triggers the overdue watchdog, when Shift-Window-relative resolution is configured and a Shift Window is active for that holder.
- [ ] A key held past a flat configured duration triggers the same watchdog when no Shift Window applies.
- [ ] A dispense event from a connected Key Cabinet Adaptor at `activity` disposition automatically opens a `CustodyAssociation` for the dispensed key with no manual step; a subsequent return event closes it.
- [ ] A cabinet event disposed as `telemetry` is retained and visible without changing any Key's custody state.
- [ ] Overdue escalation reaches Supervisor+ through the existing Critical Event Escalation Policy with no new delivery mechanism.

## Open Questions

- Exact Key Cabinet Adaptor connector list (Traka and which others) — technical-spec/roadmap, not resolved here.
- Whether a lost/stolen key (never returned, watchdog escalated, never resolved) needs its own terminal "presumed lost" status distinct from an ordinary open custody transfer — flagged as a plausible future addition, not built here since MODULES.md doesn't call it out explicitly.
- Whether Duration Watchdog's new EntityAssociation-watching capability should be retroactively offered to any other existing single-current-value association (e.g., a Credential sitting `pending_provisioning_confirmation` too long) — a natural extension now that the mechanism supports it, but not built into any other doc's own instance here; flagged for those docs to pick up if a real need surfaces.
