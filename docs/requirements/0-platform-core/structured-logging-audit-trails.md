# Structured Logging & Audit Trails

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

This feature is the platform's foundational event-logging and audit infrastructure: every module emits structured events through a shared pipeline. All events are available as structured JSON for operational/SIEM use; a defined subset — security events, record access, record changes, and bulk exports — are additionally written to an append-only, cryptographically tamper-evident audit store with stricter access control and regulatory retention. It also owns automated security-event/anomaly monitoring that watches this event stream for suspicious activity.

This feature owns the underlying immutability mechanism (append-only storage + hash-chaining). Module 21's "Audit Trail Immutability" feature is a downstream verification dashboard/reporting layer that reads and validates what this feature produces — it does not reimplement tamper-evidence.

## Actors & Roles

- **Platform Super Admin** — platform-wide log infrastructure oversight, cross-tenant investigation access.
- **Tenant Admin** — configures tenant's SIEM integration, sets audit retention (at or above platform floor), designates Audit Viewer role holders.
- **Compliance / Audit Team member** — holds the Audit Viewer permission; reviews and exports audit logs for regulatory purposes.
- **Security Officer / Manager** — receives security-event-monitoring alerts, reviews flagged anomalies.
- **Any platform user** — is the "actor" on most log/audit entries; not a direct consumer of this feature's UI.
- **External SIEM system** — machine consumer of the streamed/exported log data via configured adaptor.

## User Stories

- As a **Tenant Admin**, I want to connect our SIEM tool via a real-time streaming adaptor, so our SOC has near-live visibility into Sentinel Suite security events.
- As a **Compliance Team member**, I want to search and export the immutable audit log for a date range and record type, so I can respond to a DOE audit request.
- As a **Security Officer**, I want to be alerted when a user exports an unusually large volume of records, so I can investigate potential data exfiltration.
- As a **Tenant Admin**, I want to set our audit retention to 7 years to match our contract, exceeding the platform's enforced minimum.
- As a **Platform Super Admin**, I want every record view of a BOLO/trespass subject logged, so we have a defensible access trail if a flagged individual disputes their listing.
- As an **auditor**, I want to verify that no audit entry has been altered since it was written, so I can certify the log's integrity for a compliance review.

## Functional Requirements

### Event pipeline & tiers
1. Every module emits events through a shared platform event pipeline as structured JSON (actor, action, target, timestamp, tenant, source metadata).
2. Events tagged `audit-relevant` are additionally written to a separate append-only audit store, distinct from general operational/application logs (which may be more freely rotated/pruned per standard ops retention).
3. The following event categories are always audit-relevant platform-wide, regardless of module (features may additionally flag their own events as audit-relevant):
   - Authentication & permission events (login, logout, failed login, lockout, role/permission change, break-glass use, MFA reset) — per Authentication & Authorization.
   - Create, update, or delete of any data record, capturing a before/after diff of changed fields.
   - Views/reads of designated sensitive record types (e.g., medical alert fields, BOLO/trespass status, evidence files, weapons/armory custody, government ID numbers).
   - Any report export, bulk download, or data extraction, recording scope and record count.

### Immutability
4. The audit store is append-only at the API level: no update or delete operation exists for audit entries.
5. Each audit entry is cryptographically hash-chained to the prior entry (per tenant partition), so any retroactive alteration or deletion is detectable by hash-chain verification.
6. A verification routine can walk the chain and report the first point of tamper/break, if any, consumable by Module 21's Audit Trail Immutability feature.

### Retention
7. The platform enforces a hard-minimum retention floor for audit-tier entries (aligned to NIST 800-53 / DOE Orders baseline expectations); no tenant configuration can reduce below this floor.
8. Tenant Admins may configure a longer retention period for their tenant to satisfy contract or regulator requirements.
9. Expiration of the retention window triggers archival (not immediate deletion) consistent with the platform's data retention policy; permanent deletion follows a separate, explicitly authorized process.

### SIEM / export integration
10. Tenant Admins can configure one or more SIEM integrations via adaptors for supported platforms; each adaptor's goal is real-time streaming where the target SIEM supports it.
11. Where real-time streaming isn't supported by the target, the adaptor falls back to scheduled batch export.
12. A pull-style export API is also available for ad hoc / on-demand extraction independent of any configured adaptor.

