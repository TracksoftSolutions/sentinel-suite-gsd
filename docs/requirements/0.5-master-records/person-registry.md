# Person Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Person Registry specifies **Person**, an Extension Type of the base **Party** Entity Type (per Party Registry — `nc:EntityType`, meaning Person-or-Organization), plus Person's own further Extension Types: Employee, Visitor, Contractor, Occupant (renamed from MODULES.md's "tenant" to avoid collision with the platform's own SaaS tenant concept), and BOLO/Trespass Subject. (A person's involvement in incidents/calls/citations is tracked via Activity Registry's generic Participant association, not a Person-side extension — see § Extension types below.) Per the platform's NIEM-Core-alignment modeling discipline, the base Person record's structure is drawn from `nc:PersonType` rather than an invented ad hoc field list — this matters concretely for this domain, since NIEM Core's physical-description and identification structures are a substantially better fit for BOLO/witness-description use cases than a generic HR-style profile would be. Fields with no NIEM Core equivalent (medical alerts, language fluencies) are clearly-labeled platform-specific augmentations layered on top. This doc also defines the governance model for the most sensitive/liability-heavy extension, BOLO/Trespass Subject.

This doc establishes each extension type's *core* identifying fields and lifecycle only. Richer, feature-specific fields (e.g., an Employee's badge number and reporting structure, a Contractor's vendor affiliation, an Occupant's lease/unit details) are added by each extension type's owning module when that module is specified later (Personnel for Employee, Access Control for Visitor, Subcontractor Management for Contractor, Facility & Zone Management for Occupant) — consistent with Entity Registry Core's extensible registration pattern.

## Actors & Roles

- **HR/Personnel Coordinator, Site Admin, Supervisor** — create/maintain Employee, Contractor, and Occupant profiles within their permission scope.
- **Front desk/lobby Guard, Kiosk flow** — create/update Visitor profiles (detailed further in Access Control's Pre-Registration Portal and Visitor Kiosk App).
- **Supervisor+ (elevated permission)** — creates/clears BOLO/Trespass Subject extensions.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Person entities specifically.
- **Any user with view permission** — views profiles/fields per the RBAC/ABAC and sensitivity rules below.

## User Stories

- As a **front desk Guard**, I want to look up a returning visitor by name/phone and see their existing profile rather than creating a duplicate.
- As a **Safety Officer**, I want a person's medical alert visible to me automatically during an active muster/evacuation event, even though I don't normally have permission to view it day-to-day.
- As a **Supervisor**, I want to flag a person as BOLO with a required justification and an expiration date, so the flag doesn't persist indefinitely without review and there's a clear reason on record.
- As a **Supervisor clearing a BOLO flag**, I want to be re-prompted for authentication before it takes effect, given how consequential clearing a wrong flag (or failing to clear a legitimately resolved one) can be.
- As an **Investigator**, I want to tag a witness, victim, and suspect on an incident with their respective roles, so the person's profile shows this involvement in their interaction timeline.
- As a **Personnel Coordinator**, I want to create an Employee extension on an existing Person record (e.g., a former Visitor who was just hired) rather than creating a brand-new disconnected profile.
- As a **Records Admin**, I want language fluency and emergency contact fields available on every profile type, even if most Visitor records leave them empty, so the fields are there when they're actually needed (e.g., a contractor who's on-site for months).
- As a **Supervisor creating a BOLO flag**, I want to record height, build, and distinguishing features alongside a name, since a real BOLO is often built from a physical description before an identity is even confirmed.
- As a **front desk Guard**, I want a BOLO subject known by a street name or alias to still surface as a match when I search under that alias, not just their legal name.
- As a **DOE Site Admin**, I want to record citizenship for personnel where our facility access rules require it, while a commercial office-building tenant using the same platform never sees that field at all.

## Functional Requirements

