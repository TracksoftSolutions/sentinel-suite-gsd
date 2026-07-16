# Notifications Engine

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

The Notifications Engine is the shared delivery infrastructure for **internal** notifications to platform users (guards, dispatchers, supervisors, admins) — system-generated alerts, task assignments, approvals, reminders, and safety escalations. It owns priority tiering, channel routing, acknowledgment tracking for critical alerts, muting/fatigue controls, message templating, and the in-app notification center.

Explicitly out of scope: Module 17 (Mass Notification & Crisis Communications), which targets an entirely different audience (building occupants, tenants, community cohorts) at a different scale and with different compliance needs (opt-in registers, IVR, translation). Module 17 may reuse this engine's underlying channel infrastructure (SMS/email/push gateways), but its authoring, targeting, and delivery-tracking model is its own feature.

## Actors & Roles

- **Any platform user** — recipient of notifications; configures personal channel/mute preferences within tenant-set limits.
- **Tenant Admin** — configures tenant-wide category defaults, channel minimums for Critical-tier categories, admin-enforced non-mutable categories per role, message template customization, retention settings.
- **Any platform feature/module** — producer of notifications via the shared event pipeline (e.g., Guard Tour firing a missed-checkpoint alert, Personnel firing a certification-expiry reminder).
- **Supervisor / Escalation recipient** — receives escalated Critical-tier alerts when the original recipient fails to acknowledge in time.

## User Stories

- As a **Guard**, I want a missed-checkpoint alert to reach my supervisor immediately across every channel, so a real problem doesn't sit unnoticed in an unread badge.
- As a **Dispatcher**, I want new-call alerts to always come through regardless of my personal notification settings, so I can never accidentally mute something operationally critical to my role.
- As a **Guard**, I want to mute non-urgent shift-reminder notifications during my off-hours quiet period, so I'm not pinged with routine noise while off duty.
- As a **Supervisor**, I want to be automatically notified if a guard doesn't acknowledge a panic alert within a couple minutes, so I know to escalate immediately instead of assuming someone saw it.
- As a **Tenant Admin**, I want to customize the wording of our certification-expiry reminder template to match our internal terminology, so it reads naturally to our staff.
- As a **user with a backgrounded mobile app**, I want push alerts to still arrive via the OS push service, so I don't miss anything just because the app wasn't in the foreground.
- As a **Guard**, I want one clear alert for a missed checkpoint, not a new ping every minute it stays missed, so real alerts don't get lost in noise.

## Functional Requirements

### Priority tiers & escalation
1. Every notification carries one of four priority tiers: **Critical, High, Normal, Low**.
2. **Critical** (e.g., panic button, missed checkpoint, officer safety check-in timeout) bypasses all batching, quiet hours, and per-user mute settings, and is pushed through every enabled channel simultaneously.
3. **High** escalates faster than Normal/Low but still respects hard admin-enforced mute overrides for non-emergency roles (i.e., it can be muted by an admin-level "not applicable to this role" setting, unlike Critical).
4. **Normal** and **Low** follow standard user/tenant routing, muting, quiet-hours, and digest rules.
5. Critical-tier notifications require **explicit acknowledgment** by the recipient (or a defined recipient group, first-to-ack or all-must-ack per category configuration), not just delivery confirmation.
6. If a Critical alert is not acknowledged within a configurable window, the engine automatically escalates: notifying the next tier (e.g., a supervisor) via additional channels, repeating until acknowledged or an admin-defined escalation chain is exhausted.
7. Escalation chains and ack timeout windows are configurable per notification category, with sane platform defaults.

### Channels & routing
8. Supported delivery channels: in-app notification center, mobile push, desktop push, SMS, email, **voice call** *(retrofit, by Pre-Incident Plans — a real outbound-call adaptor, e.g. IVR/telephony, distinct from SMS: text-based channels are explicitly insufficient for a genuine phone-call escalation requirement)*. Voice call is the platform's first Notifications Engine channel requiring a dedicated provider adaptor (per the platform-wide provider-adaptor discipline) rather than a standard push/SMS/email gateway; a tenant with no voice-call adaptor configured simply doesn't have the channel available — consuming features must degrade gracefully (e.g., fall back to a manually-worked list) rather than claim a call was placed when it wasn't.
9. Each notification category ships with a platform-default channel set. Tenant Admins can adjust defaults for their tenant; users can further customize channel preference for Normal/Low (and, where permitted, High) categories.
10. Critical-tier categories carry a **tenant-enforced minimum channel set** that individual users cannot disable.
11. Mobile push is delivered via the standard OS push service (APNs/FCM), independent of the app's foreground/background state or the Offline Data Sync queue — it arrives even if the app itself was recently offline, distinct from that feature's own data-queuing mechanics.

