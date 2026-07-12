# API & Messaging Layer

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

The API & Messaging Layer is the platform's external-facing interface: GraphQL for the internal web/mobile frontend, versioned REST for external integrations, WebSocket topic subscriptions for real-time push (dispatch updates, EOC dashboards), and self-service API keys/webhooks for third-party tooling. It sits on top of — and is a distinct layer from — the internal Event & Command Bus Architecture (server-side CQRS mechanism this layer's resolvers/handlers publish to and read from) and Module 19's Physical Security Integration Gateway (hardware protocol adapters, which may reuse this layer's webhook/API infrastructure for outbound notification only).

Per platform-wide architectural decision (see [_DECISIONS.md](../_DECISIONS.md)), the backend is built on .NET, chosen to ease FedRAMP acceptance and NIST/FIPS compliance; REST/GraphQL conventions and versioning follow standard ASP.NET Core idioms.

## Actors & Roles

- **Internal web/mobile frontend** — primary GraphQL consumer.
- **External integrator / third-party tool** — REST API and webhook consumer, authenticated via API key/service account.
- **Tenant Admin** — creates/manages API keys, webhook subscriptions, and views the developer portal for their tenant.
- **Platform Super Admin** — manages platform-wide rate-limit tiers, published API documentation, and cross-tenant API health monitoring.
- **Dispatch/EOC frontend clients** — WebSocket topic subscribers for real-time updates.

## User Stories

- As a **Tenant Admin**, I want to create an API key scoped to only the permissions our HR integration needs, so a compromised key can't touch anything else.
- As a **Tenant Admin**, I want a reminder before my API key expires, so our integration doesn't break silently on rotation day.
- As an **external integrator**, I want to subscribe to `incident.created` and `dispatch.status_changed` webhook events, so my SIEM/ticketing tool stays in sync without polling.
- As an **external integrator**, I want webhook payloads signed, so I can verify they actually came from Sentinel Suite and not a spoofed request.
- As a **Dispatcher**, I want the CAD console to update in real time via WebSocket the moment a unit's status changes, without polling.
- As an **external developer**, I want to browse a published OpenAPI spec and try sample REST calls, so I can build an integration without a support ticket.
- As a **Platform Super Admin**, I want a noisy integration to be rate-limited rather than degrading the platform for everyone else.
- As a **Tenant Admin**, I want to instantly revoke a compromised API key and see the revocation take effect within seconds.

## Functional Requirements

### API surfaces
1. The internal web/mobile frontend consumes a GraphQL API.
2. External integrations and third-party tooling consume a versioned REST API.
3. Both surfaces execute against the same underlying business logic, permission (RBAC/ABAC), and tenant-isolation layer — no divergent logic between the two.
4. REST API versioning follows a clear, documented scheme (e.g., URL path versioning) so breaking changes don't silently affect existing integrators.

### API keys / service accounts
5. Tenant Admins can self-service create API keys/service accounts through the admin console, each scoped to a specific subset of RBAC (and applicable ABAC) permissions — never exceeding what the creating admin themselves holds.
6. API keys have a maximum lifetime (tenant-configurable, platform-capped); the platform proactively notifies admins ahead of expiry via the Notifications Engine rather than allowing indefinitely-lived keys.
7. Any API key can be revoked instantly by an authorized admin, taking effect within seconds; key creation, use pattern anomalies, and revocation are audit-tier events (mirroring human-account hard-revoke behavior in Authentication & Authorization).

### Webhooks
8. The platform publishes a defined catalog of webhook-eligible events (e.g., `incident.created`, `dispatch.status_changed`, `checkpoint.missed`); tenants register an endpoint and subscribe to specific event types rather than receiving every event.
9. Every webhook delivery is HMAC-signed with a per-subscription secret so the receiving endpoint can verify authenticity.
10. Failed deliveries retry with exponential backoff up to a defined limit; a subscription that keeps failing is automatically disabled and the tenant is notified, rather than retrying indefinitely.

### WebSockets
11. Real-time push uses a topic/channel subscription model over a persistent connection (e.g., `dispatch:site-42`, `eoc:tenant-dashboard`).
12. The server pushes only events the connected user's current RBAC/ABAC grants permit, re-evaluated on each push — never a broader firehose relying on client-side filtering.

### Rate limiting
13. Each API key carries a default rate limit (requests/minute); Tenant Admins can request higher limits for high-volume integrations, subject to platform approval/tiering.
14. Rate-limited requests receive a clear `429` response with a retry-after indicator.

### Developer documentation
15. REST endpoints are documented via an auto-generated, published OpenAPI specification; the GraphQL schema is introspectable with generated documentation.
16. This documentation is accessible through a self-service developer portal, browsable by Tenant Admins/integrators without requiring platform-team involvement to get started.

