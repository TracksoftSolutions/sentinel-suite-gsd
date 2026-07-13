# AI-Assisted Incident Report Writing

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Leans on AI narrative generation so officers spend their time in the field, not at a keyboard. Three kinds of low-friction input feed the AI, all already-established or lightly extended platform mechanisms:

1. **Response Timeline Events** — a new thin Activity extension (`origin_incident_ref`, `phase`, `phase_timestamp`, `recorded_by`, optional `source_location_ref`) capturing the classic response lifecycle: call received, dispatched, en route (with where the responding unit started from), arrived, on scene, cleared. Modeled as real Activities per the user's own framing ("they are activities related to the incident"), each phase its own timestamped event rather than a handful of plain fields — the same "everything trackable is an Activity" discipline used throughout. This is a deliberately interim mechanism: Dispatch/CAD (Module 2, not yet specified) will eventually own the canonical version of this timeline as part of a real Call/dispatch record, and this doc's version is built now specifically because the requirements process is working through modules in order, not implementation order — not because it needs to precede Dispatch/CAD in any technical sense.
2. **Quick field notes** — ordinary Incident Updates (already established), entered via typed text, **voice-to-text** (a new shared transcription capability this doc establishes, later reused by CLI-Style Input's own AI-assist mode voice input), or a tap on a **tenant-configurable canned phrase** ("No injuries," "Suspect fled northbound," "Area secured") that logs instantly with zero typing.
3. **AI Draft Narrative** — an explicit, human-triggered action (never automatic) that synthesizes the Incident's Response Timeline Events, Incident Updates, and core structured fields (category, severity, participants) into a coherent draft narrative. A draft is always a working, editable record — never auto-published. Once a human reviews, edits as needed, and approves it, the final text commits as a normal, immutable **Incident Update** tagged `source = ai_generated`, referencing which timeline data it was synthesized from — keeping "the Update timeline is the complete record" true, and letting Incident Report generation include it exactly like any other update, with no special-casing.

This doc **retrofits Incident Update** (from `incident-reporting-management.md`) only to the extent of adding a `source`/`source_draft_ref` field — no change to Incident's own fields, lifecycle, or Supervisor Review is required.

## Actors & Roles

- **Guard/Responder** — logs Response Timeline Events for their own response (self-logged, mirroring Patrol Management's self-or-on-behalf-of pattern), adds quick field notes (typed/voice/canned phrase), requests an AI Draft Narrative, reviews/edits/approves it.
- **Supervisor/Dispatcher-equivalent** — can log or correct a Response Timeline Event on a unit's behalf, same on-behalf-of posture already established for Mobile Patrol Unit location.
- **Tenant Admin** — configures the canned quick-phrase catalog and Response Timeline phase list via Settings & Preferences.
- **Investigator** — may also request/approve an AI Draft Narrative when compiling a fuller update later.

## User Stories

- As a **Guard responding to an incident**, I want to tap "En Route" and "Arrived" as they happen, instead of writing down times to transcribe later.
- As a **Guard**, I want to tap a canned phrase like "Area secured" instead of typing it out while I'm still standing in the field.
- As a **Guard**, I want to speak a quick note and have it transcribed into the incident's update timeline without unlocking a keyboard.
- As an **Investigator**, I want to request an AI-drafted narrative from an incident's timeline and timestamps, then edit it before it becomes part of the official record, rather than writing the whole thing from scratch.
- As a **Supervisor**, I want to log an "Arrived" timestamp on behalf of a guard who radioed it in and can't operate their device, the same way I already can for a Mobile Patrol Unit's location.
- As a **Tenant Admin**, I want to define our own set of canned quick phrases relevant to our site, rather than being stuck with a generic platform list.

## Functional Requirements

### Response Timeline Event (thin Activity extension, interim pending Dispatch/CAD)
1. **Response Timeline Event** registers as a thin Activity extension: `origin_incident_ref` (direct field, fixed at creation, same non-EntityAssociation reasoning as Incident Update's own parent link), `phase` (call_received, dispatched, en_route, arrived, on_scene, cleared — platform-fixed, not tenant-configurable, since these are structural response-lifecycle stages), `phase_timestamp`, `recorded_by`, `source_location_ref` (nullable, meaningful for `en_route` — where the responding unit started from).
2. A Response Timeline Event is created either **self-logged** by the responding Guard (a single tap stamps `now()`) or **on behalf of** a unit by a Supervisor/Dispatcher-equivalent — the identical self-or-on-behalf-of posture Patrol Management already established for Mobile Patrol Unit location, including the ability to correct a timestamp after the fact (audit-logged, per Structured Logging & Audit Trails).
3. Multiple responders on the same Incident each log their own Response Timeline Events — the timeline is per-responder, not a single shared clock, consistent with an incident potentially having several independently-responding units.

### Quick field notes
4. Incident Update (established in Incident Reporting & Management) gains a `source` field: `typed`, `voice`, `canned_phrase`, or `ai_generated` — recording how each entry was captured, for traceability, without changing Incident Update's own immutability or base shape.
5. **Voice-to-text transcription** is established here as a shared platform capability: a Guard speaks a note, it transcribes to text, and (after the guard confirms, consistent with the platform's general "AI-assist proposes, human confirms" discipline) commits as an Incident Update with `source = voice`. This is the capability CLI-Style Input's own AI-assist mode voice input later reuses rather than building its own.
6. **Canned Quick Phrase** is a tenant-configurable catalog (Settings & Preferences-registered: text, optional category, enabled); tapping one instantly creates an Incident Update with `source = canned_phrase` and zero typing.

### AI Draft Narrative (explicit, human-in-the-loop, never automatic)
7. Requesting an **AI Draft Narrative** is always an explicit action taken by a Guard/Responder, Investigator, or Supervisor — never automatically triggered or silently inserted, matching the discipline already established for CLI-Style Input's AI-assist mode.
8. A draft synthesizes the Incident's Response Timeline Events, its full Incident Update timeline (including prior AI-generated updates, if any), and core structured fields (category, severity, participants) into a coherent narrative — a working, editable record, not yet part of the official timeline.
9. The requesting user reviews and edits the draft as needed, then **approves** it — only on approval does it commit as a real, immutable Incident Update (`source = ai_generated`), referencing the draft it came from (`source_draft_ref`) and which timeline data was synthesized (`generated_from_refs[]`) for full traceability. A discarded (never-approved) draft leaves no trace on the Incident's official timeline.
10. Generating an AI Draft Narrative requires connectivity to the underlying AI generation capability — unlike the rest of the platform's offline-first data capture (timeline events and quick notes all remain fully offline-safe as usual), drafting itself is deferred until connectivity is available, an explicit, disclosed exception rather than a silent limitation.

## Data Model / Fields

**Response Timeline Event** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- origin_incident_ref (direct field, fixed at creation)
- phase (call_received, dispatched, en_route, arrived, on_scene, cleared)
- phase_timestamp, recorded_by, source_location_ref (nullable)
- corrected (bool), correction_history[] (prior value, changed_by, changed_at) — if corrected after initial logging

**Canned Quick Phrase** (Settings & Preferences registration)
- phrase_id, tenant_id, text, category (nullable), enabled

**AI Draft Narrative** (working record — not an Activity, not a Document; mutable until approved or discarded)
- draft_id, tenant_id, incident_ref
- generated_from_refs[] (Response Timeline Event and Incident Update references synthesized into this draft)
- draft_text (current, editable), status (drafted, edited, approved, discarded)
- requested_by, generated_at, approved_by (nullable), approved_at (nullable)

**Incident Update** (retrofit — adds two fields to the existing table in incident-reporting-management.md)
- source (typed, voice, canned_phrase, ai_generated)
- source_draft_ref (nullable, set only when source = ai_generated)

## States & Transitions

**Response Timeline Event:** created once (self- or on-behalf-of logged); `phase_timestamp` correctable after the fact with a retained correction history — the event itself is never deleted or re-parented.

**AI Draft Narrative:** `drafted` → `edited` (optional, user modifies draft_text) → `approved` (commits as an Incident Update, draft becomes read-only history) | `discarded` (abandoned, no Incident Update created).

**Canned Quick Phrase:** `enabled` → `disabled` (soft-disable, preserves historical Incident Updates' references).

## Integrations

- **Incident Reporting & Management**: Response Timeline Event references an Incident directly; Incident Update gains the `source`/`source_draft_ref` fields described here — a light, additive retrofit, no change to that doc's own lifecycle, Supervisor Review, or Report generation.
- **Activity Registry**: Response Timeline Event registers as an Activity extension, inheriting identity, offline-safe numbering, and standard treatment.
- **CLI-Style Input**: consumes this doc's voice transcription capability for its own AI-assist mode's optional voice input, rather than building its own — the dependency direction that doc's own Integrations section already anticipated.
- **Settings & Preferences**: owns the Canned Quick Phrase catalog.
- **Patrol Management**: Response Timeline Event's self-or-on-behalf-of logging directly reuses the posture already established for Mobile Patrol Unit location updates — no new authorization pattern introduced.
- **Structured Logging & Audit Trails**: Response Timeline Event creation/correction, AI Draft Narrative generation/approval/discard, and every resulting Incident Update are audit-tier events.
- **Command/Action Bus**: "Log response timeline event," "Add quick note," "Request AI draft narrative," "Approve draft" register as invokable actions across every surface.
- **Dispatch/CAD (future, Module 2)**: intended eventual owner of the canonical Response Timeline/Call lifecycle; this doc's Response Timeline Event is the deliberately interim mechanism, flagged for reconciliation once that module is specified — same deferred-integration posture used throughout Module 1.

## Permissions

| Action | Guard/Responder | Supervisor | Tenant Admin |
|---|---|---|---|
| Log own Response Timeline Event | ✅ | ✅ | ❌ |
| Log/correct a Response Timeline Event on another unit's behalf | ❌ | ✅ | ❌ |
| Add a quick field note (typed/voice/canned) | ✅ | ✅ | ❌ |
| Request an AI Draft Narrative | ✅ (own/assigned incidents) | ✅ | ❌ |
| Edit/approve/discard a draft | ✅ (own draft) | ✅ | ❌ |
| Configure canned phrases / timeline phase list | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Response Timeline Events and quick field notes (typed/voice/canned) must all work fully offline, consistent with the platform's general offline-first model.
- AI Draft Narrative generation is the one explicitly online-only step in this doc's flow — disclosed clearly to the user when offline, not silently unavailable.
- Voice transcription must degrade gracefully (surface the raw transcript for manual correction) rather than silently failing on poor audio quality.
- An AI-generated draft must never auto-commit as an Incident Update without explicit human approval — this is a hard requirement, not a configurable default, given the liability weight of an incident record.
- WCAG 2.1 / Section 508 accessible timeline-event logging, quick-note entry, and draft review/edit flows, day one.

## Acceptance Criteria

- [ ] A Guard tapping "En Route" creates a Response Timeline Event with `phase = en_route` and the correct timestamp, offline or online.
- [ ] A Supervisor can log an "Arrived" event on behalf of a different unit, and correct a previously-logged timestamp with the correction retained in history.
- [ ] A voice note transcribes to text and, after guard confirmation, commits as an Incident Update with `source = voice`.
- [ ] Tapping a tenant-configured canned phrase instantly creates an Incident Update with `source = canned_phrase` and no typing.
- [ ] Requesting an AI Draft Narrative synthesizes the incident's current Response Timeline Events and Incident Updates into a draft, without altering any existing Incident Update.
- [ ] Editing and approving a draft commits exactly one new, immutable Incident Update with `source = ai_generated`, correctly referencing its source draft and synthesized timeline data.
- [ ] Discarding a draft leaves no Incident Update and no trace on the Incident's official timeline.
- [ ] Attempting to request an AI Draft Narrative while offline is clearly surfaced as unavailable, not silently failed.
- [ ] An approved AI-generated Incident Update appears in Incident Report generation exactly like any other update, with no special-casing.

## Open Questions

- Whether a dedicated "AI/LLM Services" Platform Core feature is eventually needed to formalize the underlying generation/transcription capability this doc (and CLI-Style Input's AI-assist mode) both consume — not specified here, treated as an assumed platform capability for now.
- Exact default canned-phrase catalog and Response Timeline phase list refinements (e.g., whether `arrived` vs. `on_scene` needs tenant-level clarification/renaming for smaller single-building sites where they're effectively simultaneous) — pending UX/content design.
- Whether multiple responders' independently-logged Response Timeline Events need any cross-responder view/reconciliation (e.g., a supervisor comparing every unit's arrival time on one incident) — not addressed here, likely a Module 12 reporting concern.
- Exact AI generation prompt/quality-control approach (hallucination mitigation, fact-grounding strictly to logged timeline data) — a technical-spec-level concern, though the mandatory human-approval gate (#9) is the primary safeguard specified here.
- Whether AI Draft Narrative generation should be rate-limited or cost-tracked per tenant, given real LLM API costs — not addressed here.
