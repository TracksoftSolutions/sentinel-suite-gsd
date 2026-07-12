# Settings & Preferences

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Settings & Preferences is the platform's single hierarchical configuration registry and resolution engine — not just a specific bundle of platform preferences, but the shared mechanism every feature registers its tenant/site/user-configurable settings against, replacing ad hoc per-feature override logic. A feature registers a setting key with a typed schema, its eligible hierarchy levels, and (if applicable) a device-anchored vs. identity-anchored declaration; this feature owns resolving the *effective* value for any given runtime context by walking two independent chains — a **location chain** (Platform → Tenant → configurable sub-units → Site → configurable sub-sites → Device) and an **identity chain** (User → UserDevice) — and merging them.

Earlier feature docs (Authentication & Authorization's Tenant Auth Policy, GIS's GPS Retention Policy, Notifications Engine's Tenant Notification Policy, and others) drafted their own scoped override models before this feature existed; those become registrations against this shared engine rather than one-off implementations. This doc defines the engine; it does not re-litigate what those features' individual settings should contain.

It also owns **Network Profiles**: bundled bandwidth-affecting behavior presets (media sync deferral, compression, tile pre-cache aggressiveness, sync batch frequency, push payload verbosity) for high-cost/low-bandwidth links, auto-selected by detected connection quality with manual pin override.

## Actors & Roles

- **Platform Super Admin** — sets platform-wide defaults, manages the setting registry itself (which features can register what).
- **Tenant Admin** — sets tenant-level values/locks; visible to and inherited by everything under their tenant.
- **Region/Sub-unit Admin, Site Admin, Sub-site Admin** — set values/locks scoped to their own hierarchy node, per their own delegated authority.
- **IT/Systems Administrator** — configures Device-level settings for shared/fixed org-owned devices (kiosks, terminals).
- **Any user** — sets their own User-level preferences and, per-device, UserDevice-level preferences.
- **Every platform feature/module** — registers its own settings against this engine rather than implementing independent override logic.

## User Stories

- As a **Tenant Admin**, I want to set our tenant's default session timeout once and have every Site under us inherit it automatically, without configuring each site individually.
- As a **Site Admin at a remote DOE facility**, I want to lock our Network Profile to "Satellite/Low-Bandwidth" so no individual user's device setting can override it and blow our link budget.
- As a **user**, I want my personal notification quiet hours to apply on my phone regardless of what my site's default is, since it's a personal preference, not an operational requirement.
- As an **IT Admin**, I want a shared lobby kiosk's display brightness and pinned Network Profile to stay fixed no matter who's logged into it, while still respecting each guard's personal language preference when they check in.
- As a **feature developer**, I want to register a new setting (e.g., a retention period) once, declare which hierarchy levels it's eligible at, and get inheritance/override/locking behavior for free, rather than building it myself.
- As a **Tenant Admin**, I want to see, for any given setting, exactly which level currently controls its effective value (inherited from Tenant vs. overridden at Site), so I can debug unexpected behavior.

## Functional Requirements

### Setting registration
1. A feature registers a **Setting Definition**: key, typed schema (with validation rules), default value, the hierarchy levels it's eligible to be set at, and — for settings eligible at both a location level and a personal level — whether it's **device-anchored** (a shared Device's value wins over a logged-in User's for that setting, regardless of who's using it) or **identity-anchored** (the logged-in User's own value wins even on a shared Device).
2. Settings are not required to be eligible at every level — a feature declares only the levels that make sense for that setting (e.g., `isolation_tier` is Tenant-only; `personal_quiet_hours` is User/UserDevice-only; `network_profile` is eligible at Site, Device, User, and UserDevice).

### Hierarchy & resolution
3. The **location chain** runs Platform → Tenant → configurable-depth sub-units → Site → configurable-depth sub-sites → Device, mirroring the org hierarchy defined in Facility & Zone Management, with Device anchored to a specific node in that chain.
4. The **identity chain** runs User → UserDevice, independent of physical location.
5. A setting's **effective value** for a given runtime context (a specific user, on a specific device, in a specific location) resolves by finding the narrowest level with an explicit value set, walking each eligible chain, applying the device-anchored/identity-anchored rule (item 1) to determine which chain takes precedence when both have an explicit value, and otherwise inheriting upward to the next-broader level, ultimately falling back to the platform default.
6. Personal-chain values (User/UserDevice) take precedence over location-chain values by default for identity-anchored settings, unless overridden by a lock (item 7).

### Locking
7. An admin with authority at a given hierarchy level can **lock** a specific setting at that level, preventing every narrower level (sub-units, sites, devices, users) from overriding it — mirroring the admin-pinned-non-mutable pattern already established for Notifications Engine categories, generalized here as the platform-wide mechanism.
8. A locked setting's effective value is the locking level's value for every context beneath it, regardless of any narrower-level value that may still exist in storage (preserved but inert, so unlocking later restores prior personal preferences rather than requiring re-entry).

### Network Profiles
9. A Network Profile bundles bandwidth-affecting behaviors: media/attachment sync deferral threshold, image/video compression level, map tile pre-cache aggressiveness, sync batch frequency, and push payload verbosity.
10. The platform ships preset profiles (e.g., "Satellite/Low-Bandwidth," "Cellular," "Unrestricted/WiFi"); tenants can customize a preset or clone it into their own custom profile.
11. The active profile is auto-selected by the client's detected connection type/quality by default; a Network Profile setting (eligible at Site, Device, User, and UserDevice per the standard resolution model) can pin a specific profile manually, overriding auto-detection.

### Discoverability
12. A settings interface shows, for any given setting, its effective value and which hierarchy level currently controls it (inherited vs. explicitly set vs. locked-from-above), so an admin can debug why a value is what it is without guessing.

### Change management
13. Setting value changes and lock/unlock actions are audit-tier events, consistent with the platform's general configuration-change audit requirements.
14. Setting changes are versioned (prior value, changed by, changed at) — mirroring the versioning pattern already established for Domain Events rules and CLI aliases — so a setting's history is reconstructable and a bad change can be identified and reverted.

## Data Model / Fields

**Setting Definition** (registered by a feature)
- setting_key, owning_feature, schema (type, validation_rules), default_value
- eligible_levels[] (platform, tenant, sub_unit, site, sub_site, device, user, user_device)
- anchor_type (device_anchored, identity_anchored, location_only, identity_only — for settings not eligible at both dimensions)

**Setting Value**
- setting_key, scope_level, scope_ref (tenant_id, site_id, device_id, account_id, etc.)
- value, locked (bool), locked_by (nullable), locked_at (nullable)
- version, version_history[] (value, changed_by, changed_at)

**Device** (shared/fixed org-owned)
- device_id, anchored_location_ref (node in the location chain), name, type (kiosk, terminal, etc.)

**UserDevice**
- user_device_id, account_id, device_descriptor (personal device identifier)

**Network Profile**
- profile_id, tenant_id (null for platform preset), name, is_preset (bool), based_on_preset_id (nullable)
- media_sync_deferral_threshold, compression_level, tile_precache_aggressiveness, sync_batch_frequency, push_payload_verbosity

## States & Transitions

**Setting Value:** `inherited` (no explicit value at this level, resolves from broader) → `explicit` (value set at this level) → `locked` (explicit + locked, blocks narrower overrides) → `unlocked` (returns to `explicit`, narrower-level stored-but-inert values become effective again where narrower than the unlocking level).

**Network Profile (active selection):** `auto_detected` → `pinned` (explicit override at an eligible level) → `auto_detected` (pin removed).

## Integrations

- **Every other module**: registers its own settings against this engine rather than implementing independent tenant/site override logic. Earlier docs (Authentication & Authorization's MFA/SSO/session policy, GIS's GPS retention, Notifications Engine's category defaults, CLI-Style Input's AI-assist toggle) are retroactively understood as Setting Definitions registered here.
- **Facility & Zone Management**: source of the org hierarchy (Tenant → sub-units → Site → sub-sites) the location chain walks.
- **Structured Logging & Audit Trails**: setting changes, lock/unlock actions are audit-tier events.
- **Offline Data Sync, GIS & Mapping Services**: primary consumers of Network Profile behavior (attachment/tile sync deferral).
- **Notifications Engine**: push payload verbosity setting influences notification delivery on constrained links.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Region/Site Admin | IT Admin | User |
|---|---|---|---|---|---|
| Set platform-default values | ✅ | ❌ | ❌ | ❌ | ❌ |
| Set/lock tenant-level values | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Set/lock sub-unit/site/sub-site values | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ |
| Set Device-level values | ✅ | ✅ | ✅ (own scope) | ✅ (own scope) | ❌ |
| Set own User/UserDevice preferences | ✅ | ✅ | ✅ | ✅ | ✅ (own, unless locked above) |
| View effective-value resolution trace | ✅ | ✅ (own tenant) | ✅ (own scope) | ✅ (own scope) | ✅ (own) |
| Register a new Setting Definition (developer, via feature build) | ✅ | ❌ | ❌ | ❌ | ❌ |

## Non-Functional / Constraints

- Resolution must be fast enough to evaluate on every relevant request/render without a noticeable delay — effective values should be cacheable per context, invalidated on the relevant change event.
- Locking must be enforced server-side; a client cannot present or apply a narrower-level value once a broader level has locked that setting.
- The registry itself (which settings exist, their schema/eligible levels) must be inspectable by developers/admins for debugging, consistent with the discoverability requirement.
- Air-gapped/self-hosted DOE deployments must be able to lock Network Profiles and other bandwidth-sensitive settings without any dependency on cloud-based connection-detection services.
- WCAG 2.1 / Section 508 accessible settings UI, day one.

## Acceptance Criteria

- [ ] A Tenant Admin sets a value at Tenant level with no Site-level override; every Site under that tenant resolves to the tenant's value.
- [ ] A Site Admin sets a Site-level override for the same setting; that Site now resolves to its own value while sibling Sites still inherit the Tenant value.
- [ ] A Site Admin locks a setting at Site level; a User attempting to set a personal override for that setting at that site either cannot, or their stored preference is preserved-but-inert and doesn't affect the effective value.
- [ ] Unlocking that setting restores the previously-stored personal preference as effective again, without the user needing to re-enter it.
- [ ] An identity-anchored setting (e.g., personal quiet hours) set at UserDevice level takes precedence over a Site-level value for that user, when not locked.
- [ ] A device-anchored setting (e.g., pinned Network Profile) set at Device level takes precedence over a logged-in User's own value for that setting on that specific shared device.
- [ ] The settings UI correctly shows, for a given setting, whether its effective value is inherited, explicitly set at the current level, or locked from a broader level.
- [ ] A Network Profile auto-selects based on simulated connection quality; pinning a specific profile at an eligible level overrides the auto-detected choice until the pin is removed.
- [ ] Every setting value change and lock/unlock action produces an audit-tier event and a version history entry.
- [ ] A malicious/malformed client request attempting to bypass a server-enforced lock is rejected.

## Open Questions

- Exact platform-shipped default settings and their default values across every registered feature — built out incrementally as each feature specifies its own settings against this engine.
- Whether sub-unit and sub-site depth is unbounded or capped at a practical maximum — to be confirmed alongside Facility & Zone Management's Location Hierarchy Designer.
- Exact caching/invalidation strategy for resolved effective values at scale — deferred to technical spec.
- Whether Network Profile presets need per-deployment-model (SaaS vs. DOE self-hosted) different defaults — likely yes (DOE defaulting to a more conservative profile), to be confirmed during technical spec.