### Tenant isolation at the API layer
17. Every API key, WebSocket connection, and webhook subscription is bound to exactly one tenant context (or, for a multi-tenant identity per the Authentication feature's cross-tenant identity model, one tenant per credential).
18. The server derives tenant context from the authenticated credential itself, never from a client-supplied parameter — preventing a compromised or buggy client from requesting another tenant's data by altering an ID.

## Data Model / Fields

**API Key / Service Account**
- key_id, tenant_id, name, created_by, created_at
- permission_scope[] (feature, action, data_scope — subset of creator's own grants)
- secret_hash, expires_at, last_used_at
- status (active, revoked, expired)
- rate_limit_tier

**Webhook Subscription**
- subscription_id, tenant_id, endpoint_url, secret (for HMAC signing)
- subscribed_events[] (from the platform event catalog)
- status (active, disabled_on_failure, manually_disabled)
- consecutive_failure_count, last_delivery_attempt_at, last_success_at

**Webhook Delivery Attempt**
- attempt_id, subscription_id, event_id, timestamp, response_status, retry_count

**WebSocket Subscription** (connection-scoped, not persisted long-term)
- connection_id, account_id, tenant_id, subscribed_topics[]

**Rate Limit Tier**
- tier_id, requests_per_minute, applies_to (default, or specific key_id override)

## States & Transitions

**API Key:** `active` → `expiring-soon` (notification window) → `expired` (auto, no longer usable) | `revoked` (manual, immediate, terminal).

**Webhook Subscription:** `active` → `disabled_on_failure` (after consecutive-failure threshold, auto, with tenant notification) → `active` (manual re-enable after fix) | `manually_disabled` (admin action, terminal until re-enabled).

## Integrations

- **Authentication & Authorization**: source of the RBAC/ABAC permission model API keys are scoped against; owns the identity concept, this feature owns key lifecycle and request handling.
- **Notifications Engine**: API key expiry reminders, webhook auto-disable notices, rate-limit tier change confirmations.
- **Structured Logging & Audit Trails**: API key creation/use/revocation and webhook subscription changes are audit-tier events.
- **Event & Command Bus Architecture** (internal): this layer's resolvers/handlers publish to and read from the internal bus; external consumers never touch the bus directly.
- **Module 19 — Physical Security Integration Gateway**: may reuse this feature's webhook/API key infrastructure for outbound notification of hardware events, but owns its own inbound protocol adapters separately.
- **Every module that defines webhook-eligible events**: each feature's own doc designates which of its events are added to the webhook catalog.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Standard User |
|---|---|---|---|
| Create/scope an API key | ✅ | ✅ (own tenant, ≤ own grants) | ❌ |
| Revoke an API key | ✅ | ✅ (own tenant) | ❌ |
| Register/manage webhook subscriptions | ✅ | ✅ (own tenant) | ❌ |
| Request elevated rate-limit tier | ✅ (approves) | ✅ (own tenant, requests) | ❌ |
| Browse developer portal / API docs | ✅ | ✅ | ✅ (read-only, if granted) |
| Subscribe to WebSocket topics (frontend use) | ✅ | ✅ | ✅ (scoped to own grants) |

## Non-Functional / Constraints

- .NET/ASP.NET Core backend, chosen for FedRAMP/NIST/FIPS alignment — cryptographic operations (HMAC signing, key hashing) must use FIPS-validated modules for DOE/secure tenants.
- Tenant isolation enforcement at this layer must be defense-in-depth with the data-access layer's own isolation checks (never rely solely on the API layer).
- WebSocket push must not leak data across a permission boundary even transiently (e.g., a race between a permission change and an in-flight push).
- Rate limiting must protect shared platform infrastructure without being so aggressive it breaks legitimate high-volume integrations (EOC dashboards, SIEM streaming) — tiering exists specifically to accommodate this.
- Developer portal and published API docs must not expose tenant-specific configuration or data — schema/docs are generic, not tenant-instantiated.
- Air-gapped/self-hosted DOE deployments must be able to run the full API layer with no dependency on an externally-hosted developer portal (self-hostable docs).

## Acceptance Criteria

- [ ] A Tenant Admin creates an API key scoped to a specific permission subset; the key cannot perform any action outside that subset even if the creating admin has broader access.
- [ ] An API key nearing its configured expiry triggers a notification to the Tenant Admin ahead of time.
- [ ] Revoking an API key blocks all subsequent requests using it within seconds.
- [ ] A registered webhook subscription receives correctly HMAC-signed payloads for its subscribed event types only.
- [ ] A webhook endpoint that fails repeatedly is automatically disabled after the configured failure threshold, and the tenant is notified.
- [ ] A WebSocket client subscribed to a topic only receives events its current permissions allow; revoking a permission stops further pushes of that data without requiring a reconnect.
- [ ] Exceeding a key's rate limit returns a 429 with a retry-after value; requests resume succeeding after the window.
- [ ] An external developer can retrieve the OpenAPI spec and GraphQL schema from the developer portal without platform-team assistance.
- [ ] A request made with tenant-A's API key cannot retrieve tenant-B's data even if a tenant identifier is manipulated in the request payload.
- [ ] A multi-tenant subcontractor identity's API credential for tenant A cannot be used to access tenant B's data, consistent with the per-tenant credential binding model.

## Open Questions

- Exact REST versioning scheme (URL path vs header-based) — to be finalized in technical spec per .NET/ASP.NET Core conventions.
- Full webhook event catalog — built out incrementally as each feature doc specifies its own webhook-eligible events.
- Default and maximum rate-limit tier values — to be set during technical spec based on expected integration load patterns.
- Whether the developer portal supports tenant-specific webhook/API key management UI directly, or that lives in the general admin console with the portal being docs-only — to be resolved when the admin console UI is specced.
