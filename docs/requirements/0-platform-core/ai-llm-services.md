# AI / LLM Services

**Module:** 0 Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Generalizes the underlying AI-generation capability that AI-Assisted Incident Report Writing (narrative drafting, voice transcription) and CLI-Style Input (AI-assist mode's natural-language-to-command translation, voice input) both consume — flagged as an open question in the former, resolved here as its own Platform Core feature rather than an unspecified assumed dependency living inside either doc.

Three parts:

1. **Provider abstraction, SaaS-pooled or BYO API key.** A tenant can use the platform's own pooled SaaS token allocation (usage-metered, feeding a future Module 15 billing integration) or bring their own API key for a specific provider — **Anthropic, OpenAI, Google Gemini, or Microsoft Azure OpenAI Service** (a distinct provider from OpenAI's own API — different auth/endpoint/data-residency characteristics, relevant to DOE/federal tenants needing Azure Government hosting). A BYO key is encrypted at rest and never redisplayed in plaintext after entry, per Authentication & Authorization's secrets-handling model.
2. **Prompt Templates with `{placeholder}` substitution.** A template's `template_text` references named placeholders a consuming feature/context declares as available (e.g., Incident Narrative generation offers `{incident_category}`, `{severity}`, `{timeline_events}`, `{location_name}`); an undeclared placeholder is rejected at save time — the same schema-governed-substitution discipline already established for Tenant-Defined Types & Custom Fields' `extended_fields`.
3. **Custom Instructions (system prompt), tenant-configurable by incident category AND location, with platform defaults.** Not a simple hierarchical override — Settings & Preferences' location/identity chains resolve one value per level — but a **category × location lookup**: a Tenant Admin can set different system instructions for, say, Theft incidents at Site A vs. Medical incidents platform-wide, resolved narrowest-match-wins across both axes independently, always falling back to a shipped platform default so nothing is ever left unconfigured.

## Actors & Roles

- **Tenant Admin** — configures AI Provider mode (SaaS-pooled vs. BYO key), authors/edits Prompt Templates and AI Prompt Configurations for their tenant, within any delegated authority granted to Site Admins.
- **Site Admin** — may configure site-scoped AI Prompt Configurations if delegated, per Settings & Preferences' existing delegated-authority pattern.
- **Platform Super Admin** — manages platform-default templates/configurations, sets SaaS-pooled quotas, has audit visibility into BYO key *status* (never the key value itself) across tenants.
- **Every platform feature/module** — declares itself as an **AI Context** (a `context_key`, e.g. `incident_narrative`, `cli_assist_translation`, `voice_transcription`) and the placeholder variables it makes available, rather than talking to a provider or hardcoding a prompt directly.

## User Stories

- As a **Tenant Admin**, I want to use our own Anthropic API key instead of the platform's pooled tokens, for our own cost and data-handling reasons.
- As a **Tenant Admin** at a DOE facility, I want to use Azure OpenAI specifically, since that's what our data-residency requirements call for.
- As a **Tenant Admin**, I want Theft incidents at our downtown location to generate narratives in a more formal tone than our general default, without affecting every other site or category.
- As a **Platform Architect**, I want every feature that needs AI generation to declare its own context and placeholders once, rather than each building its own prompt-assembly logic.
- As a **Platform Super Admin**, I want to see that a tenant's BYO key has started failing, without ever being able to see the key itself.
- As a **Tenant Admin**, I want a sensible platform default to apply immediately for any incident category/site I haven't explicitly configured, rather than AI generation breaking until I configure everything myself.

## Functional Requirements

