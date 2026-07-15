# Access Credential Management

## Overview

**Correction carried from elicitation, worth stating plainly up front: this doc is not an attempt to put Sentinel Suite in the credential-management/PACS business.** Per explicit user direction, the platform's value here is the same one Module 19 already established for VMS/alarms — "the single package for all security operations," a unified reporting/operations layer, not a competitor to the vendors that actually control doors. The **primary, flagship mode is multi-vendor ingestion**: a tenant may run CCure, Safelok, HID SAFE, or any other PACS/credential vendor (often more than one, across different sites) — each gets its own adaptor implementation behind the PIAM Adaptor Registration interface (extended here with a `credential_sync` capability, alongside Pre-Registration Portal's `visitor_sync`), pulling that vendor's credential/access-assignment state into one unified **Access Credential** record set regardless of source. **Native/local issuance is a lighter fallback path**, for a site with no real PACS at all — the same object model, just `source_system = sentinel_native`, and it is honest that a native-only credential is a tracked/audited record of who *should* have access, never something that itself opens a door (the platform never speaks door-controller/card-encoding protocols directly, the same boundary Live Camera Feed Ingestion and Alarm Panel Monitors already drew for their own hardware).

Structurally, **Access Credential** registers as an Item extension (full identity/dedup/audit, the same treatment as Vehicle/Camera/Alarm Panel) and **CredentialAssignment** is a new, purpose-built EntityAssociation kind (Person ↔ Credential) rather than a reuse of plain custody — assignment needs fields custody doesn't carry (recertification dates, a forward reference to the next doc's Clearance Profile). This doc is deliberately **access-content-agnostic**: it never models zones, doors, or what a credential actually grants — that's entirely Clearance Profiles' job (next doc), referenced here only as a nullable forward pointer.

## Actors & Roles

- **Security/Access Admin** — reviews and approves new native-provisioning requests, manages Recertification Lapse Policy, configures PIAM Adaptor credential sync.
- **Site/Tenant Admin** — provisions the PIAM Adaptor's `credential_sync` capability per vendor system in use.
- **HR/Personnel Coordinator** *(reference only — Personnel not yet specified)* — the source of Employee status transitions that trigger deprovisioning.
- **Employee/Contractor/Visitor** — the credential holder; sees their own assignment status.
- **Supervisor+** — performs periodic recertification review.

## User Stories

- As a **Security Admin** at a site running CCure, I want every badge CCure already manages to show up in Sentinel Suite automatically, without me re-entering anything, so I have one place to see the whole picture.
- As a **Security Admin** at a small site with no PACS at all, I want to still track who has a physical key-card and its status, even without a real access-control system behind it.
- As an **HR Coordinator terminating an employee**, I want their access credential revoked the same day, automatically, with zero approval delay — that's the whole point.
- As a **Security Admin**, I want a new credential request to require my explicit approval before it's provisioned, since granting new access is the risk to gate.
- As a **Security Admin**, I want to know for certain that a revoked badge's access was actually cut at the door, not just that Sentinel Suite sent the instruction.
- As a **Supervisor**, I want to periodically re-affirm that people who report to me still need the credentials they hold, and I want to configure whether a missed review just alerts someone or actually suspends the credential.

## Functional Requirements

