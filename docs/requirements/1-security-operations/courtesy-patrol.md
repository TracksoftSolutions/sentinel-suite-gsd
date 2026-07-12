# Courtesy Patrol

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Courtesy Patrol is a category of goodwill/service-oriented guard activity, distinct from both Guard Tour (fixed checkpoint verification) and Patrol Management's dispatch layer: a **patrol without checkpoints**, an **escort** (a quick one-off walk to a car — not a secured-area escort, a different concept entirely), a **tire change**, a **jump start**, and a **welfare check**, among others. It registers as its own **Activity extension** (per Activity Registry), not a specialization of Checkpoint Scan or a reuse of Patrol Management's Patrol Request — a Courtesy Patrol can be entirely self-initiated by a Guard (they noticed someone struggling and helped) with no dispatch step at all, but when it *is* dispatched through Patrol Management (a call comes in, a Supervisor/Dispatcher creates a Patrol Request and assigns a unit), the resulting Courtesy Patrol optionally references the Patrol Request it fulfilled — the same launch-point relationship Checkpoint Scan has to Patrol Finding, not a merged mechanism.

**Category is tenant-configurable**, mirroring the pattern already established for DAR Entry and Patrol Finding — Patrol, Escort to Vehicle, Tire Change, Jump Start, and Welfare Check ship as defaults, tenant-editable.

**Some categories need a few extra structured fields the others don't** (a Jump Start cares about battery type; a Tire Change cares about which tire; a generic Patrol needs neither). Rather than giving each category its own full Activity-extension TPT level — disproportionate machinery for "a few optional extra fields on a narrative record" — Courtesy Patrol uses Entity Registry Core's **`extended_fields`** mechanism: a bounded, schema-governed JSONB column living on the base `Entity` row (shared, per TPT, by every level including this one), reserved as an explicit escape hatch and never the default way to add fields. This pattern was drafted locally here first, then **promoted to Entity Registry Core** during elicitation so any future feature with the same "a few optional fields that don't earn a full TPT level" need can reuse one governed mechanism instead of reinventing it — see the retrofitted [entity-registry-core.md](../0.5-master-records/entity-registry-core.md). Courtesy Patrol's own contribution is the **schema selector**: its Category Definition declares which small field set applies (`extra_fields_schema[]`), scoped *per tenant-configurable category* — finer-grained than Entity Registry Core's own type-level default schema — while the actual storage and write-time validation is Entity Registry Core's, not reimplemented here.

**Recipient vs. requestor are kept distinct.** The **requestor** is whoever asked for help — often unregistered/anonymous (someone flags down a guard, or calls in), captured as free text rather than forcing a full Person record for every one-off caller (unlike Person Registry's `EmergencyContactAssociation`, which deliberately requires a real minimal Person entity because it's a durable, dedup-worthy formal relationship — a one-off courtesy caller isn't). The **recipient** — who actually received the help — is tagged via Activity Registry's existing generic `ActivityParticipantAssociation` when they're identifiable, making them a real, searchable participant like on any other Activity; a **subject Item** (the vehicle needing a jump start or tire change) is tagged the same way when relevant. A basic external-request intake (requestor name/contact, intake method) is in scope now, even though full call-logging is deferred to Dispatch/CAD and Module 17's future Call Inbound Info Line.

Where a Courtesy Patrol is a checkpoint-free walk-through, it's also a **launch point** for a Patrol Finding or richer Activity type, reusing exactly the mechanism Guard Tour's Checkpoint Scan already established — no new mechanism needed.

## Actors & Roles

- **Guard** — performs Courtesy Patrols, either self-initiated or when assigned; logs category, recipient, and outcome.
- **Supervisor/Dispatcher** — logs an external intake request (a call comes in), creates a Patrol Management Patrol Request and assigns a unit when dispatch is warranted, reviews Courtesy Patrol activity.
- **Tenant Admin** — configures Category Definitions (including any extra-fields schema) and outcome taxonomy via Settings & Preferences.
- **Records Admin** — resolves Entity Registry Core dedup flags on Courtesy Patrol (standard Activity dedup applies — unlike Checkpoint Scan, these are meaningfully distinct occurrences, not high-volume routine events).

## User Stories

- As a **Guard**, I want to log that I walked an employee to their car after dark, so there's a record even though nobody dispatched me to do it.
- As a **Dispatcher**, I want to log a call from a tenant whose battery died, create a Patrol Request, and have the responding guard's Courtesy Patrol record automatically reference that request.
- As a **Guard performing a jump start**, I want to record the vehicle and a couple of jump-start-specific details without the form asking me questions that make no sense for an escort.
- As a **Tenant Admin**, I want to add a new courtesy category specific to our site (e.g., "Umbrella Escort") without needing a platform change.
- As a **Supervisor**, I want a welfare check logged during a courtesy patrol to still let the guard escalate to a real Incident if something's actually wrong, using the same launch-point mechanism as everywhere else.
- As a **Records Admin**, I want two independently-logged courtesy patrols to be treated as genuinely separate occurrences, not silently suppressed the way near-duplicate checkpoint scans are.

## Functional Requirements

### Courtesy Patrol (Activity extension)
1. **Courtesy Patrol** registers as an Activity extension (per Activity Registry): inherits base Activity identity, offline-safe numbering, standard dedup/merge (no debounce exception here — these are meaningfully distinct occurrences, unlike Checkpoint Scan), and display-label requirements.
2. `category` references a tenant-configurable **Category Definition** (Settings & Preferences-registered), defaulting to a shipped set: Patrol, Escort to Vehicle, Tire Change, Jump Start, Welfare Check.
3. A Category Definition may optionally declare an `extra_fields_schema[]` (field key, label, type, required) for categories that need structured detail beyond the base narrative (e.g., Jump Start: battery type; Tire Change: tire position). This schema is what a specific Courtesy Patrol's `extended_fields` (Entity Registry Core's shared, bounded JSONB column — see that doc) is validated against at write time; categories with no schema leave it unpopulated.
4. `narrative` is free text, always available regardless of category.
5. **Requestor** — `requestor_name`, `requestor_contact` (free text, capturing an unregistered/anonymous caller), and `intake_method` (phone, in_person, app, self_initiated). An optional `requestor_party_ref` links to a known/registered Party when the requestor is already recognized — never mandatory.
6. **Recipient** (who was actually helped) and, where relevant, **subject Item** (the vehicle needing a jump start/tire change) are tagged via `ActivityParticipantAssociation` — the same generic mechanism every other Activity type uses, not a bespoke field.
7. **Location** is tagged via `ActivityLocationAssociation`, same as any other Activity.
8. `outcome` references a tenant-configurable outcome list (e.g., Resolved, Referred to Towing, Unable to Assist), with `outcome_notes` free text.
9. `source_patrol_request_ref` (nullable, direct field, fixed at creation) links back to the Patrol Management Patrol Request this Courtesy Patrol fulfilled, when dispatched through that mechanism — never required, since a self-initiated Courtesy Patrol has no Patrol Request at all.

