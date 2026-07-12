# Event & Command Bus Architecture

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

This is the internal server-side architecture underpinning every write and read in the platform: a full CQRS (Command Query Responsibility **Segregation**) pattern applied uniformly across all modules, with three clearly differentiated pillars:

- **Command side** — validated write intent (`CreateIncident`, `AssignUnit`) processed by a command handler, producing state changes.
- **Query side** — read intent, served directly from optimized read-models, entirely bypassing the command pipeline (a query never triggers a domain event or a write).
- **Event side** — the immutable record of what happened, published once per successful write and fanned out to every interested subscriber (audit, notifications, real-time push, read-model projections).

Commands and queries are the two directions of traffic through this architecture; events are the connective tissue that keeps read-models, audit, and notifications in sync with writes without direct coupling between features. This differentiation matters specifically because "CQRS" without a clear Query-side story tends to collapse into "just an event bus" — this doc treats all three pillars as first-class.

When a feature performs a write, it publishes exactly one domain event; Structured Logging & Audit Trails, the Notifications Engine, WebSocket topic push, and the webhook dispatcher (all owned by the API & Messaging Layer) are independent subscribers off that same event — no feature separately re-implements "also audit this" or "also notify." Events are well-structured and immutable (versioned schema, causally ordered per aggregate) so that true event sourcing can be adopted later for specific aggregates without rework, even though most aggregates use standard state-based persistence at launch.

This feature covers the raw Command/Query/Event bus infrastructure only. Two related, higher-level concepts — a **Domain Events** feature (business-meaningful events configured to trigger cross-module side effects, e.g. "incident created" cascading into other actions) and a **Command/Action Bus** feature (a reusable, possibly tenant-configurable catalog of invokable actions/automations) — are specified as their own separate features; see [domain-events.md](domain-events.md) and [command-action-bus.md](command-action-bus.md). This doc is the low-level plumbing those two build on top of.

This feature has no direct human end-user — it is infrastructure other features are built on. "Actors" below are developer/system-level; "user stories" are framed as system capabilities.

## Actors & Roles

- **Every platform feature/module** — publishes commands and domain events, and subscribes to events it cares about (as a read-model projector or a side-effect handler).
- **Structured Logging & Audit Trails, Notifications Engine, API & Messaging Layer (WebSocket/webhooks)** — the platform-baseline subscribers present for essentially every event.
- **Platform Super Admin / Ops** — monitors dead-letter queues, bus health, and adapter configuration per deployment.
- **IT/Systems Administrator (self-hosted/DOE)** — configures the distributed bus adapter for on-prem deployments.

## User Stories (system capabilities)

- As a **feature developer**, I want to publish one domain event on a write and have audit logging, notifications, and real-time UI updates all happen automatically, so I don't hand-wire three separate integrations per feature.
- As a **platform operator**, I want a command handler bug that keeps throwing to land in a dead-letter queue after a bounded number of retries, so it doesn't silently block processing or retry forever.
- As a **DOE IT Administrator**, I want the bus to run entirely in-process with no external broker dependency on our air-gapped single-instance deployment, so we have zero external network dependency.
- As a **commercial platform operator**, I want the bus to scale across multiple service instances via a managed cloud service bus, so the platform can handle SaaS-scale load.
- As a **downstream consumer** (e.g., a read-model projector), I want events for the same incident delivered and processed in the order they occurred, so my projection never shows a later state overwritten by an earlier one arriving out of order.

## Functional Requirements

### Command side
1. Every module's writes go through a consistent command → validated write → domain event → read-model projection pattern, applied platform-wide (not selectively), including simpler registry-style features.
2. Authorization (RBAC/ABAC) is enforced at the API/command-handler entry point before a command is accepted onto the bus; once dispatched, a command is already known-authorized, and bus/handler logic focuses on business rules and event production, not permission re-checking.
3. Command validation failures (business-rule rejection, not authorization) return a clear error to the caller synchronously; they do not silently fail asynchronously.
3a. Commands are dispatched through a single, uniform in-process **command bus/mediator** (e.g., a .NET mediator pattern) that routes each command type to exactly one handler — callers never invoke handlers directly, keeping the dispatch mechanism consistent and interceptable (for logging, validation, authorization) platform-wide.

