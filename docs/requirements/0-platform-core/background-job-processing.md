# Background Job Processing

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Background Job Processing is the platform's shared durable job/scheduled-task engine: the mechanism any feature needing work that outlives a single request — a long-running rewrite, a recurring sweep, a deferred escalation check — registers against, rather than inventing its own retry/scheduling logic. It was promoted to Platform Core the same way Tenant-Defined Types and AI/LLM Services were: multiple features had already independently assumed durable background execution existed (Entity Registry Core's merge background-rewrite, Offline Data Sync's outbox replay process) before this doc gave it a real specification, and today's other three Module 0 gap-review features (Feature Management's quota escalation, Blob/File Storage's malware scanning and orphan cleanup) all need it too.

This doc owns job registration, execution, retry/dead-letter behavior, idempotency enforcement, scheduling (one-off and recurring), and tenant isolation-tier-aware execution placement. It does not own what any specific job does — that's the registering feature's responsibility.

## Actors & Roles

- **Platform Super Admin** — monitors the job engine's health, inspects dead-lettered jobs across tenants, manages the job type registry.
- **Tenant Admin** — sees status of jobs relevant to their own tenant (e.g., their tenant's offboarding purge job, their Entity merges in progress).
- **Every platform feature/module** — registers job types against this engine and triggers/schedules instances rather than building bespoke background execution.

## User Stories

- As a **feature developer**, I want to register a job type once and get retry-with-backoff, dead-lettering, idempotency enforcement, and isolation-tier-aware placement for free, rather than reimplementing background execution per feature.
- As a **Platform Super Admin**, I want a failed job that exhausted its retries to land somewhere visible and inspectable, not silently vanish or loop forever.
- As a **DOE Tenant Admin** on `on_prem` isolation, I want every background job touching our data to run entirely on our own infrastructure, never on shared platform compute, mirroring the same guarantee our storage and database already have.
- As a **Tenant Admin**, I want to see that our Entity merge's background rewrite is still in progress and roughly how far along it is, rather than wondering if it's stuck.
- As a **feature developer** registering a recurring sweep (e.g., orphan cleanup), I want the same engine to handle both my one-off triggered jobs and my nightly scheduled job, rather than needing two different mechanisms.

## Functional Requirements

### Registration
1. A feature registers a **Job Type**: a key, the code it invokes, its trigger kind (**one-off** — triggered on demand by an event/action, e.g. Entity Merge's rewrite; or **recurring** — cron-style schedule, e.g. nightly orphan cleanup), and a required **idempotency key template** (how to derive a unique dedup key from the job's input, e.g. `merge:{merge_record_id}` or `orphan_sweep:{tenant_id}:{date}`).
2. Recurring Job Types declare their schedule (cron expression) at registration; one-off Job Types are triggered programmatically by the owning feature (e.g., on a Domain Event, a Command/Action Bus action, or another job's completion).

### Idempotency (platform-enforced)
3. The engine enforces idempotency at the platform level: before executing a triggered job instance, it checks the derived idempotency key against already-executed/in-flight instances for that Job Type and no-ops (returns the prior result/status) if a match exists, rather than trusting each job's own code to self-check. This generalizes the "idempotent and resumable" requirement Entity Registry Core's merge rewrite already stated on its own.
4. A job instance that is interrupted mid-run (process crash, deploy) and re-picked-up resumes under the same idempotency key — the job's own code is responsible for internal checkpointing/resumability logic (item 3 guarantees the engine won't double-trigger it, not that the job's internal logic is automatically resumable).

### Execution, retry, dead-lettering
5. A failed job instance is automatically retried with exponential backoff, up to a Job-Type-configured maximum attempt count.
6. A job instance that exhausts its retries transitions to **dead-lettered**: visible and inspectable (full error history, all attempt timestamps) for manual review, never silently dropped and never retried further without explicit action.
7. A dead-lettered job can be manually re-triggered by a Platform Super Admin (or a role the owning feature designates), which creates a fresh execution attempt under the same idempotency key.

### Tenant isolation-tier-aware placement
8. A job instance touching a specific tenant's data executes within that tenant's isolation tier: `shared`/`dedicated_db` tenants' jobs run on shared platform job-execution compute; `on_prem` tenants' jobs execute entirely within that tenant's own on-premises infrastructure, never dispatched to shared platform compute — mirroring the isolation guarantee already established for storage (Blob/File Storage) and the database itself (Tenant Management).
9. A recurring Job Type that operates tenant-by-tenant (e.g., a nightly sweep) is really N per-tenant job instances under the hood, each independently placed per item 8, not a single cross-tenant execution that would violate an `on_prem` tenant's isolation boundary.

### Observability
10. A job instance's status (queued, running, succeeded, failed-retrying, dead-lettered) and progress (where applicable — e.g., percent-complete for a long-running rewrite) is queryable by the owning feature, so it can surface status to the relevant admin (per Entity Registry Core's Merge Record `status` field, which is exactly this).

## Data Model / Fields

**Job Type Definition** (registered by a feature)
- job_type_key, owning_feature, trigger_kind (one_off, recurring)
- schedule_cron (recurring only)
- idempotency_key_template
- max_retry_attempts, backoff_strategy

**Job Instance**
- job_instance_id, job_type_key, idempotency_key, tenant_id (nullable — some jobs are platform-level, not tenant-scoped)
- status (queued, running, succeeded, failed_retrying, dead_lettered)
- attempt_count, attempt_history[] (attempted_at, outcome, error_detail)
- progress_percent (nullable), execution_placement (shared_compute, tenant_on_prem)
- created_at, completed_at (nullable)

## States & Transitions

**Job Instance:** `queued` → `running` → `succeeded` (terminal) or `failed_retrying` (retry with backoff, returns to `queued`) → after max attempts: `dead_lettered` → (manual re-trigger) `queued`.

## Integrations

- **Entity Registry Core**: the merge background-rewrite (already specified as "idempotent and resumable") is a canonical one-off Job Type against this engine.
- **Offline Data Sync**: the client-side sync engine's replay process is conceptually this pattern; server-side reconciliation work it depends on registers here.
- **Feature Management**: quota over-hard-cap escalation checks and notifications are jobs against this engine.
- **Blob/File Storage**: malware scanning, orphan-cleanup sweeps, and dedup reference-count maintenance are jobs against this engine (scanning is one-off per upload; cleanup is recurring).
- **Tenant Management**: offboarding export-window-expiry → archive, and archive-retention-expiry → purge transitions are scheduled jobs; Frozen Engagement Copy snapshots are one-off jobs triggered on Engagement end.
- **Structured Logging & Audit Trails**: job dead-lettering and manual re-triggers are audit-tier events.
- **Notifications Engine**: dead-lettered jobs relevant to a tenant (e.g., their own offboarding purge failing) notify the relevant admin.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Feature developer (via feature build) |
|---|---|---|---|
| Register a new Job Type | ✅ | ❌ | ✅ |
| View job instance status for own tenant's jobs | ✅ | ✅ (own tenant) | n/a |
| View/inspect a dead-lettered job (any tenant) | ✅ | ❌ | n/a |
| Manually re-trigger a dead-lettered job | ✅ | ❌ (unless owning feature designates) | n/a |

## Non-Functional / Constraints

- Idempotency-key collision checking must be race-safe under concurrent triggers of the same logical job (two near-simultaneous triggers of the same merge rewrite must not both execute).
- `on_prem` tenant job placement must be structurally enforced — a job touching that tenant's data must be undispatchable to shared compute, not merely configured to prefer local execution.
- Dead-lettered jobs must retain full attempt history indefinitely (or per the platform's audit retention standard) for post-incident review, not just the final failure.
- Recurring jobs operating across many tenants must not create a thundering-herd load spike (e.g., every tenant's nightly sweep firing at the exact same instant) — staggering/jitter is a technical-spec concern this doc flags but doesn't fully resolve.

## Acceptance Criteria

- [ ] A one-off Job Type (e.g., a merge rewrite) triggered twice with the same derived idempotency key executes once; the second trigger no-ops against the first's result.
- [ ] A recurring Job Type fires on its declared schedule without manual intervention, producing one Job Instance per applicable tenant.
- [ ] A job instance that fails is retried with increasing backoff delay up to its Job Type's max attempts, then transitions to `dead_lettered` with full attempt history visible.
- [ ] A dead-lettered job manually re-triggered by a Platform Super Admin creates a new execution attempt under the same idempotency key, not a duplicate logical job.
- [ ] A job instance touching an `on_prem` tenant's data executes on that tenant's own infrastructure; the same Job Type touching a `shared`-tier tenant executes on shared platform compute.
- [ ] An owning feature can query a running job instance's progress (where applicable) and surface it to an admin, matching Entity Registry Core's existing Merge Record status display.

## Open Questions

- Exact backoff curve defaults (initial delay, multiplier, cap) — technical-spec concern, likely a sensible platform default with per-Job-Type override.
- Whether recurring job staggering/jitter across tenants needs to be a first-class scheduling feature or is purely an infrastructure-level concern outside this doc's scope — flagged in NFRs, not resolved here.
- Whether platform-level (non-tenant-scoped) jobs need a different observability/permission model than tenant-scoped ones — likely just `tenant_id = null` on the Job Instance with Platform Super Admin-only visibility, to be confirmed during technical spec.