### Launch point (checkpoint-free walk-through)
10. A Courtesy Patrol (particularly the checkpoint-free "Patrol" category) is a **launch point** for a Patrol Finding or any other Activity type, via the exact mechanism already established on Guard Tour's Checkpoint Scan (Command/Action Bus context-seeding, location pre-filled from the Courtesy Patrol's own `ActivityLocationAssociation`) — no new mechanism is introduced here.

## Data Model / Fields

**Courtesy Patrol Category Definition** (Settings & Preferences registration)
- category_id, tenant_id, name, enabled
- extra_fields_schema[] (nullable — field_key, label, field_type, required)

**Courtesy Patrol Outcome Definition** (Settings & Preferences registration)
- outcome_id, tenant_id, name, enabled

**Courtesy Patrol** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- category (ref → Category Definition) — selects which schema its inherited `extended_fields` (Entity Registry Core, on the shared base Entity row — not a field of this table) is validated against
- narrative
- requestor_name, requestor_contact (free text), requestor_party_ref (nullable), intake_method (phone, in_person, app, self_initiated)
- outcome (ref → Outcome Definition, nullable until concluded), outcome_notes
- source_patrol_request_ref (nullable, direct field, fixed at creation)
- *(recipient/subject are rows, not fields — ActivityParticipantAssociation; location is a row — ActivityLocationAssociation)*

## States & Transitions