### Security event monitoring
13. The platform continuously evaluates the event stream for:
    - Access pattern anomalies (impossible-travel logins, off-hours/off-pattern access, spikes in record views by one account).
    - Bulk/mass export detection (unusually large exports or bulk downloads relative to the account's baseline).
14. For a defined set of high-confidence triggers, the platform can automatically respond (e.g., force-terminate the session, temporarily suspend the account) in addition to alerting — the specific trigger-to-response mapping is tenant-configurable with sane defaults, and every auto-response is itself an audit-tier event.
15. All triggers, whether alert-only or auto-response, notify designated Security Officers/Tenant Admins via the Notifications Engine.

### Access to audit data
16. Viewing audit logs requires a dedicated "Audit Viewer" permission, distinct from permission to view the underlying records themselves, assignable to any role (built-in or custom) and scoped by the same org-hierarchy data-scope model used elsewhere in RBAC.
17. Platform Super Admins can view audit data across tenants for platform operational/security purposes; this cross-tenant access is itself audit-logged.
18. Every read/export of the audit log itself generates an audit-tier entry (meta-audit), to prevent unaudited snooping of the audit trail.

### PII / sensitive data handling
19. A field-level redaction ruleset defines which data classes (medical information, government ID numbers, biometric templates, and similar) are redacted or tokenized in log/audit payloads by default. The fact that a change occurred and which field changed is still recorded even when the value itself is redacted.
20. All audit log storage is encrypted at rest, independent of field-level redaction.

## Data Model / Fields

**Event** (operational tier — structured JSON)
- event_id, tenant_id, timestamp
- actor (account_id, or "system")
- action (verb, e.g. record.updated, login.failed, export.completed)
- target (entity_type, entity_id)
- source (ip_address, device_id, client_type)
- payload (action-specific structured data)
- audit_relevant (bool)

**Audit Entry** (audit tier — append-only)
- audit_id, tenant_id, timestamp
- event_id (FK to originating event)
- actor, action, target (as above)
- diff (before/after field values, with redaction rules applied)
- prev_hash, entry_hash (chain fields)
- retention_expires_at

**SIEM Integration**
- integration_id, tenant_id
- adaptor_type (vendor/protocol identifier)
- delivery_mode (stream, batch, pull-only)
- endpoint_config (encrypted credentials/connection details)
- status (active, degraded, disabled), last_delivered_at

**Security Monitoring Rule**
- rule_id, tenant_id (or platform-default)
- trigger_type (anomaly pattern, bulk export threshold, custom)
- threshold_config
- response_mode (alert_only, auto_response)
- auto_response_action (nullable: force_logout, suspend_account)

**Retention Policy**
- tenant_id
- audit_retention_period (≥ platform floor)
- operational_log_retention_period

## States & Transitions

**Audit Entry:** `written` → `active` → `archived` (retention window expired) → *(permanent deletion only via separate authorized data-lifecycle process, not part of this feature's normal flow)*. No `edited` or `deleted-by-user` state exists — this is the core immutability guarantee.

**SIEM Integration:** `configuring` → `active` → `degraded` (delivery failures detected) → `disabled` (manual). Degraded state triggers an admin notification.

**Security Monitoring Trigger:** `detected` → `alerted` → (`acknowledged` | `auto-responded`) → `resolved`/`closed`.

## Integrations

- **Authentication & Authorization**: source of the baseline auth/permission audit events; consumes step-up auth for viewing/exporting audit logs on sensitive requests (open question below).
- **Notifications Engine**: delivery channel for security-event alerts and SIEM/retention degradation notices.
- **Master Records**: sensitive-record-view detection references entity types defined there (e.g., Person Registry medical fields, BOLO status).
- **Every other module**: all modules emit events through this shared pipeline; audit-relevant tagging is partly platform-baseline (see #3) and partly feature-specific (each feature's own doc may designate additional audit-relevant actions).
- **Module 21 — Audit Trail Immutability / Compliance Dashboard / Auditor Evidence Vault**: downstream consumers that read, verify, and package this feature's audit data for compliance workflows.
- **Settings & Preferences**: retention policy and SIEM adaptor configuration are surfaced through tenant settings.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Audit Viewer role | Security Officer | Standard User |
|---|---|---|---|---|---|
| View audit logs (own tenant) | ✅ | ✅ | ✅ (scoped) | ❌ (unless also granted) | ❌ |
| View audit logs (cross-tenant) | ✅ (self-audited) | ❌ | ❌ | ❌ | ❌ |
| Export audit logs | ✅ | ✅ | ✅ (scoped) | ❌ (unless also granted) | ❌ |
| Configure SIEM integration | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ |
| Set retention period | ✅ (floor) | ✅ (own tenant, ≥ floor) | ❌ | ❌ | ❌ |
| Configure security monitoring rules | ✅ | ✅ (own tenant) | ❌ | view only | ❌ |
| Receive security event alerts | ✅ | ✅ | ❌ | ✅ | ❌ |
| Trigger chain verification | ✅ | ✅ (own tenant) | ✅ | ❌ | ❌ |

## Non-Functional / Constraints

- Must satisfy NIST 800-53 audit control family (AU-2 through AU-12 range) and DOE Orders 470-series logging expectations for DOE tenants — day-one blocker per PDD.
- Audit write path must not be bypassable by any application code path, including admin tooling and data migrations — enforced at the data-access layer.
- Log volume from record-view auditing (item 3, sensitive reads) must be engineered for cost/performance at scale; this is explicitly accepted as a tradeoff for compliance completeness.
- SIEM streaming must degrade gracefully (queue/retry, not drop) during target SIEM downtime, surfaced as "degraded" status.
- Encryption at rest for audit storage is mandatory regardless of tenant isolation tier.
- WCAG 2.1 / Section 508 accessible audit log viewer UI, day one.
- Air-gapped/self-hosted DOE deployments must be able to operate this feature fully offline from any external SIEM (local-only SIEM adaptor or none), with export still functioning for manual compliance packaging.

## Acceptance Criteria

- [ ] Every module's create/update/delete actions produce an audit entry with an accurate before/after diff.
- [ ] Viewing a designated sensitive record (e.g., a person's medical alert field) produces an audit entry distinct from a change event.
- [ ] A bulk export of records produces an audit entry recording exporter identity, scope, and record count.
- [ ] Attempting to modify or delete an existing audit entry via any code path fails; no such API exists.
- [ ] Running chain verification against an untampered audit log returns valid; against a manually corrupted entry (simulated in test), it correctly identifies the break point.
- [ ] A Tenant Admin can configure a SIEM adaptor and see events arrive in near-real-time on a supported target, or via scheduled batch on an unsupported one.
- [ ] A Tenant Admin can set retention above the platform floor; attempting to set it below the floor is rejected.
- [ ] A simulated bulk-export anomaly and an off-pattern login both generate alerts to designated Security Officers within an acceptable latency window.
- [ ] A configured high-confidence auto-response trigger force-terminates the offending session and logs the auto-response itself as an audit entry.
- [ ] A user with the Audit Viewer permission (but not full Tenant Admin) can view/export audit logs scoped to their assigned data scope only.
- [ ] Reading or exporting the audit log itself generates a meta-audit entry.
- [ ] Redacted fields (e.g., SSN) appear tokenized/redacted in the stored diff while the change-occurred fact and field name remain visible.

## Open Questions

- Exact platform-floor retention duration (e.g., 3 years vs 7 years) — needs a specific regulatory citation decision during technical spec, likely driven by DOE Orders/NIST 800-53 minimums plus general industry practice.
- Should viewing/exporting audit logs itself require step-up authentication (per the mechanism defined in Authentication & Authorization) given its sensitivity? Leaning yes, to be confirmed when that feature's step-up action list is finalized.
- Specific list of "sensitive record types" subject to view-logging beyond the illustrative examples given — to be finalized as Master Records and other data-holding features are specified.
- Named SIEM adaptor targets to support at launch (e.g., Splunk, Microsoft Sentinel, Elastic) — deferred to technical spec / go-to-market prioritization.
