# Bulk Import & Data Migration

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Bulk Import & Data Migration is the shared mechanism for getting existing data into the platform at scale — a new tenant's personnel roster, asset/vehicle list, or historical incident records from a prior system — rather than every feature building its own import path or every onboarding requiring hand-entry. It owns the upload/validation/commit pipeline; it explicitly does not own duplicate-identity resolution (that stays with Entity Registry Core's existing dedup/merge workflow) or what any specific record type's fields mean (that stays with the owning feature).

Tenant Management owns getting a tenant to exist; this doc owns getting a tenant's legacy data into it.

## Actors & Roles

- **Whoever holds the `bulk_import` permission for a record type** — a dedicated, explicitly granted capability, independent of both admin role and ordinary per-record create permission (see Permissions).
- **Platform Super Admin** — can run/authorize a bulk import for any tenant (e.g., assisted onboarding).
- **Every platform feature/module** — registers which of its record types support bulk import, and their import schema/template.

## User Stories

- As a **Tenant Admin onboarding from a prior guard-management system**, I want to upload a structured export of our personnel roster and have it land as properly-formed Person/Employee records, without re-typing every guard by hand.
- As a **Tenant Admin doing initial setup**, I want a CSV template with the exact columns a feature expects, so I don't have to guess the schema.
- As an **integration engineer** migrating a larger client from a competitor's platform, I want a structured bulk-file (JSON/XML) import path in addition to CSV, since the source system's export doesn't fit a flat spreadsheet cleanly.
- As anyone running an import, I want to see every validation error and warning **before** anything is committed, so I can fix a bad source file once rather than discovering problems row-by-row after partial data has already landed.
- As a **Tenant Admin**, I want an imported record that looks like a duplicate of an existing one to go through the same human-reviewed merge process as any other duplicate, not get silently skipped or silently duplicated.
- As a **Platform Super Admin**, I want bulk import gated by its own explicit permission, not implicitly available to anyone who can create one record at a time — importing 10,000 records is a different risk profile than creating one.

## Functional Requirements

### Registration
1. A feature registers a record type for **Bulk Import eligibility**: its import schema (required/optional fields, validation rules), a downloadable CSV/XLSX template, and — optionally — a structured bulk-file (JSON/XML) schema for system-to-system migration.
2. Not every record type needs to support bulk import; a feature opts in explicitly, matching the platform's registration-pattern default.