### Provider & credential configuration
1. **AI Provider Configuration** declares `mode` (`platform_pooled` or `byo_key`), `provider_adaptor`, and `model` — one per tenant by default (see Open Questions for per-context override). Providers are **adaptors** behind one interface (the same architecture GIS & Mapping Services established, per the platform-wide provider-adaptor decision in `_DECISIONS.md`): launch adaptors are anthropic, openai, google_gemini, and azure_openai; a **self-hosted inference adaptor** (local model server) is the designed-but-not-built future adaptor for air-gapped/DOE tenants — adding it is adaptor implementation work, never a rework of this feature or its consumers.
1a. **Air-gapped limitation, disclosed**: until a self-hosted adaptor ships, AI features are unavailable in air-gapped deployments — a documented, bounded limitation (same posture as AI drafting being online-only within the offline-first flow), not a silent gap. Air-gapped/DOE tenant configurations are restricted to self-hostable adaptors, exactly mirroring GIS's adaptor restriction rule for those tenants.
2. A BYO API key is encrypted at rest, write-only after initial entry (never redisplayed in plaintext, to any role including Platform Super Admin), and independently rotatable/revocable, consistent with Authentication & Authorization's existing secrets-handling posture.
3. `platform_pooled` usage is metered per tenant (token/request counts) — this doc records usage only; actual billing/invoicing is a future Module 15 integration, not built here.
4. A failed BYO key (invalid, revoked, rate-limited, provider outage) surfaces a clear, actionable error to the requesting user — it never silently falls back to platform-pooled tokens on the tenant's behalf, avoiding surprise cross-billing; the tenant explicitly fixes the key or switches modes.

### Prompt Templates
5. A **Prompt Template** declares `template_text` containing named `{placeholder}` tokens, scoped to a `context_key` an **AI Context Registration** (declared by the consuming feature) defines, including that context's own available placeholder variables.
6. An undeclared placeholder in a template is rejected at save time; a declared-but-unused placeholder is allowed.
7. The platform ships a default Prompt Template per `context_key`; a Tenant Admin can clone-and-customize or author from scratch.

### Custom Instructions — category × location resolution
8. An **AI Prompt Configuration** bundles a `prompt_template_ref` and `system_instructions` (free text) for a given `context_key`, optionally scoped by `incident_category_ref` (nullable = all categories) and `site_location_ref` (nullable = tenant-wide).
9. Resolving a specific generation request selects the **most specific matching configuration**: (category + site) > (category only) > (site only) > platform default — a default always exists per `context_key`, so a request is never left unconfigured.
10. AI Prompt Configuration changes are versioned (prior value, changed by, changed at), mirroring the platform's established versioning discipline (Settings & Preferences, Route/Tour Definition).