### Access Credential as an Item extension
1. **Access Credential** registers as an Item extension — full identity/dedup/audit treatment, the same as Vehicle/Camera/Alarm Panel — carrying `credential_type` (physical_badge, mobile_credential, pin_code, biometric_ref, other; type-agnostic, since the platform never encodes the underlying hardware/media itself) and `source_system` (`sentinel_native` or the specific PIAM/PACS adaptor that's authoritative for it).
2. **CredentialAssignment** registers as a new EntityAssociation kind (Person ↔ Credential, single-current-value like `CustodyAssociation`'s shape) rather than reusing plain custody directly — it carries fields custody doesn't: recertification tracking (`last_recertified_at`, `next_recertification_due`). *(Retrofit — Clearance Profiles: what a person can access is decoupled from which Credential they carry — a person's Clearance Profile Assignments are their own independent, many-to-many relationship, not a field on CredentialAssignment. The `clearance_profile_ref` originally sketched here as a bare forward pointer is removed in favor of that doc's real mechanism.)*

### Multi-vendor ingestion (primary mode)
3. A tenant may configure **any number of PIAM/PACS Adaptor connections**, each declaring `credential_sync` capability — CCure, Safelok, HID SAFE, and others are all peer adaptor implementations behind the same interface, never a single hardcoded vendor. A site running two different vendor systems across two buildings is a normal, supported case: each ingested Credential records which `source_system` it actually came from.
4. For an ingested Credential, the adaptor's state is authoritative (`credential_authority = external`, extending Pre-Registration Portal's `credential_authority`/`watchlist_authority` shape) — Sentinel Suite mirrors it, it never independently overrides an externally-sourced credential's status. Sentinel Suite never re-derives or second-guesses what the vendor system reports; ingestion failures/staleness are surfaced honestly (a Health Signal Registration entry, per Command Center Wallboard View's established mechanism), never silently assumed current.

### Native provisioning (fallback mode)
5. Where no write-capable adaptor covers a site (or `credential_authority = sentinel_native`), a **Provisioning Request** is the gated path to a new CredentialAssignment: requested (by the future holder, a Supervisor, or an on-behalf-of Admin action) → held at `pending_approval` per a tenant-configurable **Credential Approval Policy** (Settings & Preferences Definition, mirroring Pre-Registration Approval Policy's shape: required approver(s), none/security/admin/both) → approved → the Credential moves to `active` (native-only) or, if a write-capable adaptor exists for that site, a provisioning command is pushed out and the Credential holds at `pending_provisioning_confirmation` until the adaptor confirms success, only then reaching `active` — the same "never silently claim something happened that hasn't been confirmed" honesty already established for revocation (#7).
6. **Provisioning is deliberately gated; deprovisioning is deliberately not** — a genuine, intentional asymmetry per explicit user direction: granting new access is the risk to slow down and review; delaying its removal is the risk that actually matters.

### Deprovisioning (immediate, ungated)
7. A monitored Employee/Contractor/Visitor status transition to `terminated`/`inactive` (Person Registry) fires a **Domain Event** that immediately revokes every active CredentialAssignment that Person holds — zero approval delay, the platform's second deliberate exception to the confirmation-gate default (after SOS Alert's zero-hesitation trigger), because delaying a revocation is the harm, not executing one. The Credential transitions to `revocation_pending_confirmation` — a distinct, honest state — and only reaches `revoked` once the responsible adaptor confirms the physical/logical access was actually cut; a native-only credential (nothing external to confirm) goes straight to `revoked`. Manual revocation (a lost badge, a policy violation) follows the identical immediate, ungated path via an explicit Revoke action.

### Badge template
8. A **Badge Template** (Site/Tenant Admin-configurable, local to this doc) declares which fields render where on a printed badge (photo, name, credential type, expiration, custom logo/color scheme) — closing MODULES.md's "Badge Print Layout Tool." Visitor Kiosk App's own visitor badge is *retrofitted* to render through this same mechanism (a `visitor` Badge Template, distinct from a general employee/contractor template) rather than the fixed layout that doc originally assumed — one configurable rendering mechanism, not two.

