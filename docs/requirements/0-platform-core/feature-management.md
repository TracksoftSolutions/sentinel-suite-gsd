# Feature Management

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Feature Management is the shared runtime entitlement-gating engine: the mechanism that turns a tenant's Edition and à-la-carte Module Entitlements (both owned by Tenant Management) into actual on/off and quota behavior at every point in the platform a feature checks "is this tenant allowed to do this." It is deliberately a separate axis from Settings & Preferences — Settings resolves *values* a tenant configured (a retention period, a quiet-hours window); Feature Management resolves *entitlements* the platform granted (whether a capability exists for this tenant at all, and how much of it).

A feature registers a **Feature Flag** the same way features register a Setting Definition against Settings & Preferences — this doc owns the resolution engine and runtime behavior; individual features own what their own flags mean.

## Actors & Roles

- **Platform Super Admin** — manages the Feature Flag registry itself (which flags exist, their type/defaults), assigns Editions and grants overrides.
- **Tenant Admin** — sees their tenant's effective flags/quotas, requests upgrades when blocked or warned.
- **Every platform feature/module** — registers its own Feature Flags against this engine and asks it for resolved entitlement state rather than hardcoding tier logic.

## User Stories

- As a **feature developer**, I want to register a new capability as a Feature Flag once and have edition bundling, à-la-carte override, and runtime gating all come for free, rather than writing my own tier-check logic.
- As a **Tenant Admin on Standard edition**, I want a feature I don't have grayed out with an upgrade prompt, so I know it exists and how to get it.
- As a **Federal Tenant Admin**, I want a compliance-restricted feature (e.g., a cloud AI adaptor) to not appear in my UI at all, rather than being visibly-but-uselessly grayed out, since even showing it could raise questions during an assessment.
- As a **Tenant Admin approaching our Site quota** during an active incident, I want to still be able to add an emergency Site rather than being hard-blocked, with the platform flagging us over-quota for billing follow-up rather than stopping operations.
- As a **Platform Super Admin**, I want to grant a one-off exception flag to a specific tenant (a pilot, a negotiated custom deal) without inventing a whole new Edition for it.

## Functional Requirements

