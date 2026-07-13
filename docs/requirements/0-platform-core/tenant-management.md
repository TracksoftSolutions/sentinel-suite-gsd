# Tenant Management

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Tenant Management owns the tenant as a first-class object: how one comes into existence (self-service or admin-provisioned), what isolation tier it runs under, what edition/plan and à-la-carte modules it's entitled to (in concert with Feature Management), its lifecycle from provisioning through offboarding, and — the mechanism that drove this feature's elicitation — **Client Engagements**, the contractor↔client dual-tenancy model specific to contract security.

Every other Platform Core doc has assumed "tenant" as a given (Authentication & Authorization's tenant role grants, Settings & Preferences' location chain rooted at Tenant, Entity Registry Core's tenant-scoped IDs). This doc is where the tenant object itself — its provisioning, plan, isolation, and lifecycle — is actually specified.

## Actors & Roles

- **Platform Super Admin** — provisions/approves tenants, assigns isolation tier and edition, manages platform-wide tenant lifecycle actions (suspend, offboard override).
- **Tenant Admin** — manages their own tenant's profile, sees their edition/entitlements, requests upgrades or à-la-carte module purchases, initiates self-service offboarding.
- **Contractor Org Admin** — a Tenant Admin acting in the specific context of a contractor tenant that holds one or more active Client Engagements.
- **Client Admin** — a Tenant Admin acting in the specific context of a client tenant (the asset owner) that has engaged one or more contractors.
- **Billing/Ops** (internal Sentinel Suite staff) — handles plan changes, trial conversion, suspension for non-payment; not a platform end-user role.

## User Stories

- As a **prospective commercial customer** (e.g., a single hotel), I want to self-signup and start a trial without waiting on a sales call, so I can evaluate the platform immediately.
- As a **DOE facility's contracting officer**, I want our tenant admin-provisioned with an on-prem deployment target, so procurement/ATO requirements are met before any user ever logs in.
- As a **Platform Super Admin**, I want to assign a tenant's isolation tier (shared DB / dedicated DB / on-prem) at provisioning time and see it reflected everywhere the tenant's data lives, so I never have to reason about it feature-by-feature.
- As a **Tenant Admin**, I want to see my tenant's edition and know exactly which modules/features that unlocks, and purchase additional à-la-carte modules without a support ticket.
- As a **Client Admin** (asset owner) who just switched security contractors, I want to keep seeing every incident, DAR, and patrol record my *previous* contractor's staff generated at my site, so I don't lose institutional history about my own property.
- As a **Contractor Org Admin**, I want my staff's access to a client's site data to end cleanly when the contract ends, while I retain a read-only copy of just the records my own people entered, so I can defend against a billing dispute after the fact.
- As a **Contractor Org Admin** with staff actively assigned to a client site, I want to see that client's site-level scheduling relevant to my crew, without seeing anything about the client's other sites or their other contractors.
- As a **Tenant Admin offboarding entirely**, I want a defined export window before my data goes to cold storage and is eventually purged, so I have time to get everything out under my own control.

## Functional Requirements

### Provisioning
1. A tenant is created one of two ways: **self-service signup** (smaller commercial tenants; starts in `trial`) or **admin-provisioned** (Platform Super Admin, typically for DOE/federal/enterprise tenants that require infrastructure stand-up before any login is possible — starts in `provisioning`).
2. Every tenant is assigned an **isolation tier** at provisioning: `shared` (row-level, default — shared DB/schema, isolated by `tenant_id` + RBAC/ABAC), `dedicated_db` (own database, same application/schema version, for stronger data-residency without full self-hosting), or `on_prem` (fully self-hosted/air-gapped deployment, e.g. DOE facilities). Self-service signup only ever provisions `shared`; `dedicated_db` and `on_prem` are admin-provisioned only.
3. Isolation tier is set once at provisioning and is a platform-level constraint every other module's tenant-scoping must honor (Authentication & Authorization's tenant isolation boundary, Entity Registry Core's tenant-scoped IDs, etc.) — changing tiers post-provisioning is a migration project, not a config toggle, and out of scope for this doc.

### Edition, plan & à-la-carte modules
4. A tenant is assigned an **Edition** (e.g., Standard, Professional, Federal) — a named bundle of Feature Management flags (see Feature Management doc) that sets the tenant's baseline entitlements (a Federal edition, for example, bundles FIPS-only crypto and disables cloud AI adaptors by default).
5. Independent of edition, a tenant can purchase **à-la-carte Module Entitlements** — individual modules/features not included in their edition, added or removed without changing the underlying edition assignment.
6. Both edition assignment and à-la-carte entitlements resolve into Feature Management flags; this doc owns *what a tenant is entitled to*, Feature Management owns *how that entitlement gates behavior at runtime*.

### Lifecycle
7. Tenant lifecycle states: `provisioning` (admin-provisioned tenant, infrastructure being stood up, no login possible yet) → `trial` (self-service, time-boxed, conversion prompt before expiry) → `active` → `suspended` (billing failure or admin action; existing data intact, access locked or read-only per admin choice) → `offboarding` (export window open) → `archived` (cold storage, no live access) → `purged` (terminal, data destroyed).
8. `suspended` can return to `active` (payment resolved); every other forward transition is one-directional.
9. Any lifecycle transition is an audit-tier event (Structured Logging & Audit Trails).

### Offboarding
10. Offboarding opens a defined **export window** (default 90 days) during which the Tenant Admin can self-export all tenant data via the platform's export mechanism.
11. After the export window, data moves to **cold-storage archive** (inaccessible to normal platform access, retained for compliance/legal-hold purposes) for a further retention period (default 90 days), then is **hard-purged**.
12. A tenant with one or more active Client Engagements (below) cannot complete offboarding while those engagements still reference it as the *client* tenant of record without first resolving what happens to engagement data — see Open Questions.

### Client Engagements (contractor↔client dual-tenancy)
13. A **Client Engagement** is a contract-scoped grant between two tenants: a **Client tenant** (the asset owner — tenant of record and canonical data owner) and a **Contractor tenant** (a security company whose staff work the client's site(s)). This is distinct from — and layers on top of — Authentication & Authorization's existing per-user Tenant Role Grant: an Engagement is the org-level contract envelope; individual Contractor staff still each hold their own Tenant Role Grant into the Client tenant, scoped to the Engagement's site(s).
14. **Ownership is unambiguous: the Client tenant is the tenant of record for every record generated under the Engagement** (Incidents, DARs, Patrols, Tickets, etc.) — `tenant_id` on those records is always the Client's, never the Contractor's, regardless of which Contractor's staff authored them.
15. While an Engagement is `active`, access is **bidirectional but scoped to the Engagement's site(s)**: the Client sees everything generated at their site(s) by the current (and any prior) Contractor; the Contractor's staff/admins see operational data scoped to that site — e.g., their own crew's scheduling — but nothing about the Client's other sites or the Client's other Contractor Engagements, and nothing about the Contractor's other Clients is exposed to this Client.
16. **Client read-access to historical records survives the Engagement ending**, as long as the Client remains a Sentinel Suite tenant — a new Contractor's staff (and the Client itself) can see the full site history regardless of who generated which record. This is the whole point of the mechanism: institutional history about the Client's own asset outlives any single vendor relationship.
17. When an Engagement ends (`ended`), the Contractor's staff Tenant Role Grants into that Client are revoked (per Authentication & Authorization's existing revoke mechanism) and live cross-tenant access stops. The Contractor tenant is left with a **Frozen Engagement Copy**: a read-only, point-in-time snapshot containing *only the records the Contractor's own staff authored* during the Engagement (not the Client's full site history) — for the Contractor's own post-engagement purposes (billing disputes, liability defense). The Frozen Copy is scoped per-Engagement, distinct from the platform-wide tenant offboarding export in items 10-11.
18. An Engagement can be re-`active`-d (contractor re-engaged after a gap) or a new Engagement created between the same or different Contractor and the same Client — either way, the Client's continuous record history (item 16) is unaffected either way since it was never contractor-owned.