### Recertification
9. A tenant-configurable recurring cadence (per credential type or role, another consumer of Background Job Processing's recurring-job registry) prompts the current holder's Supervisor to re-affirm a CredentialAssignment is still needed. Confirming updates `last_recertified_at`/`next_recertification_due`; recertification is deliberately **not** modeled as a governance record layered over a batch (unlike DAR's Shift Review) since it's a single, recurring per-assignment prompt, not a retrospective review of many records at once.
10. A **Recertification Lapse Policy** (Settings & Preferences Definition, tenant-configurable) sets `lapse_action`: `escalate_only` (a new Duration Watchdog instance feeds Critical Event Escalation Policy, the credential stays active — the standard no-compliance-blocking-operations posture) or `auto_suspend` (the Credential transitions to `suspended` once the deadline passes with no confirmation, a deliberate, tenant-opt-in exception given real access-control liability at higher-security tenants) — both configurable, neither is the platform-fixed default.

## Data Model / Fields

**Access Credential** (Item extension; entity_id is the shared PK, FK → Item)
- credential_type (physical_badge, mobile_credential, pin_code, biometric_ref, other)
- source_system (sentinel_native, or a specific PIAM Adaptor's adaptor_type)
- external_credential_ref (nullable — the vendor system's own record identifier)
- status (requested, pending_approval, pending_provisioning_confirmation, active, suspended, revocation_pending_confirmation, revoked, returned)

**CredentialAssignment** (new EntityAssociation kind — entity_id_a = Person, entity_id_b = Credential; association_id is the shared PK)
- assigned_at, removed_at (nullable)
- last_recertified_at (nullable), next_recertification_due (nullable)

**Provisioning Request** (native-fallback path only)
- request_id, requested_for (Person ref), requested_by, site_ref
- status (pending_approval, approved, rejected, provisioned)
- approved_by (nullable), approved_at (nullable)

**Credential Approval Policy** (Settings & Preferences Definition)
- tenant_id/site_id, required_approvers (none, security, admin, both)

**Recertification Lapse Policy** (Settings & Preferences Definition)
- tenant_id/site_id, lapse_action (escalate_only, auto_suspend), recertification_cadence

**Badge Template** (Site/Tenant Admin-configurable, local)
- template_id, tenant_id, applies_to (visitor, employee, contractor, general), field_layout{} (which fields render where), logo_ref (nullable), color_scheme (nullable)

**PIAM Adaptor Registration** *(retrofit — Pre-Registration Portal)*
- sync_capabilities{} gains `credential_sync` (bool); `clearance_sync` reserved for Clearance Profiles to declare
- credential_authority (sentinel_native, external) — per-tenant, mirroring `watchlist_authority`'s shape

## States & Transitions

**Access Credential:** `requested` → `pending_approval` (native path only) → `active` | `pending_provisioning_confirmation` → `active`. Independently, `active` → `suspended` (recertification lapse under `auto_suspend`, or manual) → `active` (Supervisor re-affirms) | `revocation_pending_confirmation` → `revoked` → `returned` (physical media recovered, terminal). An externally-sourced Credential's status is always a mirror of the adaptor's own reported state (#4), never independently driven through this state machine by Sentinel Suite.

**Provisioning Request:** `pending_approval` → `approved` (creates/activates the Credential) | `rejected`.

## Integrations

- **Item Registry**: Access Credential's base identity/dedup/audit treatment, reused wholesale.
- **Entity Registry Core**: CredentialAssignment's base EntityAssociation shape.
- **Person Registry**: the Employee/Contractor/Visitor status transition that fires immediate deprovisioning (#7); a credential holder is always a real Person.
- **PIAM Adaptor (multi-vendor: CCure, Safelok, HID SAFE, others)**: this doc's primary consumer — `credential_sync` capability for ingestion, optional write-path for provisioning/revocation commands where a vendor adaptor supports it.
- **Domain Events**: owns the termination-triggers-immediate-revocation automation, the platform's standard trigger/effect split.
- **Background Job Processing**: recertification's recurring cadence is another consumer of the existing recurring-job registry.
- **Active Call Alerts & Timers (Duration Watchdog) / Critical Event Escalation Policy**: a lapsed recertification under `escalate_only` reuses both, unmodified.
- **Command Center Wallboard View (Health Signal Registration)**: an ingestion adaptor's staleness/failure surfaces here, matching the platform's graceful-degradation posture rather than a silent gap.
- **Clearance Profiles** *(retrofit — built after this doc)*: owns what a person can actually access as its own independent, many-to-many Person↔Profile relationship — this doc never models zones/doors/access levels itself, and no longer carries a `clearance_profile_ref` field at all.
- **Settings & Preferences**: owns Credential Approval Policy, Recertification Lapse Policy.
- **Structured Logging & Audit Trails**: every provisioning approval/rejection, revocation, suspension, and recertification outcome is audit-tier.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Supervisor | Employee/Contractor (self) |
|---|---|---|---|---|
| Configure PIAM Adaptor credential_sync, Credential Approval Policy, Recertification Lapse Policy | ✅ | ❌ | ❌ | ❌ |
| Approve/reject a native Provisioning Request | ✅ | ✅ (per resolved policy) | — | ❌ |
| Manually revoke a credential | ✅ | ✅ | ❌ | ❌ |
| Perform recertification review | ✅ | ✅ | ✅ (own reports) | ❌ |
| View own credential status | ✅ | ✅ | ✅ | ✅ (own only) |

## Non-Functional / Constraints

- An externally-sourced Credential's status is never independently mutated by Sentinel Suite outside of mirroring the adaptor — a discrepancy is surfaced (Health Signal), never silently reconciled by guessing which side is right.
- Deprovisioning's Domain Event trigger must fire without delay on the qualifying Person status transition — this is the doc's single hardest latency expectation, though exact numeric targets are technical-spec-level.
- `pending_provisioning_confirmation` and `revocation_pending_confirmation` must never silently resolve to `active`/`revoked` on a timeout without an explicit adaptor response — an unconfirmed state stays visibly unconfirmed indefinitely rather than being assumed.
- Recertification's `escalate_only` mode never blocks any operational action, consistent with the platform's standing quota/compliance rule.

## Acceptance Criteria

- [ ] A badge already provisioned in a connected CCure (or other adaptor-supported vendor) system appears as an Access Credential with `source_system` set to that adaptor, without manual re-entry.
- [ ] An ingestion adaptor going stale/unreachable surfaces as a Health Signal, never a silent gap in the credential list.
- [ ] At a site with no adaptor, a Provisioning Request held at `pending_approval` under a configured Credential Approval Policy cannot become `active` without the required approver(s) recording approval.
- [ ] An Employee's status transition to `terminated` immediately revokes every active CredentialAssignment they hold, with zero approval-gate delay, regardless of any Credential Approval Policy configured for provisioning.
- [ ] A revocation against a write-capable adaptor shows `revocation_pending_confirmation` until the adaptor confirms, only then `revoked`; a native-only revocation goes straight to `revoked`.
- [ ] Under `auto_suspend`, a lapsed recertification transitions the Credential to `suspended` automatically; under `escalate_only`, the same lapse only raises an escalation and the Credential stays active.
- [ ] CredentialAssignment carries no `clearance_profile_ref` or any other zone/door/access-level field — confirmed entirely access-content-agnostic.
- [ ] Changing an `employee`-scoped Badge Template's field layout is reflected the next time any employee/contractor badge prints; Visitor Kiosk App's badge continues rendering through its own `visitor`-scoped template, confirming one shared mechanism serves both without cross-contamination.
- [ ] Attempting to directly edit an externally-sourced Credential's status through Sentinel Suite (rather than the adaptor) is rejected or clearly flagged as inconsistent with `credential_authority = external`.

## Open Questions

- Exact adaptor connector list at launch (which of CCure/Safelok/HID SAFE/others ship first) — a technical-spec/roadmap decision, not resolved here; this doc's contract is vendor-agnostic by design.
- Whether a site running more than one adaptor ever needs cross-vendor dedup (the same physical person holding a badge in two different vendor systems at two buildings) — likely resolved by Entity Registry Core's existing Person-level dedup once both badges are attached to the same Person, not a new mechanism; not further elaborated here.
- Exact recertification cadence defaults per credential type — technical-spec/tenant-configuration, not platform-fixed.
- Whether `pending_provisioning_confirmation`/`revocation_pending_confirmation` need their own timeout-driven escalation (adaptor never responds) — likely a natural Duration Watchdog instance, flagged as a probable but not yet elicited addition.