### Upload & dry-run validation
3. An import always runs a **mandatory dry-run validation pass** first: the uploaded file (CSV/XLSX or structured bulk-file) is checked against the registered schema, field-level validation rules, and referential integrity (e.g., a referenced Site must exist) — producing a full report of errors (block commit) and warnings (don't block, but are shown) before any record is written.
4. Nothing is committed until the importer explicitly confirms after reviewing the dry-run report. A dry-run with zero blocking errors can still be reviewed and cancelled; nothing is ever silently auto-committed.
5. On confirmation, the import commits in one batch operation, itself a Background Job Processing job (long-running imports don't block the initiating request) — with per-row commit status reported back (succeeded, skipped-as-duplicate-pending-review, failed).

### Duplicate handling
6. Bulk Import does not implement its own identity-matching logic. A row that plausibly matches an existing Entity (Person, Organization, Vehicle, etc.) is routed through **Entity Registry Core's existing human-reviewed dedup/merge workflow** — same mechanism as any other duplicate-identity case, not a parallel one invented for imports.
7. A row identified as a likely duplicate is still validated and staged, but its final commit is held pending the dedup/merge review's outcome (create-as-new vs. merge-into-existing), consistent with Entity Registry Core's existing merge-review shape.

### Permission
8. Bulk Import is gated by its own dedicated permission (`bulk_import` on a given record type), granted explicitly — independent of, and not implied by, either an admin role or ordinary per-record create permission. Creating one record and importing ten thousand are different risk profiles and are permissioned separately.

## Data Model / Fields

**Bulk Import Registration** (registered by a feature)
- record_type, owning_feature, csv_template_schema, structured_file_schema (nullable), validation_rules[]

**Import Batch**
- batch_id, tenant_id, record_type, initiated_by, initiated_at, source_format (csv, xlsx, json, xml)
- status (dry_run_pending, dry_run_complete, awaiting_confirmation, committing, completed, cancelled)
- validation_report (errors[], warnings[]), row_count

**Import Row Result**
- batch_id, row_number, outcome (committed, skipped_duplicate_pending_review, failed), linked_dedup_review_ref (nullable), error_detail (nullable)

## States & Transitions

**Import Batch:** `dry_run_pending` → `dry_run_complete` (validation report ready) → `awaiting_confirmation` → `committing` (Background Job Processing job running) → `completed`. Any point before `committing` can transition to `cancelled`.

## Integrations

- **Tenant Management**: the primary trigger context — new tenant onboarding is Bulk Import's main use case.
- **Entity Registry Core**: owns all duplicate-identity resolution; Bulk Import routes candidate duplicates there rather than reimplementing matching logic.
- **Background Job Processing**: the actual commit phase (item 5) runs as a job, inheriting standard retry/dead-letter behavior for a partially-committed batch interrupted mid-run.
- **Blob/File Storage**: uploaded source files and generated validation reports are stored through this engine.
- **Structured Logging & Audit Trails**: every import batch (initiator, row counts, outcomes) is an audit-tier event.

## Permissions

| Action | Platform Super Admin | Holder of `bulk_import` grant for a record type | Ordinary record-type create permission holder (no `bulk_import` grant) |
|---|---|---|---|
| Register a record type for Bulk Import (developer, via feature build) | ✅ (via feature build) | n/a | n/a |
| Upload a file and run dry-run validation | ✅ | ✅ | ❌ |
| Confirm and commit an import batch | ✅ | ✅ | ❌ |
| Cancel a pending import batch | ✅ | ✅ (own batch) | ❌ |

## Non-Functional / Constraints

- Dry-run validation must complete and report results without committing any partial data under any failure mode (a crash mid-validation leaves zero records written).
- A commit-phase interruption (job crash mid-batch) must be safely resumable per Background Job Processing's idempotency guarantee — no row double-committed, no row silently skipped.
- Large batches (tens of thousands of rows) must not degrade platform performance for concurrent users — running as a background job (item 5) is the primary mitigation.
- Validation error/warning reports must be specific enough (row number, field, reason) that a non-technical Tenant Admin can fix a source file without developer help.

## Acceptance Criteria

- [ ] Uploading a CSV against a registered record type's template produces a dry-run report listing every validation error/warning with row and field specificity, with zero records committed.
- [ ] Confirming a dry-run with zero blocking errors commits every row; confirming one with blocking errors is not possible until the source file is corrected and re-validated.
- [ ] A row matching an existing Entity is not silently created as a duplicate nor silently dropped — it surfaces in Entity Registry Core's dedup/merge review, and its final state depends on that review's outcome.
- [ ] A user with ordinary create permission on a record type but no `bulk_import` grant cannot initiate a bulk import for that type.
- [ ] An import batch interrupted mid-commit (simulated job failure) resumes cleanly with no duplicate or missing rows on retry.
- [ ] A structured bulk-file (JSON/XML) import for a record type that registered a structured schema validates and commits equivalently to the CSV path.

## Open Questions

- Whether a partially-failed commit (some rows succeed, some fail validation only discoverable at commit time — e.g., a race against a concurrently-deleted referenced Site) should auto-rollback the whole batch or land as a mixed-outcome batch with per-row results — leaning toward the latter (matches item 5's per-row status design) but worth confirming during technical spec.
- Whether Bulk Import needs a scheduled/recurring mode (e.g., a nightly sync from a tenant's external HR system) or is strictly an ad hoc, manually-triggered tool — out of scope as currently elicited; flagged as a possible fast-follow.
- Exact retention/audit requirements for uploaded source files after a batch completes — likely governed by the platform's general audit/document retention posture rather than a bespoke rule here.