### Muting & fatigue control
12. Users can mute specific notification categories and set personal quiet hours, during which Normal/Low tier notifications are held and delivered as a digest rather than pushed immediately; Critical and (mostly) High still break through per #2–3.
13. Users may opt into a periodic digest (e.g., hourly/daily summary) for Low-tier notifications instead of individual delivery.
14. Tenant Admins can pin specific categories as **always-on, non-mutable** for specific roles (e.g., Dispatchers can never mute new-call alerts), overriding individual user preference even for categories below Critical tier.

### Templates & content
15. Each notification category has a platform-default message template supporting variable substitution (e.g., `{{officer_name}}`, `{{location}}`, `{{checkpoint_name}}`).
16. Tenant Admins can customize template wording/branding per category, per tenant.
17. Templates are English-only at launch, structured (externalized strings, not hardcoded) to support future multi-language localization per the platform roadmap.

### In-app notification center
18. All notifications land in a persistent in-app notification center regardless of which other channels they were also delivered through, marked read/unread.
19. Retention period for the in-app center is tenant-configurable; entries aging out of the center remain permanently available in the audit trail (Structured Logging & Audit Trails) — center retention only affects the user-facing list, not the underlying record.

### Storm control & deduplication
20. The engine deduplicates repeated triggers of the same underlying condition (e.g., one missed checkpoint generates one notification plus its escalation chain, not a repeat every polling interval); re-notification only occurs on genuinely new state (e.g., a second, distinct missed checkpoint, or the original condition re-triggering after being cleared and recurring).

## Data Model / Fields

**Notification Category** (platform + tenant override)
- category_id, name, default_priority
- default_channels[], critical_minimum_channels[] (if priority = Critical)
- default_template, tenant_template_overrides{tenant_id: template}
- ack_required (bool), ack_timeout, escalation_chain[] (ordered recipient roles/individuals)
- mutable_by_user (bool), admin_pinned_roles[] (roles for which this category cannot be muted)

**Notification Instance**
- notification_id, tenant_id, category_id, priority
- recipient(s) (account_id[] or role/group ref)
- trigger_event_ref (source event, for dedup key)
- payload (template variables resolved)
- channels_attempted[], channels_delivered[], delivery_status per channel
- created_at
- ack_status (not_required, pending, acknowledged), acknowledged_by, acknowledged_at
- escalation_state (none, in_progress, exhausted), escalated_to[]

**User Notification Preference**
- account_id, category_id
- muted (bool) — rejected if category is admin-pinned non-mutable for the user's role
- channel_overrides[] (per-channel opt-in/out, within tenant-allowed set)
- quiet_hours (start, end, timezone)
- digest_opt_in (bool), digest_frequency

**Tenant Notification Policy**
- tenant_id
- category_overrides{category_id: {channels, template, ack_timeout, escalation_chain}}
- in_app_retention_period

## States & Transitions

**Notification Instance:** `created` → `dispatching` → per-channel `delivered`/`failed` → (`Critical` only) `ack_pending` → `acknowledged` | `escalated` (on timeout, loops to next escalation-chain recipient) → `resolved`.

**User Notification Preference (mute):** `active` (default) ↔ `muted` (user-toggled) — forced back to `active` and locked if an admin pins the category as non-mutable for the user's role.

## Integrations