**Courtesy Patrol:** follows base Activity lifecycle unmodified — `open` → `in_progress` → `concluded` (outcome recorded) | `cancelled`. No extension-level status nuance needed (unlike Guard Tour's Patrol `missed` state) — Courtesy Patrol has no "required occurrence" concept to fail to meet.

**Category Definition / Outcome Definition:** `enabled` → `disabled` (soft-disable, preserves historical records' references).

## Integrations

- **Activity Registry**: Courtesy Patrol registers as its own Activity extension, using the standard (non-excepted) dedup/merge path.
- **Entity Registry Core**: identity, dedup/merge, and display-label requirements, same as any other Activity type. Also the **source of the `extended_fields` storage/validation mechanism itself** (retrofitted during this doc's elicitation, see that doc's Extended Fields section) — Courtesy Patrol supplies the per-category schema selector (`extra_fields_schema[]` on its own Category Definition), not the underlying storage.
- **Settings & Preferences**: owns Category Definition (including any `extra_fields_schema`) and Outcome Definition, both tenant-configurable via the existing engine.
- **Party Registry, Item/Vehicle Registry**: source of recipient and subject-vehicle references via `ActivityParticipantAssociation`.
- **Location Registry**: source of the Courtesy Patrol's `ActivityLocationAssociation`.
- **Patrol Management**: Patrol Request gains an optional `courtesy_patrol_ref` (nullable — retrofit, mirroring the `post_ref` retrofit already made to Guard Tour's Route Assignment) so a dispatched courtesy call's fulfillment detail is queryable from the request side too.
- **Guard Tour & Checkpoint Verification**: reuses its Checkpoint Scan → Patrol Finding launch-point mechanism unmodified for Courtesy Patrol's own follow-on record creation (#10) — no duplicated mechanism.
- **Daily Activity Reports (DAR)**: Courtesy Patrols are ordinary Activities, automatically picked up by any DAR filter matching guard/site/time window.
- **Shift Passdowns & Handover Notes**: an unresolved Courtesy Patrol (e.g., "Referred to Towing" pending follow-up) is a natural Pass-On Rule candidate, same mechanism as elsewhere.
- **Command/Action Bus**: "Log courtesy patrol," "Create courtesy patrol from patrol request" register as invokable actions.
- **Dispatch/CAD, Module 17 (Call Inbound Info Line, future)**: intended eventual replacement/enrichment of the basic `requestor_name`/`requestor_contact` intake fields with real call-logging — deferred, not built now.

## Permissions

| Action | Guard | Supervisor/Dispatcher | Tenant Admin |
|---|---|---|---|
| Log a self-initiated Courtesy Patrol | ✅ | ✅ | ❌ |
| Log an external intake request | ✅ | ✅ | ❌ |
| Create a Patrol Request and assign a unit for a courtesy call | ❌ | ✅ | ✅ |
| Record outcome / complete a Courtesy Patrol | ✅ (own) | ✅ | ❌ |
| Configure Category Definitions (incl. extra-fields schema) | ❌ | ❌ | ✅ |
| Configure Outcome Definitions | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- `extended_fields` schema validation (Entity Registry Core's mechanism, per the category schema this doc selects) must reject fields not declared on the category's schema, and enforce declared `required` fields — but must never block saving the record entirely if a non-required field is missing (same graceful-degradation principle established for Guard Tour's verification methods).
- Courtesy Patrol creation must work fully offline, per the platform's established offline model, including when category schema data isn't yet synced locally (degrade to accepting the narrative and syncing `extended_fields` validation later, never blocking the guard in the field).
- WCAG 2.1 / Section 508 accessible logging, category-detail entry, and outcome-recording flows, day one.

## Acceptance Criteria

- [ ] A Guard can log a self-initiated Escort to Vehicle with no Patrol Request involved at all.
- [ ] A Dispatcher logging an external intake and creating a Patrol Request produces a Courtesy Patrol, once the assigned guard completes it, correctly referencing that Patrol Request via `source_patrol_request_ref`.
- [ ] Selecting the Jump Start category surfaces only that category's declared extra fields, not another category's.
- [ ] Saving a Jump Start record missing a `required` extra field is rejected; saving one missing a non-required extra field succeeds.
- [ ] A recipient Party and a subject Vehicle Item are both correctly tagged via `ActivityParticipantAssociation` on the same Courtesy Patrol record.
- [ ] A Tenant Admin can add a new Category Definition with its own extra-fields schema without a platform change.
- [ ] Two independently-logged Courtesy Patrols for the same guard/location/day are NOT collapsed or debounced — both remain distinct records (contrast with Checkpoint Scan's debounce behavior).
- [ ] A checkpoint-free "Patrol" category Courtesy Patrol can launch creation of a Patrol Finding with location pre-filled, identically to Guard Tour's Checkpoint Scan launch point.
- [ ] A Courtesy Patrol logged offline, including one populating `extended_fields`, syncs correctly once connectivity returns.

## Open Questions

- Exact default `extra_fields_schema` for the shipped Jump Start / Tire Change categories — pending UX/content design.
- Whether `extended_fields` field types need richer validation (e.g., enum/select options) beyond the basic key/label/type/required shape — a technical-spec-level decision, shared with Entity Registry Core's own open question on the same point.
- Whether a requestor who turns out to be a known Party should be retroactively linkable (`requestor_party_ref` set after the fact) or must be identified at creation time — not addressed here.
- Exact relationship between Courtesy Patrol's `outcome` taxonomy and any future Incident-escalation path (e.g., does "Unable to Assist" on a Welfare Check nudge toward creating an Incident) — left as an available launch-point action (#10), not a forced/automatic escalation.
- Whether Courtesy Patrol volume ever justifies its own dedup-debounce exception (like Checkpoint Scan's) in high-traffic sites — current default assumes standard dedup is appropriate since occurrences are meaningfully distinct, not confirmed against real usage data.