### Query side
3b. Reads are served by a parallel, equally uniform **query bus/mediator**: every query type is routed to exactly one query handler that reads from a read-model, never from replaying events or touching command-side write logic.
3c. Read-models are independent, denormalized-as-needed projections optimized for their specific query (e.g., a dispatcher's "active units" board is a different read-model than an audit report over the same underlying entities), built and kept current by subscribing to domain events (see Event side, below).
3d. Query-side RBAC/ABAC enforcement happens at the query handler: a query never returns data outside the caller's data-scope/attribute-gated permissions, applied as a filter on the read-model, not as a post-hoc client-side filter.
3e. Queries are read-only by construction — a query handler cannot publish commands or domain events; any handler found producing a side effect is a modeling error, not a supported pattern.

### Event side (publication & fan-out)
4. A successful write publishes exactly one domain event capturing what happened (aggregate type/id, event type, payload, causally-ordered sequence number, timestamp, actor).
5. Structured Logging & Audit Trails, the Notifications Engine, and the API & Messaging Layer's WebSocket/webhook dispatchers subscribe to this same event stream as independent consumers — none of them are invoked via direct feature-to-feature service calls for this purpose.
6. Individual features may additionally subscribe to events from other features to build their own read-model projections (e.g., a KPI dashboard projecting from Incident and Dispatch events) without coupling to those features' internal write logic.

### Bus architecture & deployment adaptation
7. A high-performance in-memory bus handles same-process pub/sub in every deployment.
8. For multi-instance/scaled deployments, a distributed adapter layers on top of the in-memory bus behind a common publish/subscribe interface: a managed cloud service bus for commercial SaaS, self-hosted RabbitMQ or Kafka for DOE on-prem Docker deployments.
9. Single-instance air-gapped installs run on the in-memory bus alone, with no external broker dependency.
10. Application/feature code publishes and subscribes against the common interface regardless of which adapter is active underneath — swapping deployment models does not require feature-level code changes.

### Delivery guarantees
11. Events for the same aggregate are delivered and processed in order; ordering across different aggregates is not guaranteed or required.
12. The bus guarantees at-least-once delivery; consumers must be idempotent (safely reprocessable without double-effect, e.g., not sending a duplicate notification for a redelivered event).
13. A command or event that repeatedly fails processing is moved to a dead-letter queue after a bounded retry count, surfaced to platform operations for investigation, rather than blocking the queue or retrying indefinitely.

### Event sourcing readiness
14. Every domain event follows a versioned, immutable schema and is causally ordered per aggregate, making it suitable as a future event-sourced record even where not currently used that way.
15. Most aggregates use standard state-based persistence, with events published alongside (not derived from) each write, at launch.
16. A defined small set of especially compliance-critical aggregates (e.g., incident lifecycle, chain-of-custody records) may adopt true event-sourced storage — state reconstructed by replaying their event stream — where full reconstructable history has clear compliance value; this set is determined per-feature as those features are specified.

## Data Model / Fields

**Domain Event**
- event_id, tenant_id, aggregate_type, aggregate_id
- sequence_number (per-aggregate, strictly increasing)
- event_type, schema_version, payload
- actor (account_id or "system"), timestamp
- causation_id (originating command), correlation_id (traces a chain of related events)

**Command**
- command_id, tenant_id, command_type, payload
- issued_by (account_id), issued_at
- authorization_context (pre-validated RBAC/ABAC decision reference)
- result (accepted, rejected, failed)

**Query** (not persisted long-term; shown for symmetry with Command)
- query_type, parameters
- issued_by (account_id), data_scope_context (RBAC/ABAC filter applied at the read-model)
- target_read_model

**Read Model**
- read_model_id, name, owning_feature
- source_events[] (event types it projects from)
- schema_version, last_projected_sequence (per source aggregate, for projection catch-up/lag tracking)

**Dead-Letter Entry**
- entry_id, original_message_type (command or event), payload
- failure_reason, retry_count, first_failed_at, last_failed_at
- status (pending_review, resolved, discarded)

**Bus Adapter Config** (per deployment)
- deployment_id, adapter_type (in_memory_only, cloud_service_bus, rabbitmq, kafka)
- connection_config (encrypted, if applicable)

## States & Transitions

**Command:** `received` → `authorized` (pre-checked, per #2) → `processing` → `accepted` (event published) | `rejected` (business-rule validation failure, synchronous error to caller) | `failed` (unexpected error, retried per #13).

**Event delivery (per consumer):** `published` → `delivered` → `processed` (consumer ack) | `failed` (retried with backoff) → `dead-lettered` (retry limit exhausted).

**Read Model (projection lag):** `current` (projection caught up to latest source event) → `lagging` (behind by more than a defined threshold, surfaced to ops) → `current` (catches up). A read-model is never in a "processing a query" state distinct from `current`/`lagging` — queries always read whatever the projection's present state is, staleness is bounded but not blocking.

## Integrations

- **Structured Logging & Audit Trails**: baseline subscriber to every domain event; determines which events are audit-tier per its own defined taxonomy.
- **Notifications Engine**: baseline subscriber; each feature's notification categories map to specific event types.
- **API & Messaging Layer**: WebSocket topic push and webhook dispatch are both subscribers off this bus, not separate event sources; this is the internal mechanism those external-facing features read from.
- **Authentication & Authorization**: owns the pre-dispatch authorization check that gates command acceptance.
- **Every other module**: publishes domain events for its own writes and may subscribe to others' events for cross-feature read-model projections (e.g., KPI/Performance Reporting, Master Records interaction timelines).

## Permissions

Not directly applicable — this is internal infrastructure with no user-facing permission surface. Access to dead-letter queue monitoring and bus adapter configuration is Platform Super Admin / Ops only; no tenant-level actor interacts with this feature directly.

## Non-Functional / Constraints

- Must sustain platform-wide write throughput without becoming a bottleneck, particularly for high-frequency sources like GIS position ingestion.
- Per-aggregate ordering must hold even across adapter swaps (in-memory vs distributed) and during adapter failover.
- At-least-once delivery means every consumer across the entire platform must be built idempotently from day one — this is a cross-cutting engineering discipline requirement, not just a nice-to-have for this feature alone.
- Dead-letter entries must never be silently discarded — they require explicit operator review/resolution, since a poison message could represent a missed audit event or notification.
- Air-gapped in-memory-only mode must not lose events on process restart in a way that violates audit completeness guarantees (durable local persistence of at-least the outbox until confirmed delivered).
- .NET/ASP.NET Core implementation, consistent with the platform-wide backend decision (see [_DECISIONS.md](../_DECISIONS.md)).

## Acceptance Criteria

- [ ] A write in any module publishes exactly one domain event, and Audit Trails, Notifications, and WebSocket/webhook dispatch all receive and process it independently without the originating feature calling any of them directly.
- [ ] A command submitted without sufficient RBAC/ABAC authorization is rejected at the API layer before ever reaching a command handler.
- [ ] A business-rule validation failure (e.g., invalid state transition) returns a synchronous, clear error to the caller rather than failing silently downstream.
- [ ] Two events for the same aggregate are always processed by a given consumer in their original order, even under simulated redelivery/retry.
- [ ] A consumer that processes the same event twice (simulated redelivery) produces no duplicate side effect (e.g., no duplicate notification sent).
- [ ] A handler that throws on every attempt is retried a bounded number of times, then lands in the dead-letter queue with the failure reason recorded, and does not block subsequent unrelated messages.
- [ ] The same feature code runs unmodified against the in-memory-only adapter (simulated air-gapped single instance) and against a distributed adapter (simulated multi-instance), differing only in deployment configuration.
- [ ] A designated event-sourced aggregate's current state can be correctly reconstructed by replaying its event stream from scratch.
- [ ] A query handler executes entirely against its read-model with no command dispatch or event publication occurring as a side effect, verified by asserting zero commands/events emitted during a read-only query.
- [ ] Two users with different data-scope grants querying the same read-model receive correctly filtered, different result sets — scope enforcement happens at the query handler, not the client.
- [ ] A read-model's projection lag (time between a source event publishing and the read-model reflecting it) stays within a defined threshold under normal load, and lag beyond threshold is surfaced to operations.

## Open Questions

- Exact list of aggregates adopting true event sourcing (beyond illustrative examples like incident lifecycle and chain-of-custody) — to be decided per-feature as those docs are written, guided by compliance value.
- Dead-letter retry count and backoff parameters — to be tuned during technical spec.
- Specific managed cloud service bus product for commercial SaaS (e.g., a specific vendor's offering) — deferred to technical spec / infrastructure decisions.
- Outbox durability mechanism for the air-gapped in-memory-only mode (e.g., local durable queue backing the in-memory bus) — to be designed during technical spec to satisfy the audit-completeness constraint above.