### Base Person fields (NIEM Core `nc:Person`-aligned)
1. **Name**: structured per `nc:PersonName` (given, middle, sur, suffix, full-name) rather than a flat string, with support for multiple `PersonOtherName` entries (aliases/AKAs) — directly useful for BOLO subjects known by more than one name.
2. **Physical description**: sex, race/ethnicity, height, weight, eye color, hair color, and distinguishing physical features (scars/marks/tattoos), matching `nc:Person`'s physical-description structure. Race/ethnicity and citizenship (below) are on by default as nullable fields — this platform prioritizes operational/security completeness (matching real-world BOLO and witness-description practice) over omitting fields — but remain tenant-configurable to disable collection entirely, and are classified as sensitive fields subject to the same visibility/audit rules as medical alerts. These are intrinsic characteristics of the person, not of any particular extension/relationship, so they live once on the base entity — never duplicated per extension, which would risk the same person showing conflicting values depending on which extension happened to record it.
2a. **Field presence vs. workflow display are independent.** These fields existing on the base Person record does not obligate every extension type's create/edit UI to prompt for or display them. Each extension type's owning module decides what its own workflow surfaces by default — e.g., Personnel's Employee onboarding flow has no operational reason to prompt for race/ethnicity and can simply omit it from that screen, while Access Control's BOLO-subject creation flow prominently prompts for it, since a real BOLO is often built from physical description before an identity is confirmed. Both read/write the same underlying field on the same Person record; only the surfacing differs per workflow.
3. **Identification**: uses Party's inherited generic `identifications[]` structure (an ID value + a category, e.g., SSN, driver's license, passport, government ID, employee badge) — not a separately defined Person-specific structure.
4. **Citizenship & birth**: citizenship/nationality and birth date/location, relevant to DOE facility-access and security-clearance determination; same on-by-default-but-configurable-and-sensitive treatment as physical description.
5. **Contact information & base identification**: inherited from Party Registry's base fields (`contact_information`, `identifications[]`) — not redefined here. Person's own `identifications[]` usage (SSN, driver's license, passport, government ID, employee badge) simply populates the categories relevant to a person, using the structure Party already provides.
6. **Emergency contacts**: modeled as an `EmergencyContactAssociation` — a TPT subtype of Entity Registry Core's EntityAssociation, `entity_id_a` = this Person, `entity_id_b` = the contact's own Person entity, `role` = the relationship (e.g., "spouse," "parent"). The contact is always a real Person entity in the registry, even if minimal (name + phone only, no other extensions) — there is no free-text/dual-mode fallback, keeping the relationship genuinely Person-to-Person and giving it the same add/remove audit trail as any other association.
7. **Photo**: stored as the canonical reference image for the person; badge printing and kiosk display (Access Control) consume it rather than maintaining separate copies.
8. **Platform-specific augmentations (no NIEM Core equivalent)**: medical alerts and language fluencies. Clearly modeled as additive, non-NIEM fields rather than misrepresented as part of the core taxonomy.
9. Every base field above is optional/nullable — a given profile type collects only what's relevant to it (e.g., a brief Visitor record typically populates name and photo only; a BOLO/Trespass Subject record leans heavily on physical description).
9a. **Display label** (per Entity Registry Core's universal requirement): template strategy, `person_name.full_name` (falling back to a constructed "Unknown — [primary identification]" if no name is on file, e.g., for a BOLO subject known only by description).

### Extension types
10. **Employee** — active/inactive employment lifecycle; core fields: employee identifier, employment status, primary site/assignment. Richer fields (position, reporting structure, licensing) are added by Personnel's own future feature docs.
11. **Visitor** — checked-in/checked-out lifecycle per visit; core fields: visit purpose, host reference. Richer fields (pre-registration, document signing, badge details) are added by Access Control's future feature docs.
12. **Contractor** — active/inactive lifecycle; core fields: vendor/company affiliation reference. Richer fields (license/insurance compliance) are added by Subcontractor Management's future feature docs.
13. **Occupant** — active/inactive residency lifecycle; core fields: unit/location reference. Richer fields (lease details, special assistance needs) are added by Facility & Zone Management's future feature docs.
14. **BOLO/Trespass Subject** — a Person-typed application of Entity Registry Core's generic **BOLO Flag** mechanism (see that doc's § BOLO Flag), not a separately governed extension. Person Registry's contribution is a context field (trespass_notice_ref, nullable) and leaning heavily on the physical-description and identification fields (items 2–4) rather than the base name/contact fields alone, matching real-world BOLO practice. All governance (elevated permission, justification, expiration, step-up authentication, audit) is inherited unmodified from the shared mechanism.
15. **Involvement in Activities** (calls, incidents, citations, accidents, etc.) is **not** a Person extension — it's an `ActivityParticipantAssociation` (Entity Registry Core's EntityAssociation, extended in Activity Registry). A person's cross-module history of what they've been involved in is sourced entirely from that mechanism; this doc intentionally maintains no parallel "Incident Participant" structure.

