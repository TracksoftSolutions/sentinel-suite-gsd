# Authentication & Authorization

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Authentication & Authorization is the foundation service that governs how every human user — internal platform users (employees, contractors, subcontractor staff, admins), client portal users, and mobile/offline clients — proves identity and is granted access to specific features, actions, and data scopes within a tenant. It owns login, MFA, SSO, session management, RBAC, an ABAC policy overlay, break-glass access, and the tenant isolation boundary that everything else in the platform operates inside of.

Authorization is two-layered: RBAC establishes the baseline "can this role touch this feature/action/data-scope" grant; ABAC then evaluates additional attribute conditions (subject, resource, and environmental attributes) at request time and can further narrow — but never widen — what an RBAC grant allows.

Explicitly out of scope: anonymous/unauthenticated visitor kiosk check-in flows (Module 4 — visitors are Master Records subjects, not platform accounts), and service-to-service/API-key auth (owned by the API & Messaging Layer feature, though it shares the same identity/permission model).

## Actors & Roles

- **Platform Super Admin** — Sentinel Suite operator staff; manages tenant provisioning, break-glass oversight, platform-wide security policy.
- **Tenant Admin** (Company-level) — configures the tenant's auth policy (MFA requirement, SSO config, session/lockout policy, isolation tier is platform-assigned but visible to them), creates/edits custom roles, provisions/deprovisions accounts.
- **Site/Region Admin** — scoped subset of Tenant Admin capability, limited to their branch of the org hierarchy.
- **Internal Platform User** (Guard, Supervisor, Dispatcher, Investigator, Safety Officer, etc.) — authenticates to use role-scoped features; may hold role grants across multiple tenants if working as subcontractor staff.
- **Client Portal User** — external stakeholder logging into the Client Portal (Module 15) with a restricted role set.
- **Break-glass Account Holder** — designated individual(s) per tenant authorized to use emergency local-login credentials when SSO/IdP is unavailable.

## User Stories

- As a **Tenant Admin**, I want to require MFA for all users in my tenant and restrict allowed methods to hardware keys only, so I can meet DOE facility security requirements.
- As a **DOE Tenant Admin**, I want to require CAC/PIV smart-card login as the standalone primary authenticator, so we meet federal facility credentialing mandates without layering a separate password on top.
- As a **commercial Tenant Admin**, I want to enable passkeys so my office-building guards can log in with Face ID on their phones, with no password to remember and no separate MFA prompt.
- As a **Tenant Admin at a small client site**, I want to offer magic-link login for a handful of low-risk client-portal users, trading some assurance for zero password-reset support burden.
- As a **Guard on a trusted personal device**, I want the platform to remember my device for a couple weeks after my last MFA challenge, so I'm not re-prompted every single shift.
- As a **Guard**, I want to log in once and have my session persist appropriately on my mobile device, so I'm not repeatedly interrupted during patrol.
- As a **subcontractor Guard** working shifts at three different client sites, I want a single identity with separate role grants per site, so I don't need three logins to remember.
- As a **Tenant Admin**, I want SSO auto-provisioning via SCIM, so new hires in our IdP automatically get Sentinel Suite accounts with the right role.
- As a **Tenant Admin**, I want immediate hard-revoke on termination, so a fired armed guard cannot access the platform or armory workflows one second after separation.
- As a **user who lost their MFA device**, I want a backup-code recovery path, so I'm not locked out and don't have to wait on an admin unless the backup codes are also gone.
- As a **Dispatcher**, I want to be re-prompted for MFA before issuing an armory override, so a hijacked but unattended session can't be used for a sensitive action.
- As a **Platform Super Admin**, I want break-glass accounts tightly audited, so emergency access during an IdP outage can't become a standing backdoor.
- As a **DOE Tenant Admin**, I want access to a case file gated by comparing the user's clearance level against the record's classification, so an otherwise-permitted Investigator role still can't open a file above their clearance.
- As a **Tenant Admin**, I want weapons-related features gated on an active armed-certification attribute, so a guard whose certification lapsed loses access automatically without anyone having to manually change their role.
- As a **Security Manager**, I want to see denied-access attempts caused by failed ABAC conditions in the audit trail, so I can spot a pattern of someone repeatedly trying to reach data outside their clearance.

