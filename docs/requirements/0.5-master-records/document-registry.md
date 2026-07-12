# Document Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Document Registry establishes **Document** — NIEM Core's `nc:DocumentType` — as the fifth base Entity Type (per Entity Registry Core), rounding out the platform's base-type set alongside Party, Item, Location, and Activity. A Document is any file/record artifact the platform tracks with real metadata: a policy, a signed waiver, a certificate/license scan, an evidence photo, a contract, an SDS sheet, a post order. `nc:DocumentType` genuinely supports this depth — title, author (a Party reference), date, category, format, version, and (per real-world evidence/records-management practice this platform's domain calls for) integrity verification — so this doc gives it full base-type treatment rather than a thin stub.

**Hash/integrity and version history live at the base Document level**, not deferred to a specific consuming module. This directly serves Investigation Management's future "Digital Evidence Locker" (which MODULES.md already anticipates needing a "Cryptographic File Hash Vault") without that module having to build its own file-integrity mechanism — the same "build it once at the base layer" discipline already applied to Item's custody tracking and Activity's offline-safe numbering. Every module attaching a file (Policy & Document Management, Personnel's license/cert uploads, Subcontractor Management's COI documents, Safety Management's SDS sheets, Investigation's evidence) references or extends this same canonical Document record.

## Actors & Roles

- **Any user with upload permission** (scope governed by the consuming module — Policy & Document Management for SOPs, Personnel for certifications, etc.) — uploads/creates Document records.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Document entities.
- **Every future document-consuming module** (Policy & Document Management, Personnel, Subcontractor Management, Safety Management, Investigation Management, Contract & Client Management): registers its own Document extension or simply references base Document records where no richer extension is needed.

## Functional Requirements

### Base Document fields (`nc:DocumentType`-aligned)
1. **Title & description**: `document_title`, `description`.
2. **Category**: `document_category` (policy, certificate, evidence_photo, contract, sds_sheet, post_order, waiver, other — extensible), analogous to Item's item_category and Activity's activity_type, since applicable fields/workflows genuinely differ by kind.
3. **Author/issuer**: a `DocumentAuthorAssociation` — a TPT subtype of Entity Registry Core's EntityAssociation (`entity_id_a` = this Document, `entity_id_b` = a Party, Person or Organization), not a plain field — consistent with real documents often being authored by an individual but issued under a company/department, and allowing multiple authors/co-authors without a schema change.
4. **Date**: `document_date` (creation/issue date).
5. **Format**: `format` (MIME type / file format).
6. **File reference**: `file_ref` — pointer to the actual stored file content (storage mechanism is a technical-spec concern; this doc establishes the metadata record, not the file storage backend).
7. **Classification/security marking**: `classification` (nullable — e.g., unclassified, confidential, controlled-unclassified), relevant given this platform's DOE/federal target market and NIEM's document model supporting security-marking concepts natively.
8. **Display label** (per Entity Registry Core's universal requirement): template strategy, `document_title`.

### Version history
9. Editing a Document creates a new version rather than overwriting; `version_history[]` (version, changed_by, changed_at, file_ref for that version) preserves every prior version — mirroring the versioning pattern already established for Domain Events rules, CLI aliases, and Settings values.
10. References to a Document (e.g., a policy cited elsewhere) point to the stable Document entity_id, always resolving to its current version unless a consumer explicitly pins to a specific historical version.

### Integrity verification
11. Every Document version is cryptographically hashed at upload; the hash is stored alongside that version's metadata, enabling integrity verification (confirming a file hasn't been altered since upload) — the canonical mechanism Investigation Management's future Digital Evidence Locker builds on rather than reimplementing.
12. A hash-verification check can be run on demand or automatically on access for especially sensitive categories (e.g., evidence_photo), surfacing a clear pass/fail result.

### Deduplication
13. Document-specific match signals for Entity Registry Core's deduplication engine: identical file hash is the highest-confidence signal (literally the same file uploaded twice); title + author + date similarity as a secondary fuzzy signal. Never auto-merged, per Entity Registry Core's universal governance — even an exact hash match is surfaced for human confirmation, not silently collapsed, since two genuinely distinct records might legitimately reference the same boilerplate file (e.g., a blank waiver template used across many separate signings).

## Data Model / Fields