### Flag registration
1. A feature registers a **Feature Flag** of one of two kinds: **boolean** (on/off — a capability either exists for a tenant or doesn't) or **quota** (a numeric limit paired with a **soft cap** and **hard cap**, e.g., "Sites" with soft cap 8 / hard cap 10).
2. Every registration declares an **off-state UI treatment**: `hidden` (feature/action doesn't appear in the UI at all) or `upsell` (visible, disabled, with an upgrade CTA). Per-flag, not platform-wide — a flag's owning feature decides which fits (compliance-sensitive capabilities default to `hidden`; commercial upsell capabilities default to `upsell`).
3. A quota flag's registration includes a **usage query callback**: Feature Management does not track usage itself — it asks the owning module for the tenant's current count against that quota at evaluation time (e.g., Location Registry answers "how many Sites does this tenant have"), keeping the count as a single source of truth in the module that owns the underlying records.

### Resolution
4. A tenant's effective flag state resolves by layering, narrowest wins: an explicit **tenant-level override** (Platform Super Admin grant/exception) → the tenant's assigned **Edition**'s bundled value → the flag's **platform default**.
5. Boolean flags resolve to a simple effective on/off. Quota flags resolve to an effective (soft_cap, hard_cap) pair, evaluated against the current usage count fetched live from the owning module's callback at the time of the check — not cached usage state.

### Quota enforcement
6. Crossing the **soft cap** puts the tenant in an over-quota **warning** state: the action that crossed it still succeeds, the Tenant Admin is notified, and a grace period begins.
7. Crossing the **hard cap** does not block the action either — hard cap is enforced as an extended grace/warning escalation (e.g., stronger notification, billing/ops follow-up), never a synchronous block on the operational action itself. This is a deliberate platform stance: no quota flag may block a create action outright, since the platform cannot know in the moment whether that action is routine or urgent (mid-incident Site standup, emergency headcount add).
8. Grace period length and the escalation behavior at hard-cap (notification tier, whether Billing/Ops is auto-notified) are flag-configurable, not hardcoded platform-wide.

### Overrides & exceptions
9. A Platform Super Admin can grant a **tenant-level override** on any flag (boolean flip, or a custom quota limit) independent of the tenant's Edition — for pilots, negotiated deals, or temporary exceptions — without needing a bespoke Edition.
10. Flag registry changes (new flag added, default changed) and tenant-level overrides are audit-tier events.

## Data Model / Fields

**Feature Flag Definition** (registered by a feature)
- flag_key, owning_feature, kind (boolean, quota)
- default_value (bool, or {soft_cap, hard_cap} for quota)
- off_state_ui_treatment (hidden, upsell)
- usage_query_ref (quota flags only — reference to the owning module's count callback)
- default_grace_period (quota flags only)

**Edition** *(owned by Tenant Management; referenced here)*
- edition_id, bundled_flag_values[] (flag_key → value)

**Tenant Flag Override**
- tenant_id, flag_key, override_value, granted_by, granted_at, reason, expires_at (nullable)

**Tenant Quota State** (derived, not stored authoritatively — evaluated live)
- tenant_id, flag_key, current_usage (fetched live), effective_soft_cap, effective_hard_cap, status (`within_limit`, `over_soft`, `over_hard`), grace_expires_at (nullable)

## States & Transitions

**Quota flag tenant state:** `within_limit` → `over_soft` (soft cap crossed, grace period starts, warning issued) → `over_hard` (hard cap also crossed, escalated notification/billing follow-up) → `within_limit` (usage drops back below soft cap, or an upgrade raises the effective cap).

## Integrations

- **Tenant Management**: source of a tenant's Edition and à-la-carte Module Entitlements, which this engine resolves into bundled flag values (item 4).
- **Settings & Preferences**: the sibling engine for configured *values*; this doc is deliberately the entitlement/quota axis, not a duplicate of Settings' hierarchy-resolution logic.
- **Every quota-flag-owning module** (e.g., Location Registry for Sites, Entity Registry Core for record counts): implements the usage query callback this engine calls at evaluation time.
- **Notifications Engine**: soft/hard cap crossings and off-state upsell-CTA interactions drive tenant/admin notifications.
- **Structured Logging & Audit Trails**: flag registry changes and tenant overrides are audit-tier events.

## Permissions

| Action | Platform Super Admin | Tenant Admin |
|---|---|---|
| Register a new Feature Flag (developer, via feature build) | ✅ | ❌ |
| Assign a tenant's Edition | ✅ | ❌ |
| Grant a tenant-level flag override | ✅ | ❌ |
| View own tenant's effective flags/quota state | ✅ | ✅ (own tenant) |
| Request an upgrade from an `upsell`-treated disabled feature | ✅ | ✅ |

## Non-Functional / Constraints

- Flag resolution must be cheap enough to check on every relevant request/render, same performance bar as Settings & Preferences' effective-value resolution — cacheable per tenant, invalidated on the relevant change event (Edition change, override change).
- A quota flag's usage query callback must not become a request-path bottleneck; the owning module is responsible for making its count cheap to answer (e.g., a maintained counter, not a full table scan), even though Feature Management itself doesn't cache the value.
- `hidden` off-state treatment must be enforced server-side (API/GraphQL layer), not just hidden client-side — a compliance-restricted feature must be unreachable, not merely unlisted.
- No quota flag's hard cap may be wired to block an operational create action synchronously (item 7) — this is a hard platform constraint, not a per-flag choice.

## Acceptance Criteria

- [ ] A boolean flag registered with `upsell` treatment shows as visible-but-disabled with an upgrade CTA for a tenant whose Edition doesn't include it.
- [ ] A boolean flag registered with `hidden` treatment doesn't appear in the UI, and the underlying API/GraphQL action is rejected server-side, for a tenant whose Edition doesn't include it.
- [ ] A tenant crossing a quota flag's soft cap can still complete the action that crossed it, receives a warning notification, and enters `over_soft` state.
- [ ] A tenant crossing a quota flag's hard cap can still complete the action, receives an escalated notification, and enters `over_hard` state — the action is never blocked.
- [ ] A quota flag's current usage is fetched live from the owning module at evaluation time and correctly reflects a count change made moments earlier (no stale cache).
- [ ] A Platform Super Admin grants a tenant-level override on a flag; that tenant's effective value reflects the override regardless of its Edition's bundled value.
- [ ] Every flag registry change and tenant override grant produces an audit-tier event.

## Open Questions

- Exact notification/escalation behavior at `over_hard` (who gets notified, whether it auto-creates a Billing/Ops follow-up task) — likely ties into Background Job Processing for the escalation check itself; to be confirmed once that doc exists.
- Whether quota flags need a per-flag configurable re-evaluation cadence (checked live on every relevant action vs. periodically) for quotas whose usage query is expensive to compute — deferred to technical spec.
- Whether Tenant Admins should be able to see *other* tenants' Edition/flag catalog (e.g., "what's in Professional that I don't have") for self-service upgrade discovery, or only their own effective state — leaning toward exposing the full Edition catalog (a sales/marketing surface, not a security concern), to be confirmed.
