# MVP Scope Ledger

**Status:** Living document — the working ledger for MVP scope. `docs/pdd.md`'s "MVP Definition & Target Customers" section is the stable summary; **this doc is where scope actually moves**. Every addition or slim gets a Change Log entry with rationale. Keep the two consistent; when they disagree, this doc is current and the PDD needs a touch-up.

## North star: right-sizing

The final platform must **right-size to nearly any operation** — from a single guard on shift at a small site (no dispatcher, mobile-only) up to a multi-site enterprise rolled up from campus-scale sites. The only operation deliberately below the value line is a **tiny fixed-post-only contract operation** (a lone desk guard, no patrols, no dispatch, no reporting obligation worth automating).

Right-sizing is not a future feature — it falls out of decisions already made: per-tenant/site feature activation rides the Settings & Preferences hierarchy (and the framework's feature-management if ABP lands), the site-is-the-design-unit NFR tiers already define the solo-site posture (push + timers only, no console), and registration-shaped features can be switched off per site without leaving dead mechanism weight behind. The MVP is the first proof of right-sizing: it must serve a national lab campus and a mid-size hotel shift with the same build.

## Scope gates

**To add a feature to MVP** it must pass: *the core loop (capture → dispatch → document → report) does not work right for one of the three target customers without it.* "Would be nice for the demo" and "cheap because registration-shaped" are explicitly not sufficient — cheap features still add verification and support surface.

**To slim a feature out**: it can leave if none of the three target customers' daily operation breaks without it, even if already spec'd. Spec'd ≠ committed; the spec survives for the fast-follow.

Every move, either direction, gets a Change Log row and (if architectural) a `_DECISIONS.md` entry.

## In scope

### Kernel (Modules 0 / 0.5 subset)
- [ ] Authentication & Authorization (IdP federation + local accounts; RBAC + ABAC overlay) — spec'd
- [ ] Structured Logging & Audit Trails (hash-chain core; anomaly engine deferred) — spec'd
- [ ] Offline Data Sync (append-only outbox) — spec'd
- [ ] Notifications Engine — spec'd
- [ ] GIS & Mapping Services (self-hostable default adaptor first) — spec'd
- [ ] API & Messaging Layer (REST/GraphQL surface per ABP-era revisit) — spec'd
- [ ] Event/Command/Query bus + Domain Events + Command/Action Bus — spec'd
- [ ] Settings & Preferences — spec'd
- [ ] Real-Time Delivery & Server-Side Timers — spec'd
- [ ] AI / LLM Services (cloud adaptors; self-hosted adaptor is post-MVP) — spec'd
- [ ] Master Records spine: Entity Registry Core, Party/Person/Organization, Item/Vehicle, Location, Activity, Document, Entity Relationships & History — spec'd

### Module 1 — Security Operations (complete, all spec'd)
- [ ] Daily Activity Reports · Shift Passdowns · Guard Tour & Checkpoint Verification · Patrol Management · Courtesy Patrol · Tickets/Citations & Traffic Safety · Incident Reporting & Management · AI-Assisted Incident Report Writing *(stays in MVP as headline differentiator)*

### Module 2 — Dispatch/CAD (complete, all spec'd)
- [ ] Call Intake & Logging · Unit Dispatch & Proximity Routing · Active Incident Queue · Status & State Monitors · Silent Mobile Dispatching · Multi-Incident Console · Active Call Alerts & Timers · Historical CAD Log Reconstruction

### Module 9 slice — Locations in the full facilities sense
- [ ] Location Hierarchy Designer (build/manage the site→building→floor→room/zone tree over Location Registry's existing `HierarchyAssociation` spine) — **needs elicitation (next up)**
- [ ] Zone definitions/mapping (GIS-backed zone geometry as Location records) — **needs elicitation (next up)**

### Module 7 slice — Hazard & hazmat warnings by location
- [ ] Hazmat/chemical presence + NFPA 704 placard data associated to Locations — **needs elicitation**
- [ ] Hazard warnings surfaced as **dispatch context**: a responder dispatched to a location sees its hazards (and its ancestors'/zone's) before arrival on Call/Dispatch surfaces — **needs elicitation**

## Fast-follows (spec'd or cheap, deliberately not MVP-blocking)
- Command Palette · CLI-Style Input · Tenant-Defined Subtypes & Custom Fields (all spec'd — trail the launch)
- Lost & Found (Module 16) — hotel pull, registration-cheap
- Investigation Management slice (Module 13) — casino pull
- Compliance evidence packaging slice (Module 21) — lab pull
- Self-hosted AI inference adaptor — air-gap/lab pull

## Explicitly out of MVP
- **Module 8 (scheduling)** — accepted consequence: Shift Window (DAR) and Post (Patrol Management) ship as load-bearing production features
- Contract-firm surface: client portal, billing/SOW, subcontractor management (Modules 11, 15)
- Module 3 wholesale (Command Center/EOC) — the Multi-Incident Console already covers the dispatcher's multi-pane needs; wallboards wait
- Modules 4–6, 10, 12 (beyond canned reports if needed), 13–25 except the named slices above
- Air-gapped deployment mode (self-hosted ≠ air-gapped; lab pilot runs self-hosted connected)

## Change Log

| Date | Change | Direction | Rationale |
| --- | --- | --- | --- |
| 2026-07-13 | Initial MVP defined: Modules 1+2 complete, kernel subset, Module 9 location slice, Module 7 hazard-by-location slice; targets = National Laboratory of the Rockies, Gaylord-class hotels, casinos | — | Founder-defined; recorded in pdd.md and `_DECISIONS.md`; resolves PDD Open Question #3 |
| 2026-07-13 | AI-Assisted Incident Report Writing confirmed IN despite deferability | in | Headline differentiator for all three targets |
| 2026-07-13 | Command Palette, CLI-Style Input, Tenant-Defined Subtypes moved to fast-follow | out | Core loop works without them; already spec'd, cheap to trail |
