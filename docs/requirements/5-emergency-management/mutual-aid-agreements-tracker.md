# Mutual Aid Agreements Tracker

## Overview

Mutual Aid Agreements Tracker builds the real contract/approval/renewal lifecycle EOC Logistics Hub explicitly deferred — that doc's local **Mutual Aid Organization** entry was a deliberately richer-than-a-bare-pointer interim stand-in, flagged for reconciliation once this feature existed, the same pattern Resource Logistics Catalog just closed for EOC Logistics Hub's Resource Type Definition.

Three MODULES.md components, each landing on an established mechanism:

1. **MOU/MOA Database** — the signed agreement document itself is an ordinary Document Registry record (hash/version, the same model every other signed-document need on the platform already uses — Key Custody's digital custody signatures, Pre-Registration Portal's NDA signing).
2. **Resource Directory** — a partner's contact info comes from Organization Party's own inherited contact information (nothing new); its declared **capabilities now reference Resource Logistics Catalog's real FEMA/NIMS taxonomy** (`Resource Definition` — Category/Kind/Type) instead of EOC Logistics Hub's free-text `capability_tags[]`, so "who else can supply a Type-2 generator" is a real, structured query rather than a tag-matching guess.
3. **Review Reminders** — a genuinely new **second consumer** of Improvement Plan (IP) Tracking's just-built lead-time reminder shape, confirming it as a real platform mechanism rather than a one-off local to that doc. Per the platform's established "promote on second consumer" discipline, it's promoted here into Active Call Alerts & Timers alongside Duration Watchdog — a sibling mechanism (before-a-deadline vs. after-a-threshold), not a merge of the two.

**Mutual Aid Agreement** is modeled as a rich **EntityAssociation Extension** — a relationship between the tenant's own self-designated Organization and the partner's Organization Party, with real content (dates, scope, capabilities, the signed document) that benefits from independent audit/history — the same shape already proven for `CredentialAssignment` and `ICS Role Assignment`, not a bare directory row and not an Activity (this is a governed relationship with a lifecycle, not an occurrence).

## Actors & Roles

- **EOC Coordinator / Logistics Section Chief** — authors, renews, and terminates Mutual Aid Agreements.
- **Site / Tenant Admin** — designates the tenant's own representing Organization, configures Review Reminder lead times.
- **Any EOC role holder** — browses the Resource Directory (partner capabilities/contacts) while working a Resource Request.

## User Stories

- As a **Logistics Section Chief**, I want to look up which mutual-aid partners can supply heavy equipment, filtered by the same resource categories I already use internally.
- As an **EOC Coordinator**, I want the signed MOU on file, versioned, with the same integrity guarantees as any other official document on the platform.
- As a **Tenant Admin**, I want a reminder well before an agreement's renewal date, not a surprise on the day it lapses.
- As an **EOC Coordinator**, I want to renew an agreement in place (same relationship, extended) versus recording a substantively renegotiated one as its own new record with a link back to what it replaced.
- As an **EOC Coordinator**, I want an expired agreement to stay visible, clearly marked expired, rather than silently disappearing from the directory.

## Functional Requirements

### Mutual Aid Agreement (relationship, retiring EOC Logistics Hub's stand-in)
1. **Mutual Aid Agreement** registers as an EntityAssociation Extension (`entity_id_a` = the tenant's self-designated representing Organization [nullable if none has been designated — see Open Questions], `entity_id_b` = the partner's Organization Party, inline-created if not already registered, the same durable/dedup-worthy-relationship discipline Agency Handoff Log already established) — `agreement_type` (mou, moa), `effective_start`, `effective_end`, `scope_notes`, `capability_refs[]` (Resource Definition references), `status` (draft, active, expired, terminated), and `document_ref` (the signed agreement, via Document Registry).
2. **This doc directly supersedes EOC Logistics Hub's local Mutual Aid Organization entry** *(retrofit — see [eoc-logistics-hub.md](../3-command-center-dashboard-eoc/eoc-logistics-hub.md))* — that doc's free-text `capability_tags[]` and bare agreement fields are replaced by this record's `capability_refs[]` (real Resource Definition references) and full lifecycle; the underlying Organization Party reference is unchanged.
3. `active` → `expired` is an automatic transition (Real-Time Delivery Timer Service, the same restart-safe scheduling infrastructure Duration Watchdog and Approaching-Deadline Reminder both run on) the moment `effective_end` passes with no renewal — never a silent disappearance from the directory, the same "surfaced, not hidden" discipline EOC Logistics Hub already established for its own interim fields.