### Generation invocation
11. Every AI generation call resolves its Provider Configuration and its AI Prompt Configuration (template + system instructions, populated with the calling context's declared placeholder values) before invoking the underlying provider — a consuming feature never talks to a provider directly or hardcodes a prompt.
12. Every invocation is logged (provider/model, template/configuration version, token counts, success/failure) as an audit-tier event; whether full input/output content is retained is itself tenant-configurable, a deliberate privacy/data-minimization tradeoff (see Open Questions).

## Data Model / Fields

**AI Provider Configuration**
- config_id, tenant_id, mode (platform_pooled, byo_key)
- provider_adaptor (launch: anthropic, openai, google_gemini, azure_openai; future: self_hosted_inference), model
- byo_api_key_ref (nullable, encrypted secret reference, only when mode = byo_key)
- status (active, key_invalid, disabled)

**Prompt Template**
- template_id, tenant_id (nullable = platform default), context_key
- name, template_text (with `{placeholder}` tokens)
- is_platform_default (bool)

**AI Context Registration** (declared by each consuming feature — catalog only)
- context_key (PK, e.g. incident_narrative, cli_assist_translation, voice_transcription)
- owning_feature, available_placeholders[] (key, description)

**AI Prompt Configuration**
- prompt_config_id, tenant_id, context_key (FK → AI Context Registration)
- incident_category_ref (nullable), site_location_ref (nullable)
- prompt_template_ref, system_instructions
- version, version_history[] (version, changed_by, changed_at, diff)

**AI Invocation Log**
- invocation_id, tenant_id, context_key, provider_config_ref, prompt_config_ref
- requested_by, requested_at, token_count_input, token_count_output
- status (succeeded, failed), error_reason (nullable)
- content_retained (bool, tenant-configurable whether full input/output is stored)

## States & Transitions

**AI Provider Configuration:** `active` → `key_invalid` (failed auth/rate-limit detected) → `active` (key corrected) | `disabled`.

**Prompt Template / AI Prompt Configuration:** no separate lifecycle beyond versioning in place (#10).

## Integrations

- **AI-Assisted Incident Report Writing**: consumes this feature for AI Draft Narrative generation (`context_key = incident_narrative`) and voice transcription (`context_key = voice_transcription`), declaring placeholders like `{incident_category}`, `{severity}`, `{timeline_events}`, `{updates_text}`, `{location_name}`.
- **CLI-Style Input**: consumes this feature for AI-assist mode's natural-language-to-command translation (`context_key = cli_assist_translation`) and its own voice input, reusing the `voice_transcription` context established by AI-Assisted Incident Report Writing.
- **Authentication & Authorization**: BYO API key encryption/secrets handling follows that doc's existing model; step-up auth is a plausible gate on entering/rotating a key (Open Question).
- **Settings & Preferences**: AI Prompt Configuration's category × location resolution is a related but deliberately distinct pattern — a two-axis lookup, not literally that engine's single hierarchical chain — sharing only the general "narrowest match wins, a default always exists" philosophy.
- **Tenant-Defined Types & Custom Fields**: AI Context Registration's placeholder-declaration/validation discipline directly parallels that feature's carrier/schema pattern, though kept as a separate registry since the shapes differ (prompt placeholders vs. record field schemas).
- **Module 15 (Billing Rates Matrices, Invoicing Reconciliation Export, future)**: eventual consumer of `platform_pooled` usage metering for tenant billing — deferred, not built here.
- **Structured Logging & Audit Trails**: every AI Provider Configuration change, Prompt Template/Configuration edit, and AI invocation is audit-tier.

## Permissions

| Action | Tenant Admin | Site Admin | Platform Super Admin |
|---|---|---|---|
| Configure AI Provider (mode, BYO key entry/rotation) | ✅ (own tenant) | ❌ | ✅ (any tenant) |
| Author/edit Prompt Templates | ✅ | ❌ (unless delegated) | ✅ (incl. platform defaults) |
| Configure AI Prompt Configuration (category/location) | ✅ (tenant-wide) | ✅ (site-scoped, if delegated) | ✅ |
| View AI Invocation Log | ✅ (own tenant) | ✅ (site-scoped, if delegated) | ✅ (all tenants) |
| View a tenant's BYO API key value | ❌ | ❌ | ❌ (status only, never the key) |

## Non-Functional / Constraints

- A BYO API key must never be visible in plaintext after initial entry, to any role — write-only, test-connection validation only.
- A provider outage or BYO key failure must degrade gracefully with a clear, actionable error — never silently fail or silently fall back to a different mode/provider without consent.
- Placeholder substitution must reject unresolved/undeclared placeholders at template save time, not first-use-in-production.
- AI Prompt Configuration resolution (#9) must be deterministic and explainable — an admin should be able to see exactly which configuration matched and why, mirroring Settings & Preferences' own "which level controls this" discoverability requirement.
- WCAG 2.1 / Section 508 accessible provider/template/instruction configuration flows, day one.

## Acceptance Criteria

- [ ] A tenant can switch from `platform_pooled` to `byo_key` mode for Anthropic and successfully generate an incident narrative using their own key.
- [ ] A Prompt Template referencing an undeclared placeholder is rejected at save time.
- [ ] An AI Prompt Configuration scoped to (category=Theft, site=Building A) is selected over a tenant-wide Theft configuration when both exist, for a Theft incident at Building A.
- [ ] A `context_key` with no tenant-specific configuration at all falls back correctly to the platform default.
- [ ] A revoked/invalid BYO key surfaces a clear error to the requesting user rather than silently using platform-pooled tokens.
- [ ] Every AI invocation is logged with provider/model/template/configuration version and token counts.
- [ ] Platform Super Admin cannot view a tenant's BYO API key value, only its status.

## Open Questions

- Whether AI Provider Configuration should support per-`context_key` overrides (e.g., a different provider for narrative generation vs. CLI-assist) at launch, or start tenant-wide-only — leaning toward tenant-wide-only for v1, flagged as a natural future extension.
- Exact model list/versioning per provider — a technical-spec-level, frequently-changing concern, not fixed here.
- Whether entering/rotating a BYO API key requires step-up authentication, given its sensitivity — leaning toward yes, not confirmed.
- Exact usage-metering/billing integration mechanics with Module 15 — deferred until that module is specified.
- Default retention policy for `content_retained` (full input/output logging) — a real privacy/data-minimization tradeoff against investigative/audit value, not resolved here.
- Whether Prompt Template content itself needs versioning/rollback independent of AI Prompt Configuration's own versioning, given a template could be shared across multiple configurations — not fully addressed.