### Sensitive field visibility
16. Medical alerts, `EmergencyContactAssociation` rows, race/ethnicity, and citizenship are visible by default only to roles holding a specific permission (e.g., HR/Personnel, Safety, Records Admin) — grouped together as the platform's sensitive-field classification for Person records.
17. During an active muster/evacuation/emergency event (as signaled by Emergency Planning's Muster Check-in App or equivalent), visibility of medical alerts and emergency contact associations specifically (not race/ethnicity or citizenship, which have no emergency-response urgency) automatically broadens to include EOC/muster-coordinating roles for the duration of that event, then reverts to the default restricted scope once the event closes.

### Deduplication
18. Person-specific match signals for Entity Registry Core's deduplication engine include name (including known aliases), date of birth, email, phone, and identification numbers (where collected) — surfaced as potential duplicates for human review per that feature's model, never auto-merged.

## Data Model / Fields

**Person** (TPT level: entity_id is the shared PK, FK → Party.entity_id — structured per `nc:PersonType`)
- entity_id (PK, FK → Party; `identifications[]` and `contact_information` are inherited from Party, not redefined here)
- person_name (given_name, middle_name, sur_name, suffix, full_name), other_names[] (aliases/AKAs, each with a type: alias, maiden_name, etc.)
- birth_date, birth_location (nullable)
- sex, race, ethnicity *(sensitive; on by default, tenant can disable collection)*
- height, weight, eye_color, hair_color, physical_features[] (description, e.g. scars/marks/tattoos)
- citizenship *(sensitive; on by default, tenant can disable collection)*
- photo_ref
- medical_alerts[] (description, severity) *(sensitive, non-NIEM platform augmentation)*
- language_fluencies[] (language, proficiency) *(non-NIEM platform augmentation)*

*(Emergency contacts are `EmergencyContactAssociation` rows, not a field here — see Entity Registry Core's Data Model for the base EntityAssociation shape.)*

**Employee** (TPT level: entity_id shared PK, FK → Person.entity_id)
- entity_id (PK, FK → Person), employee_identifier, employment_status, primary_site_ref

**Visitor** (TPT level)
- entity_id (PK, FK → Person), visit_purpose, host_ref, check_in_status

**Contractor** (TPT level)
- entity_id (PK, FK → Person), vendor_ref

**Occupant** (TPT level)
- entity_id (PK, FK → Person), unit_or_location_ref

**BOLO/Trespass Subject** — no separate data model here; it's a Person-entity application of Entity Registry Core's **BOLO Flag** (unary, not a TPT level — see that doc's Data Model), using its `supporting_document_ref` for a linked trespass notice.

*(A person's involvement in incidents, calls, citations, etc. is an `ActivityParticipantAssociation` row — see Activity Registry's Data Model. No parallel structure exists here.)*

**EmergencyContactAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id — defined here since it's Person-specific)
- association_id (PK, FK → EntityAssociation; entity_id_a = this Person, entity_id_b = the contact's Person entity_id, role = relationship)
- no extra fields beyond the base EntityAssociation shape

## States & Transitions

**Employee/Contractor/Occupant:** `active` → `inactive` (independent per TPT level, per item 3–6).

**Visitor:** `checked_in` → `checked_out` (per-visit; a returning visitor's existing Person/Visitor row is reused, not recreated).

**BOLO/Trespass Subject:** follows Entity Registry Core's shared BOLO Flag lifecycle unmodified (`active` → `cleared` | `expired`).

**EmergencyContactAssociation:** follows Entity Registry Core's shared EntityAssociation lifecycle unmodified (`active` → `removed`).

## Integrations

- **Entity Registry Core**: owns the shared TPT identity mechanics (global ID, deduplication, merge), the generic BOLO Flag mechanism that BOLO/Trespass Subject applies, and the base EntityAssociation shape `EmergencyContactAssociation` extends.
- **Party Registry**: owns the base Party Entity Type Person extends, including the shared `identifications[]` and `contact_information` fields this doc's Person schema inherits rather than redefines.
- **Authentication & Authorization**: step-up authentication for BOLO/Trespass creation/clearance (via Entity Registry Core's mechanism); RBAC/ABAC gating of sensitive field visibility.
- **Structured Logging & Audit Trails**: BOLO/Trespass lifecycle events, medical-alert/emergency-contact views, and merges are audit-tier.
- **Emergency Planning (Muster Check-in App)**: signals the active-emergency state that broadens sensitive field visibility.
- **Personnel, Access Control, Subcontractor Management, Facility & Zone Management**: future owners of richer extension-specific fields for Employee, Visitor, Contractor, and Occupant respectively.
- **Activity Registry**: owns the generic Participant association that tracks a person's involvement in any Activity (incident, call, citation, etc.) — Person Registry maintains no parallel structure for this.
- **Entity Relationships & History**: consumes all of the above, including Activity Registry's Participant associations, to build the person's cross-module interaction timeline.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Supervisor+ | Records Admin | Standard User |
|---|---|---|---|---|---|
| Create/view base Person profile (non-sensitive fields) | ✅ | ✅ | ✅ | ✅ | ✅ (per feature RBAC) |
| View sensitive fields — medical alerts, emergency contacts, race/ethnicity, citizenship, identifications (normal ops) | ✅ | ✅ | ❌ (unless also granted) | ✅ | ❌ |
| View medical alerts / emergency contacts specifically (active emergency) | ✅ | ✅ | ✅ (EOC/muster role) | ✅ | ❌ |
| Disable collection of race/ethnicity or citizenship for their tenant | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Create/activate BOLO/Trespass Subject *(per Entity Registry Core's BOLO Flag mechanism)* | ✅ | ✅ | ✅ (own scope, step-up) | ❌ (unless also granted) | ❌ |
| Clear BOLO/Trespass Subject *(per Entity Registry Core's BOLO Flag mechanism)* | ✅ | ✅ | ✅ (own scope, step-up) | ❌ (unless also granted) | ❌ |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ✅ | ❌ |

## Non-Functional / Constraints

- BOLO/Trespass Subject data is high-liability — creation/clearance auditability and step-up enforcement (inherited from Entity Registry Core's shared BOLO Flag mechanism) must be airtight, no exceptions or bypass paths.
- Medical alerts and emergency contact associations must be reliably accessible within the latency budget of an actual emergency response — the automatic visibility-broadening mechanism cannot be slow or fail silently when an emergency event is active.
- Photo storage must meet the same encryption-at-rest and access-audit standards as other sensitive Person fields.
- Race/ethnicity, citizenship, and identification numbers are classified sensitive per Structured Logging & Audit Trails' field-level redaction ruleset — views are audit-logged like any other sensitive field, distinct from (and without) the emergency-broadening exception that applies to medical alerts/emergency contacts.
- The field/workflow separation (item 2a) is the primary control against US-domestic employment-law exposure (EEOC disparate-treatment risk, state sensitive-PI statutes like CPRA) for race/ethnicity specifically: Employee-facing workflows simply don't surface the field by default, keeping it out of contexts where its mere visibility could create litigation risk, without removing it from the data model where BOLO/witness-description use cases legitimately need it. GDPR is not treated as an operative framework here given the platform's US-commercial/DOE-national-lab target market.
- A tenant disabling collection of race/ethnicity or citizenship must not retroactively delete already-collected data without a separate, explicit data-lifecycle action — disabling stops future collection, it isn't a purge.
- WCAG 2.1 / Section 508 accessible profile views and BOLO creation/clearance flows, day one.

## Acceptance Criteria

- [ ] A base Person record supports structured name (with aliases), physical description, identification, citizenship, contact information, medical alerts, and language fluencies, each independently optional.
- [ ] Adding an `EmergencyContactAssociation` requires the contact to be a real Person entity (even a minimal one); there is no free-text fallback path.
- [ ] Removing an `EmergencyContactAssociation` soft-removes it (status=removed), preserving the historical fact that it once existed, rather than deleting the row.
- [ ] Searching by a recorded alias/AKA correctly surfaces the matching Person record, not just a search by legal name.
- [ ] A Tenant Admin disabling race/ethnicity or citizenship collection removes those fields from data-entry forms for their tenant without deleting previously recorded values.
- [ ] Viewing a person's race/ethnicity, citizenship, or identification numbers is audit-logged as a sensitive-field view, and is not included in the emergency-broadening exception that applies to medical alerts/emergency contacts.
- [ ] Race/ethnicity recorded via a BOLO/Trespass Subject workflow is visible on that same person's underlying record if later viewed through a permission that grants sensitive-field access, confirming it's one field on one record, not a duplicated per-extension value.
- [ ] Personnel's Employee create/edit screen (once specified) does not prompt for or display race/ethnicity by default, while Access Control's BOLO-subject creation screen (once specified) does — verifying the field/workflow separation holds in practice.
- [ ] Creating an Employee extension on an existing Visitor-only Person record correctly adds the Employee extension without duplicating the person.
- [ ] A person can carry an active Employee extension while also being tagged as a Participant on an Activity (via Activity Registry), each independently tracked.
- [ ] Creating a BOLO/Trespass Subject without a Supervisor+ permission is denied; creating one without a justification is rejected; creating or clearing one without passing step-up authentication is blocked — verified as inherited behavior from Entity Registry Core's shared mechanism, not reimplemented here.
- [ ] A BOLO/Trespass flag past its expiration date automatically shows as expired without manual intervention.
- [ ] A Standard User without the medical-alert permission cannot view a person's medical alerts during normal operations, but a Supervisor with an EOC/muster role can view them once an emergency event is active, and loses that visibility once the event closes.
- [ ] Tagging a person as a Participant (role "witness") on an Activity, via Activity Registry's mechanism, correctly appears in the person's interaction timeline with no Person-side data structure involved.
- [ ] Two Person records with matching name, DOB, and phone are flagged as a potential duplicate per Entity Registry Core's model, never auto-merged.

## Open Questions

- Exact tenant-configurable default maximum expiration for BOLO/Trespass flags where no statutory requirement applies — to be set via Settings & Preferences during technical spec.
- Richer Employee/Visitor/Contractor/Occupant fields are explicitly deferred to Personnel, Access Control, Subcontractor Management, and Facility & Zone Management's own future docs — this doc intentionally specifies only the core/shared skeleton.
- Structured vs. free-text modeling for medical alerts and language fluencies (e.g., a controlled vocabulary vs. open text) — to be decided during technical spec, balancing structured-data value against real-world data entry practicality.
- Exact event source/mechanism that signals "active emergency" to trigger sensitive-field visibility broadening — to be finalized when Emergency Planning's Muster Check-in App is specified.
- Exact NIEM release/version and precise element names to map to (e.g., current NIEM 5.x/6.x `nc:PersonType` schema specifics) — this doc aligns to NIEM Core's well-established conceptual structure (structured name, physical description, generic identification, associated contact info) but verifying exact element names/versioning against the live NIEM specification is a technical-spec-level task, not resolved here.
- Whether identification category codes (SSN, driver's license, passport, etc.) follow a NIEM-standard code list or a platform-defined one — to be decided during technical spec.
- Exact per-extension-type default field-surfacing rules (which fields each of Employee/Visitor/Contractor/Occupant/BOLO's create/edit workflows prompt for by default) are owned by each extension type's owning module and will be specified in those future docs (Personnel, Access Control, Subcontractor Management, Facility & Zone Management) — this doc establishes only that the separation exists, not the full per-workflow field list.