### Renewal and supersession
4. **Renew** extends the same agreement in place — updates `effective_end`, optionally attaches an updated Document, and appends a **Renewal Event** to the agreement's own history — the relationship continues, it isn't replaced.
5. A substantively renegotiated agreement (new scope, new terms) instead **terminates** the current one and creates a fresh Mutual Aid Agreement with `superseded_agreement_ref` pointing at the one it replaces — the same "a real, honest new record rather than silently overwriting history" instinct the platform applies whenever a relationship's terms materially change (Access Credential's provisioning/revocation pending-confirmation honesty, Clearance Profile's independent assignment rows).

### Resource Directory
6. Each active Mutual Aid Agreement's `capability_refs[]` references one or more **Resource Definitions** (Resource Logistics Catalog's Category/Kind/Type taxonomy, unmodified) — a Logistics Section Chief browsing "who can supply X" filters the directory the same way they'd filter the tenant's own catalog, both speaking the same taxonomy.
7. Partner contact information is never re-entered here — it's read directly from the referenced Organization Party's own contact fields (Party Registry, inherited), the platform's standard "don't duplicate what's already owned elsewhere" discipline.

### Review Reminders (promoted mechanism, second consumer)
8. **Approaching-Deadline Reminder is promoted from Improvement Plan (IP) Tracking's locally-built Deadline Reminder Policy into a shared mechanism**, registered alongside Duration Watchdog in Active Call Alerts & Timers *(retrofit — see [active-call-alerts-timers.md](../2-dispatch-cad/active-call-alerts-timers.md))*, per the platform's "promote on second real consumer" discipline (Domain Events/Command Bus, Tenant-Defined Types, AI/LLM Services, Duration Watchdog itself all followed this same path). This doc registers a second instance — `(mutual_aid_agreement, effective_end)` — alongside IP Tracking's original `(improvement_action, target_completion_date)`, both now consuming one shared, tenant/category-configurable `lead_time_offsets_days[]` + per-offset debounce mechanism rather than two independent local ones.
9. A configured lead-time offset fires a Notifications Engine reminder to the agreement's owning Logistics Section Chief/EOC Coordinator role — never blocking anything, the platform's standing rule.

## Data Model / Fields