## Functional Requirements

### Identity & accounts
1. A person's platform identity is a single account linked to their Master Records person profile (Module 0.5).
2. One identity may hold multiple, independently-scoped **tenant role grants** (e.g., subcontractor staff working across client tenants). Each grant is individually assignable and revocable.
3. Accounts are provisioned via: (a) manual creation by a Tenant/Site Admin, or (b) SCIM auto-provisioning from a connected SSO IdP for SSO-enabled tenants.
4. Deprovisioning supports both scheduled deactivation and **immediate hard-revoke** (session kill + credential invalidation within seconds), triggerable manually or via SCIM deprovisioning event. Every revoke is written to the immutable audit trail (integration: Structured Logging & Audit Trails).

### MFA & authenticator methods
5. MFA is **off by default**, fully tenant-configurable: a Tenant Admin can require MFA platform-wide for their tenant and scope the requirement to specific roles.
6. Supported authenticator methods at launch, each tagged with the NIST 800-63B Authenticator Assurance Level (AAL) it provides:
   - **Passkeys / WebAuthn** (device-bound or synced, e.g. Face ID, Windows Hello, security key) — AAL2/AAL3, phishing-resistant, **standalone-capable** (see #8).
   - **Hardware security keys** (FIDO2/WebAuthn, e.g. YubiKey) — AAL3, phishing-resistant, **standalone-capable**.
   - **CAC/PIV smart card** (X.509 certificate via card reader) — AAL3, phishing-resistant, **standalone-capable**; required/mandated outright for many DOE/federal tenants.
   - **TOTP authenticator app** (RFC 6238, e.g. Google Authenticator, Duo) — AAL2, second-factor only.
   - **SMS OTP** — AAL1/AAL2 (NIST discourages for higher AAL due to SIM-swap risk), second-factor only.
   - **Email OTP** — AAL1, second-factor only.
   - **Magic link** (passwordless email click-through) — AAL1, standalone but weakest assurance; opt-in only.
7. Each tenant sets a **minimum required AAL**. Only methods meeting or exceeding that AAL are offered to that tenant's users for enrollment or login; methods below the bar (e.g. SMS/email OTP, magic link for a tenant requiring AAL3) are hidden/disallowed outright, regardless of any role-level MFA requirement. DOE/secure tenants are expected to set AAL3 and typically restrict to hardware key / CAC-PIV / standalone passkeys only.
8. Passkeys, hardware keys, and CAC/PIV are inherently multi-factor (possession + biometric/PIN/certificate) and may be configured as a **standalone primary login**, requiring no separate password. TOTP app, SMS OTP, email OTP, and magic link always function as an explicit **second factor layered on top of a password** — never standalone.
9. A user may enroll multiple methods (subject to the tenant's allowed set) and choose among their enrolled methods at login.
10. At TOTP/backup-code-eligible enrollment, users are issued one-time backup recovery codes. Standalone methods (passkey, hardware key, CAC/PIV) rely on multi-method enrollment or admin-assisted reset for recovery rather than backup codes.
11. If a user loses their authenticator (device, key, or card) and has no working backup codes or alternate enrolled method, a Tenant Admin can reset their MFA enrollment after identity verification; this reset is written to the audit trail and triggers a notification to the user.

### Convenience & friction reduction
11a. Tenant Admins can enable **trusted-device remember-me**: after a successful authenticator challenge, a device can be marked trusted for a tenant-configurable duration (default on the order of weeks), skipping the challenge on subsequent logins from that device until expiry or explicit revocation (e.g., on hard-revoke or user-initiated "sign out all devices").
11b. Tenant Admins can enable **adaptive/risk-based challenge**: rather than prompting on every login, the platform re-challenges only when risk signals are present (new device, new/anomalous location, pattern deviation), leveraging the same anomaly signals as Security Event Monitoring (Structured Logging & Audit Trails). This is independent of and stacks with trusted-device remember-me.
11c. Both mechanisms are off by default and independently toggleable per tenant; a DOE/high-assurance tenant may disable both to require a challenge every login with no leniency, while a convenience-prioritizing commercial tenant can enable both to minimize friction for low-risk logins.
11d. Trusted-device status and any active risk-based relaxation never bypass **step-up authentication** (#20) — sensitive actions always re-challenge regardless of device trust or risk score.

### SSO
9. SSO (SAML 2.0 and OIDC) is configurable per tenant, independently of isolation tier. A Tenant Admin can configure their tenant as: local-login only, SSO-only (local login disabled except break-glass), or hybrid (both available, optionally restricted by role — e.g. SSO required for admins, local allowed for line guards).
10. SSO supports both SP-initiated and IdP-initiated login flows.
11. When SCIM is configured, user provisioning, role/group mapping, and deprovisioning sync from the IdP.

### RBAC
12. Permissions are granted at **feature + action + data-scope** granularity: a grant names a feature (e.g. Incident Reporting), an action within it (create, read, update, approve, delete, export), and a data scope (e.g. own site only, site + region, all sites in tenant) — scope options are driven by the org hierarchy (Company → Region → Site → configurable sub-levels) defined in Facility & Zone Management.
13. The platform ships a set of built-in fixed roles (e.g. Guard, Supervisor, Dispatcher, Investigator, Safety Officer, Tenant Admin) covering common configurations out of the box.
14. Tenant Admins can additionally create **custom roles** by composing permission grants from the full catalog, and assign users to either built-in or custom roles.
15. Role and permission changes are written to the immutable audit trail.

### ABAC (attribute-based access control overlay)
15a. Every RBAC grant may carry additional **attribute conditions** that must evaluate true at request time for the action to proceed; ABAC only narrows an RBAC grant, it never grants access RBAC itself would deny.
15b. Supported attribute sources: **subject attributes** (e.g. clearance level, armed-certification status and currency, training/cert currency, employment status, assigned site/region, on-duty/shift-window status), **resource attributes** (e.g. record classification/sensitivity, record's site/tenant, record owner, record status), and **environmental attributes** (e.g. time of day, on-site vs remote network context, device trust level, whether the record's location falls within the user's currently assigned post/zone).
15c. The platform ships a set of built-in baseline ABAC policies for common compliance-driven cases (e.g. clearance-vs-classification gating for DOE tenants, armed-certification gating on weapons-related actions). Tenant Admins can additionally author their own attribute-based rules scoped to their tenant, built from a defined attribute catalog.
15d. When an ABAC condition fails on an otherwise RBAC-permitted action, the action is **hard-denied**: blocked outright, with a clear reason shown to the user (e.g. "Requires active weapons certification"). The denial is written to the audit trail so patterns of blocked attempts are visible to Security Officers/Tenant Admins.
15e. ABAC attribute values (e.g. clearance level, certification currency) are sourced from their owning feature/registry (Master Records, Personnel licensing/certification features, etc.) — this feature evaluates policies against those values but does not own the underlying attribute data.

### Sessions & credentials
16. Session idle timeout and absolute session length are configurable per tenant (platform default applies if unset; DOE/secure tenants are expected to set stricter values).
17. Concurrent session/device limits per user are configurable per tenant.
18. Accounts lock out temporarily after a configurable number of consecutive failed login attempts; lockout can be cleared by timed cooldown or admin unlock, and is audit-logged.
19. Password rules follow NIST 800-63B guidance (minimum length emphasis over complexity rules, screening against known-breached password lists, no mandatory periodic rotation) for tenants using local passwords.

### Step-up authentication
20. The platform provides a step-up (re-)authentication mechanism (re-prompt for MFA within an active session) that other features can require before allowing a specific action, independent of standard session validity. This feature defines the mechanism; the list of which actions require step-up is defined in each consuming feature's own requirements (e.g. armory issuance, evidence vault access, remote gate override).

### Break-glass access
21. Each tenant may designate a small, tightly limited number of **break-glass accounts** — local-login credentials that bypass SSO (but not MFA) — for use when the tenant's IdP is unavailable.
22. Break-glass account usage (every login, not just configuration) is immutably audit-logged and triggers a notification to Tenant Admins and Platform Super Admins.

### Tenant isolation
23. Each Company-level tenant has a single **isolation tier** classification — full database isolation or logical isolation — set once at tenant creation (by Platform Super Admin, based on client classification, e.g. DOE/secure vs commercial). All Regions/Sites/sub-levels under that tenant inherit the tier; it is not configurable per-site.
24. All queries, background jobs, and integrations must respect the tenant isolation boundary; cross-tenant data access is only possible via explicit, audited tenant role grants (see #2), never implicit.

### Client portal & mobile/offline
25. Client Portal users (Module 15) authenticate through this same system with a restricted role set drawn from the same RBAC model.
26. Mobile clients authenticate once and receive a token that supports continued app access while offline, with token refresh/re-validation on reconnect. Token issuance/lifecycle is owned here; the offline data queuing/sync behavior itself is owned by the Offline Data Sync feature.

## Data Model / Fields

**Account**
- account_id (unique)
- person_id (FK → Master Records Person Registry)
- status (active, deactivated, locked, revoked)
- auth_method (local, sso, hybrid)
- password_hash / salt (if local auth enabled)
- mfa_enrolled (bool), mfa_methods[] (type: passkey/hardware_key/cac_piv/totp_app/sms_otp/email_otp/magic_link, identifier, aal_level, is_standalone, enrolled_at)
- mfa_backup_codes[] (hashed, used/unused)
- trusted_devices[] (device_id, trusted_at, expires_at, revoked_at)
- created_at, created_by, deactivated_at, deactivated_by, revoked_at, revoked_by

**Tenant Role Grant**
- grant_id
- account_id (FK)
- tenant_id (FK)
- role_id (FK → Role, built-in or custom)
- data_scope (hierarchy node reference + depth: e.g. Site X only, Region Y and below)
- granted_at, granted_by, revoked_at, revoked_by, expires_at (nullable, for temporary grants)

**Role**
- role_id
- tenant_id (null for built-in/platform roles, set for custom roles)
- name, description, is_builtin (bool)
- permission_grants[] (feature, action, data_scope_default)

**Session**
- session_id
- account_id
- device_id / client_type (web, mobile, kiosk-n/a)
- created_at, last_active_at, expires_at
- step_up_verified_actions[] (action, verified_at) — tracks step-up state within session

**ABAC Policy**
- policy_id
- tenant_id (null for platform-baseline policies, set for tenant-authored)
- name, description, is_builtin (bool)
- applies_to (feature + action, or role reference, that this policy overlays)
- conditions[] (attribute_source: subject/resource/environmental, attribute_name, operator, comparison_value_or_ref) — e.g. `subject.clearance_level >= resource.classification`
- enabled (bool), created_at, created_by, updated_at, updated_by

**Access Decision Log** (audit-tier, referenced here / owned by Structured Logging & Audit Trails)
- decision_id, account_id, feature, action, resource_ref
- rbac_result (allow/deny), abac_result (allow/deny), abac_policy_id (nullable, which policy caused a deny)
- final_result, timestamp

**Tenant Auth Policy** (per Company tenant)
- tenant_id
- isolation_tier (full_db, logical)
- mfa_required (bool), mfa_required_roles[] (nullable — all roles if empty and mfa_required=true)
- minimum_aal (1, 2, 3) — gates which methods are offered
- trusted_device_enabled (bool), trusted_device_duration
- adaptive_challenge_enabled (bool)
- sso_mode (local_only, sso_only, hybrid), sso_config (SAML/OIDC metadata), scim_enabled (bool)
- session_idle_timeout, session_absolute_timeout, concurrent_session_limit
- lockout_threshold, lockout_cooldown
- breakglass_accounts[] (account_id, designated_by, designated_at)

## States & Transitions

**Account status:** `active` → `locked` (failed-attempt threshold; auto-clears on cooldown or admin unlock → `active`) → `deactivated` (scheduled/manual; reversible by admin → `active`) → `revoked` (hard-revoke on termination; terminal, requires new account to restore).

**Tenant Role Grant:** `active` → `expired` (if expires_at reached, automatic) or `revoked` (manual, immediate). Both terminal; a new grant must be issued to restore access.

**Session:** `active` → `idle-expired` / `absolute-expired` / `revoked` (admin action or account hard-revoke) → terminated. Step-up-verified actions within a session expire independently (short-lived, e.g. re-required after N minutes) even while the session itself remains active.

## Integrations

- **Master Records — Person Registry**: every account links to a canonical person profile.
- **Structured Logging & Audit Trails**: all auth events (login, logout, failed attempt, lockout, role change, break-glass use, MFA reset, hard-revoke) are written as immutable audit events.
- **Notifications Engine**: MFA reset confirmations, break-glass usage alerts, account lockout notices.
- **API & Messaging Layer**: shares the RBAC permission model for API key/service-account scoping, but API key lifecycle is owned by that feature.
- **Offline Data Sync**: consumes tokens issued by this feature for offline session validity.
- **Facility & Zone Management (org hierarchy)**: data-scope options in permission grants are driven by the hierarchy nodes defined there.
- **Client & Contract Management (Client Portal)**: portal logins authenticate through this feature with restricted roles.
- **Settings & Preferences**: the Tenant Auth Policy fields in this doc's data model (MFA requirement, SSO mode, session/lockout policy, minimum AAL, trusted-device/adaptive-challenge toggles) are registered as Setting Definitions against that feature's shared hierarchical config engine rather than implemented as a standalone override mechanism.
- **Personnel (Licensing & Guard Card Tracking, Armed Qualifications Registry, Skills & Capabilities Profiles)**: source of subject attributes (certification currency, clearance level, armed-qual status) evaluated by ABAC policies.
- **Master Records**: source of resource attributes (e.g. record classification) where applicable.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site/Region Admin | Internal User | Client Portal User |
|---|---|---|---|---|---|
| Set tenant isolation tier | ✅ | ❌ (view only) | ❌ | ❌ | ❌ |
| Configure MFA/SSO policy | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Create/edit custom roles | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Provision/deprovision accounts | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ |
| Hard-revoke a session/account | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ |
| Reset own MFA via backup codes | ✅ | ✅ | ✅ | ✅ | ✅ |
| Admin-assisted MFA reset (others) | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ |
| Designate break-glass accounts | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Use break-glass login | ✅ | designated only | designated only | ❌ | ❌ |
| View own audit history | ✅ | ✅ | ✅ | ✅ (own) | ✅ (own) |
| Author/edit tenant ABAC policies | ✅ (+ baseline policies) | ✅ (own tenant) | ❌ | ❌ | ❌ |
| View ABAC denial patterns (audit) | ✅ | ✅ (own tenant) | ✅ (own scope, if granted Audit Viewer) | ❌ | ❌ |

## Non-Functional / Constraints

- Must align with NIST 800-63B (auth) and NIST 800-53 / FISMA control families (access control, audit) for DOE/secure tenants — day-one blocker per PDD.
- CAC/PIV support must align with FIPS 201 / X.509 certificate validation (including revocation checking) for federal tenants.
- Weaker methods (SMS OTP, email OTP, magic link) must never be selectable for a tenant whose configured minimum AAL excludes them — enforced server-side, not just hidden in the UI.
- WCAG 2.1 / Section 508 accessible login and MFA enrollment flows, day one.
- Break-glass and hard-revoke actions must propagate within seconds, not eventual-consistency minutes — security-critical path.
- Tenant isolation enforcement must be verifiable at the data-access layer, not only the application layer (defense in depth for DOE full-DB-isolation tenants).
- White-labeling: tenant-branded login pages for contract security companies (branding config owned by Settings & Preferences; this feature must support rendering it).
- Must function under the offline-capable mobile model: token-based auth that tolerates disconnected periods without forcing re-login mid-shift.

## Acceptance Criteria

- [ ] A Tenant Admin can toggle MFA requirement on/off and scope it to specific roles; enforcement matches configuration on next login.
- [ ] A Tenant Admin sets minimum AAL to 3; SMS OTP, email OTP, TOTP app, and magic link no longer appear as enrollment/login options for that tenant's users, while hardware key, passkey, and CAC/PIV remain available.
- [ ] A user can enroll TOTP or a hardware key, receives backup codes (where applicable), and can recover access via a backup code without admin help.
- [ ] A DOE tenant user logs in using only a CAC/PIV smart card with no separate password prompt, satisfying the full authentication requirement standalone.
- [ ] A commercial tenant user enrolls a passkey and logs in with no password, no separate MFA step.
- [ ] A tenant with magic link enabled allows a user to log in via emailed link alone; a tenant with minimum AAL set above 1 does not offer magic link at all.
- [ ] A user logging in from a device marked trusted within the configured window is not re-challenged; the same user on a new/unrecognized device is challenged even within that window.
- [ ] With adaptive challenge enabled, a login from a typical device/location does not trigger an MFA prompt beyond tenant policy, while a login flagged as anomalous (new geography) does trigger one even on an otherwise-trusted device.
- [ ] A step-up-gated sensitive action still prompts for authentication even when the session is on a trusted device or within an adaptive-challenge relaxation window.
- [ ] An admin can reset a user's MFA enrollment after backup codes are exhausted; action is audit-logged and user is notified.
- [ ] A Tenant Admin can configure SSO as local-only, SSO-only, or hybrid (globally or by role), and both SP- and IdP-initiated login work as configured.
- [ ] SCIM provisioning creates an account with correct role mapping on IdP user creation, and hard-revokes access within seconds of IdP deprovisioning.
- [ ] A subcontractor guard with grants in two different tenants can log in once and see/act only within the scope granted per tenant, with no cross-tenant data leakage.
- [ ] A Tenant Admin can build a custom role from the permission catalog (feature + action + data scope) and assign it to a user.
- [ ] Manual hard-revoke of an account terminates all active sessions within seconds and blocks new logins immediately.
- [ ] Exceeding the configured failed-login threshold locks the account; lockout clears via configured cooldown or admin unlock.
- [ ] A feature that requires step-up auth (e.g., a stubbed sensitive action) successfully re-prompts for MFA mid-session and records the verification with an expiry.
- [ ] A designated break-glass account can log in locally when SSO is simulated as down, and the login generates an immediate audit event and admin notification.
- [ ] A Company tenant's isolation tier (full DB vs logical) is correctly applied to all Sites/Regions beneath it, and cannot be overridden at a lower level.
- [ ] A Client Portal user logs in through the same system with a restricted role and cannot access internal-platform-only features.
- [ ] A user with an RBAC grant that would otherwise permit an action is correctly denied when a baseline ABAC policy's condition fails (e.g., clearance level below record classification), with a clear reason shown and an audit entry recorded.
- [ ] A Tenant Admin can author a custom ABAC policy scoped to their tenant (e.g., gate a feature on an attribute) and it correctly overlays the existing RBAC grant without needing an RBAC change.
- [ ] A user whose armed-certification attribute lapses (per Personnel/Armed Qualifications Registry) is automatically denied weapons-related actions on next attempt, with no manual role change required.
- [ ] Disabling a tenant-authored ABAC policy restores the underlying RBAC grant's original behavior immediately.

## Open Questions

- Exact list of platform-wide built-in roles and their default permission bundles — to be finalized during technical spec / early Security Operations & Personnel feature docs, since roles span multiple modules.
- Which specific actions across the platform require step-up auth — to be decided feature-by-feature as those docs are written (e.g. Weapons Inventory, Digital Evidence Locker, Remote Gate & Barrier Controls).
- FedRAMP-specific auth control deltas (future SaaS tier) — deferred until FedRAMP authorization work begins.
- Air-gapped deployment mode implications for SSO/SCIM (no external IdP reachability) — deferred to the self-hosted/air-gapped deployment technical spec.
- Full attribute catalog (exact list of subject/resource/environmental attributes and their owning features/registries) — to be built out incrementally as Master Records and Personnel features are specified; this doc defines the mechanism and illustrative examples only.
- Baseline ABAC policy set beyond the two illustrative examples (clearance-vs-classification, armed-cert gating) — to be finalized during technical spec in coordination with DOE compliance requirements.
- Performance implications of evaluating ABAC conditions on high-frequency reads (e.g., list views showing many records) — needs a caching/precomputation strategy decided in technical spec.
- CAC/PIV reader/middleware support scope (which OS/browser combinations, mobile CAC reader hardware support) — to be scoped in technical spec against actual DOE client hardware.
- SMS OTP delivery vendor and cost model — deferred to technical spec; also needs a decision on whether SMS OTP is offered at all by default given NIST's discouragement, or gated behind explicit tenant opt-in even at qualifying AAL levels.
- Default trusted-device duration and whether it should vary by role (e.g., shorter for admins than line guards) — to be set during technical spec.
- Magic link expiration window and single-use vs reusable link policy — to be defined during technical spec.