**Document** (TPT level: entity_id is the shared PK, FK → Entity.entity_id — structured per `nc:DocumentType`)
- entity_id (PK, FK → Entity), tenant_id, document_category
- document_title, description, document_date
- format, file_ref, classification (nullable)
- current_version, version_history[] (version, changed_by, changed_at, file_ref, hash)
- hash (current version's cryptographic hash)

*(Author/issuer is a `DocumentAuthorAssociation` row, not a field here.)*

**DocumentAuthorAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id)
- association_id (PK, FK → EntityAssociation; entity_id_a = this Document, entity_id_b = the authoring Party)
- no extra fields beyond the base EntityAssociation shape

## States & Transitions

**Document:** `active` → `tombstoned` (merged away, per Entity Registry Core's standard model) → `active` (merge reversed). Independent versioning (`version_history[]`) proceeds regardless of the base active/tombstoned state.

## Integrations

- **Entity Registry Core**: owns the base Document TPT mechanics (global ID, deduplication, merge) and the base EntityAssociation shape `DocumentAuthorAssociation` extends.
- **Party Registry**: source of the Party entity `DocumentAuthorAssociation` points to.
- **Structured Logging & Audit Trails**: document creation, version changes, and merges are audit-tier; access to classified/sensitive-category documents is itself an audit-tier view event.
- **Investigation Management** (future Digital Evidence Locker): consumes this doc's hash/integrity mechanism directly rather than building its own "Cryptographic File Hash Vault."
- **Policy & Document Management, Personnel, Subcontractor Management, Safety Management, Contract & Client Management**: future consumers/extenders — each references base Document records or registers its own richer Document extension (e.g., a "Post Order" extension with location association and acknowledgment tracking) when that module is specified.
- **Entity Relationships & History**: consumes Document entity IDs (and Activity-Document associations, per Activity Registry) to build cross-module interaction timelines.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Any user with upload permission (per consuming module) | Records Admin |
|---|---|---|---|---|
| Create/upload a Document | ✅ | ✅ | ✅ (per own module RBAC) | ❌ |
| View a Document (non-classified) | ✅ | ✅ | ✅ (per own module RBAC) | ✅ |
| View a classified/sensitive-category Document | ✅ | ✅ | ✅ (if separately granted) | ✅ (if separately granted) |
| Create a new version | ✅ | ✅ | ✅ (per own module RBAC) | ❌ |
| Run/view hash-integrity verification | ✅ | ✅ | ✅ (per own module RBAC) | ✅ |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ✅ |

## Non-Functional / Constraints

- File hashing must use a cryptographically strong algorithm consistent with the platform's FIPS-alignment goal (per the .NET/FedRAMP backend decision) for DOE/federal tenants.
- Version history must be immutable once written — a prior version's hash and file_ref never change retroactively, only new versions append.
- Classification/security-marking fields must integrate with the platform's ABAC model (clearance-vs-classification gating, already established in Authentication & Authorization) so a classified Document is only viewable by users whose clearance attribute meets the bar.
- File storage backend must support the same encryption-at-rest standard as other sensitive platform data, regardless of where the actual bytes live (a technical-spec decision).
- WCAG 2.1 / Section 508 accessible document upload/view flows, day one.

## Acceptance Criteria

- [ ] A base Document record supports title, category, date, format, file reference, and classification, each independently optional except title/file; author is a `DocumentAuthorAssociation`, not a field.
- [ ] A Document can have more than one active `DocumentAuthorAssociation` (co-authors) without a schema change.
- [ ] Editing a Document creates a new version, preserving the prior version's file reference and hash in version_history, without breaking existing references to the Document's entity_id.
- [ ] A Document's hash is computed at upload and a subsequent integrity check correctly detects a simulated file alteration.
- [ ] Two Documents with identical file hash are flagged as a potential duplicate for human review, never silently auto-merged.
- [ ] A classified Document is only viewable by a user whose ABAC clearance attribute meets the document's classification level, verified via the existing Authentication & Authorization mechanism.
- [ ] A future Investigation Management Digital Evidence Locker feature (stubbed) successfully reuses this doc's hash/version mechanism rather than building an independent one.

## Open Questions

- Full document_category taxonomy beyond the illustrative set — built out incrementally as each consuming module (Policy & Document Management, Personnel, Investigation Management, etc.) is specified.
- File storage backend technology (object storage provider, self-hosted vs. cloud for DOE/air-gapped deployments) — a technical-spec-level decision.
- Exact hash algorithm — a technical-spec-level decision, likely SHA-256 or a FIPS-validated equivalent.
- Whether Document ever needs its own BOLO-Flag-style use case — Entity Registry Core's generic mechanism is available if needed; not speculatively built out here.
- Exact NIEM release/version and precise `nc:DocumentType` element names — same technical-spec-level verification task noted in the other Master Records docs.