**Mutual Aid Agreement** (EntityAssociation Extension — entity_id_a = tenant's representing Organization [nullable], entity_id_b = partner Organization Party; association_id is the shared PK)
- agreement_type (mou, moa)
- effective_start, effective_end
- scope_notes, capability_refs[] (FK[] → Resource Definition)
- status (draft, active, expired, terminated)
- document_ref (FK → Document)
- superseded_agreement_ref (nullable — points at the prior agreement this one replaces)

**Renewal Event** (child, append-only history)
- event_id, agreement_ref, renewed_at, renewed_by, new_effective_end, document_ref (nullable — an updated document, if any)

**Approaching-Deadline Reminder Registration** *(retrofit — [active-call-alerts-timers.md](../2-dispatch-cad/active-call-alerts-timers.md); generalized from IP Tracking's local Deadline Reminder Policy)*
- `(mutual_aid_agreement, effective_end)` — this doc's registered instance, alongside `(improvement_action, target_completion_date)`.

## States & Transitions

- **Mutual Aid Agreement:** `draft` → `active` → `expired` (automatic, on `effective_end` passing) or `terminated` (explicit action, e.g. for a substantive renegotiation, FR #5). `active` can also transition directly to `terminated` for an agreement ended outside the normal expiration path.
- **Renewal Event:** append-only, created once per renewal, never edited.

## Integrations

- **EOC Logistics Hub** *(retrofitted — superseded)*: local Mutual Aid Organization entry is replaced by this doc's real Mutual Aid Agreement; the underlying Organization Party reference carries over unchanged.
- **Resource Logistics Catalog**: source of `capability_refs[]`'s Resource Definition taxonomy, reused unmodified — the first cross-doc consumer of that taxonomy beyond the tenant's own resource catalog.
- **Party Registry / Organization Registry**: source of partner contact information, and the inline-creation discipline for a first-time partner.
- **Document Registry**: source of `document_ref`'s hash/version model for the signed agreement.
- **Active Call Alerts & Timers** *(retrofitted)*: gains the generalized Approaching-Deadline Reminder mechanism (promoted from IP Tracking), registers this doc's `(mutual_aid_agreement, effective_end)` instance.
- **Improvement Plan (IP) Tracking** *(retrofitted)*: its own Deadline Reminder Policy now consumes the shared, promoted mechanism instead of a locally-owned one — no change to its own Improvement Action behavior.
- **Real-Time Delivery & Server-Side Timers**: owns the scheduling engine driving both `active → expired` auto-transition and Approaching-Deadline Reminder checks.
- **Command/Action Bus**: Create/Renew/Terminate Mutual Aid Agreement and Designate Representing Organization all register as actions.
- **Structured Logging & Audit Trails**: every agreement status transition and Renewal Event is audit-tier.

## Permissions

| Action | Site/Tenant Admin | EOC Coordinator / Logistics Section Chief | Any EOC role holder |
|---|---|---|---|
| Designate the tenant's representing Organization | ✅ | ❌ | ❌ |
| Configure Approaching-Deadline Reminder lead times | ✅ | ❌ | ❌ |
| Create / Renew / Terminate a Mutual Aid Agreement | ✅ | ✅ | ❌ |
| Browse the Resource Directory | ✅ | ✅ | ✅ |
| View a specific agreement's terms/document | ✅ | ✅ | inherits site RBAC/ABAC |

## Non-Functional / Constraints

- An expired or terminated Mutual Aid Agreement remains fully queryable and visible in the directory, clearly marked, never hidden or deleted.
- Approaching-Deadline Reminder delivery never blocks agreement expiration, renewal, or any other operational action.
- `capability_refs[]` never duplicates Resource Definition's own fields — it's a pure reference list, kept in sync automatically if a referenced Resource Definition is later archived (the reference simply resolves to an archived definition, surfaced honestly, never silently dropped).

## Acceptance Criteria

- [ ] Creating a Mutual Aid Agreement with a not-yet-registered partner agency prompts inline Organization creation — it never accepts free text in its place.
- [ ] An agreement's `capability_refs[]` can be filtered against the same Resource Definition taxonomy used by the tenant's own Resource Logistics Catalog.
- [ ] An agreement automatically transitions to `expired` the moment its `effective_end` passes with no renewal, remaining visible and clearly marked in the directory.
- [ ] Renewing an agreement extends `effective_end` and logs a Renewal Event without creating a new Mutual Aid Agreement record.
- [ ] Recording a substantively renegotiated agreement terminates the prior one and creates a new record with `superseded_agreement_ref` set correctly.
- [ ] Configuring a lead-time offset on the Approaching-Deadline Reminder mechanism produces exactly one reminder per offset per agreement, the same debounce behavior IP Tracking's original mechanism already guarantees.
- [ ] IP Tracking's existing Deadline Reminder behavior for Improvement Actions is unaffected by this doc's promotion of the underlying mechanism.
- [ ] Partner contact information displayed in the directory always reflects the referenced Organization Party's current contact fields — never a separately maintained copy.

## Open Questions

- Exact resolution when no tenant Organization has been designated as "representing" the tenant (Tenant Management's existing self-designation mechanism) — whether `entity_id_a` stays null (tenant-implicit) or designation becomes a hard prerequisite for authoring an agreement — not committed here.
- Whether Mutual Aid Agreements should support a formal internal approval/sign-off gate (mirroring DAR's Shift Review governance-record shape) before reaching `active`, versus the lighter draft→active transition specified here — not elicited, flagged for a future pass if real usage shows a need.
- Exact content/structure of `scope_notes` (free text vs. a more structured scope taxonomy) — a content/config concern, not committed here.