- **Structured Logging & Audit Trails**: source events flow in from every module; notification delivery, acknowledgment, and escalation are themselves audit-tier events (already referenced by Authentication, Logging, and Offline Sync docs).
- **Authentication & Authorization**: recipient resolution uses RBAC/data-scope (e.g., "notify the on-duty Supervisor for this site") and ABAC attributes where relevant.
- **Offline Data Sync**: mobile push is independent of the sync queue (#11), but in-app center entries generated while a device was offline sync in through the normal data path.
- **Settings & Preferences**: Network Profiles may influence whether/how push payload size is constrained on low-bandwidth links; the Tenant Notification Policy and User Notification Preference in this doc's data model are registered as Setting Definitions against that feature's shared hierarchical config engine (the admin-pinned non-mutable category pattern is the same mechanism generalized as "locking" there) rather than implemented as standalone override mechanisms.
- **Every consuming module** (Security Operations, Dispatch/CAD, Personnel, Access Control, etc.): each defines its own notification categories (e.g., missed checkpoint, new dispatch call, certification expiring, host arrival) that plug into this shared engine rather than building their own delivery mechanism.
- **Module 17 — Mass Notification & Crisis Communications**: separate system for external/occupant broadcasts; may share channel gateway infrastructure but not this feature's category/ack/escalation model.
- **Pre-Incident Plans**: consumer of the new voice-call channel (#8 retrofit) for its Emergency Notification List's optional automated-escalation mode; also a second, structurally similar consumer of this doc's existing escalation_chain concept, applied to a Preplan-scoped ordered list rather than a role/category-based chain.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Standard User |
|---|---|---|---|
| Configure tenant category defaults (channels, template, ack timeout, escalation chain) | ✅ | ✅ (own tenant) | ❌ |
| Pin a category as non-mutable for a role | ✅ | ✅ (own tenant) | ❌ |
| Set own channel/mute/quiet-hours preferences | ✅ | ✅ | ✅ (own, within tenant limits) |
| Acknowledge a Critical alert addressed to them | ✅ | ✅ | ✅ |
| View escalation history for an alert | ✅ | ✅ (own tenant) | ✅ (own, or if in escalation chain) |
| Set in-app retention period | ✅ | ✅ (own tenant) | ❌ |

## Non-Functional / Constraints

- Critical-tier delivery and escalation must be near-real-time (seconds, not the standard delay budget) — this is the platform's life-safety notification path (panic buttons, missed checkpoints, officer-down timers).
- Deduplication must not suppress a genuinely new occurrence of a recurring condition (e.g., checkpoint missed again after being cleared).
- Push delivery must degrade gracefully on carrier/OS push service outages, with fallback to other tenant-enabled channels for Critical tier.
- WCAG 2.1 / Section 508 accessible in-app notification center and preference UI, day one.
- Template variable substitution must not allow injection into SMS/email content (standard output-encoding discipline).
- Must not violate the offline-capable model: notifications generated by offline-originated events (e.g., an offline-drafted incident later flagged for approval) fire correctly once the triggering event syncs, not lost.

## Acceptance Criteria

- [ ] A Critical-tier alert (simulated missed checkpoint) reaches its recipient through every tenant-enforced minimum channel simultaneously, regardless of that user's personal mute/quiet-hours settings.
- [ ] An unacknowledged Critical alert automatically escalates to the next recipient in the chain after the configured timeout, and this is visible in escalation history.
- [ ] A Dispatcher role with a category pinned as non-mutable cannot mute it, even attempting via their own preference settings.
- [ ] A Guard can mute a Normal-tier category and set quiet hours; during quiet hours, Low-tier notifications arrive as a digest rather than individually, while a Critical alert during the same window still breaks through immediately.
- [ ] A Tenant Admin can edit a category's message template and see the customized wording appear in subsequently generated notifications for their tenant.
- [ ] All notifications, regardless of other channels, appear in the in-app notification center with correct read/unread state.
- [ ] In-app entries older than the configured retention period age out of the visible center while remaining queryable in the audit trail.
- [ ] A missed-checkpoint condition that remains missed across multiple polling cycles produces exactly one notification (plus its escalation chain), not a repeat per cycle; a second, distinct missed checkpoint produces a new notification.
- [ ] A backgrounded mobile app still receives a push notification via the OS push service.
- [ ] An event generated from an offline-originated record correctly fires its associated notification once that record syncs, not before and not lost.

## Open Questions

- Exact default ack-required categories beyond the illustrative examples (panic button, missed checkpoint, officer safety timer) — to be finalized as each consuming feature (Guard Tour, Dispatch/CAD, etc.) is specified.
- Default escalation chain depth and timeout values per category — to be set during technical spec, likely varying by tenant risk tolerance.
- Whether SMS/email channel usage for Critical alerts has cost/rate implications that need tenant-level budgeting or alerting — deferred to technical spec.
- Multi-language template support timeline — deferred to the platform's future internationalization roadmap per the PDD.
- The voice-call channel's actual adaptor (IVR script, telephony provider selection, per-tenant availability/enablement) — this doc only establishes that the channel exists and is adaptor-gated; the adaptor itself is a technical-spec-level undertaking, not committed here.