## Data Model / Fields

**Tenant**
- tenant_id, name, tenant_type (client, contractor, or both — a security company can itself be a Sentinel Suite client for its own internal operations while also holding Contractor-side Engagements with its own clients)
- lifecycle_state (provisioning, trial, active, suspended, offboarding, archived, purged)
- isolation_tier (shared, dedicated_db, on_prem), isolation_tier_set_at, isolation_tier_set_by
- edition_ref, additional_module_entitlements[]
- trial_expires_at (nullable), suspended_reason (nullable), offboarding_started_at (nullable), archived_at (nullable), purge_scheduled_at (nullable)

**Edition**
- edition_id, name (Standard, Professional, Federal, ...), bundled_feature_flags[] (Feature Management keys)

**Module Entitlement** (à-la-carte)
- tenant_id, module_key, granted_at, granted_by, active (bool)

**Client Engagement**
- engagement_id, client_tenant_id, contractor_tenant_id, site_refs[] (Location Registry scope)
- status (active, ended), started_at, ended_at (nullable)
- created_by (Client Admin or Platform Super Admin — contract-authorized party)

**Engagement Staff Grant** (references Authentication & Authorization's Tenant Role Grant, engagement-scoped)
- engagement_id, tenant_role_grant_id

**Frozen Engagement Copy**
- engagement_id, contractor_tenant_id, snapshot_taken_at, record_refs[] (only records authored by this Contractor's staff during the Engagement), storage_location

## States & Transitions

**Tenant:** `provisioning` → `active` (admin-provisioned path) or `trial` → `active` (self-service path, on conversion) → `suspended` ⇄ `active` → `offboarding` → `archived` → `purged`.

**Client Engagement:** `active` → `ended` (Contractor staff grants revoked, Frozen Engagement Copy snapshotted; Client's live record access to Engagement history unaffected).

## Integrations

- **Authentication & Authorization**: Client Engagements layer on top of existing Tenant Role Grants — an Engagement is the contract envelope, individual staff grants remain Auth's mechanism, scoped to the Engagement's sites.
- **Feature Management**: Edition assignment and à-la-carte Module Entitlements both resolve into Feature Management flags; this doc owns entitlement, Feature Management owns runtime gating.
- **Settings & Preferences**: Tenant is the root of the location chain; tenant-level setting defaults apply per the existing hierarchy.
- **Entity Registry Core / Activity Registry / Document Registry**: every tenant-scoped record's visibility resolution must check for an active or historical Client Engagement granting cross-tenant read access, not just the record's own `tenant_id`.
- **Background Job Processing**: offboarding archive/purge transitions and Frozen Engagement Copy snapshots are scheduled/durable background jobs, not synchronous operations.
- **Blob/File Storage**: tenant data export (offboarding) and Frozen Engagement Copy snapshots both produce exported file artifacts through this mechanism.

## Permissions

| Action | Platform Super Admin | Tenant Admin (own tenant) | Contractor Org Admin | Client Admin |
|---|---|---|---|---|
| Provision a tenant (admin path) | ✅ | ❌ | ❌ | ❌ |
| Assign/change isolation tier | ✅ | ❌ | ❌ | ❌ |
| Assign edition | ✅ | ❌ | ❌ | ❌ |
| Purchase à-la-carte module | ✅ | ✅ (own tenant, subject to billing) | ✅ (own tenant) | ✅ (own tenant) |
| Suspend/reinstate a tenant | ✅ | ❌ | ❌ | ❌ |
| Initiate self-service offboarding | ✅ | ✅ (own tenant) | ✅ (own tenant) | ✅ (own tenant) |
| Create a Client Engagement | ✅ | ❌ | ❌ | ✅ (as the client party) |
| End a Client Engagement | ✅ | ❌ | ✅ (own side) | ✅ (own side) |
| View Frozen Engagement Copy | ✅ | ❌ | ✅ (own contractor tenant's copies only) | ❌ (not applicable — client retains live access, not a copy) |

## Non-Functional / Constraints

- Isolation tier enforcement must be structural, not advisory — a `dedicated_db`/`on_prem` tenant's data must be physically incapable of landing in the shared-tier store, by deployment/migration design, not application-layer discipline alone.
- Client Engagement cross-tenant reads must not become a general-purpose cross-tenant query backdoor — every read path is scoped strictly to the Engagement's declared site(s), enforced server-side identically to the ABAC/RBAC model everywhere else.
- Frozen Engagement Copy snapshots must be immutable once taken (no further edits from either party) and independently auditable.
- Offboarding export must cover 100% of tenant-owned data (parity requirement) — a partial export is a defect, not an acceptable gap.

## Acceptance Criteria

- [ ] A self-service signup produces a `trial` tenant on `shared` isolation with no admin intervention; an admin-provisioned DOE tenant starts `provisioning` on `on_prem` and cannot be logged into until moved to `active`.
- [ ] A tenant's edition bundles the correct Feature Management flags on assignment; purchasing an à-la-carte module adds exactly that entitlement without altering the edition assignment.
- [ ] A Client Engagement is created between a Client and Contractor tenant scoped to specific sites; Contractor staff granted under it can access only those sites' data, not the Client's other sites.
- [ ] Ending an Engagement revokes the Contractor staff's live Tenant Role Grants and produces a Frozen Engagement Copy containing only records that Contractor's own staff authored.
- [ ] After an Engagement ends, the Client tenant's users still see 100% of the historical records generated during that Engagement, unchanged, with no indication of data loss.
- [ ] A new Contractor Engaged at the same site sees the full pre-existing site history (owned by the Client), not just records from their own Engagement.
- [ ] A suspended tenant's users cannot access the platform, but the tenant's data is untouched and restorable on reinstatement.
- [ ] An offboarding tenant can export 100% of its data during the export window; after the window and archive retention period elapse, the data is unrecoverable.

## Open Questions

- What happens if a Client tenant itself offboards/purges while it has one or more active Client Engagements — does an active Contractor lose access immediately, or does the Engagement force a resolution step (contract termination confirmation) before the Client's offboarding can proceed? Leaning toward the latter (item 12) but needs confirmation.
- Billing mechanics for edition upgrades and à-la-carte module purchases (proration, approval workflow, self-service checkout vs. sales-assisted) — likely a technical-spec/business concern more than a requirements one; flagged here so it isn't silently assumed.
- Whether a single Contractor tenant can simultaneously be `active` on multiple Client Engagements at overlapping sites for the same Client (e.g., day-shift and night-shift contracts with different vendors) — plausible, not yet confirmed.
- Exact shape of the Frozen Engagement Copy's "record authored by this Contractor's staff" boundary for records with mixed authorship (e.g., an Incident a Contractor guard opened but a Client Admin later added updates to) — likely resolved at the individual field/update-author level rather than the whole record, to be confirmed during technical spec.
