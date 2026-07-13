# Project Design Document: Sentinel Suite

**Status:** draft
**Created:** 2026-02-22
**Last Updated:** 2026-02-23

## Problem Statement

Organizations responsible for safety, security, emergency management, emergency planning, and risk management currently rely on fragmented, disconnected tools — or worse, paper and spreadsheets — to manage critical operations. This fragmentation leads to poor incident documentation, slow response times, compliance gaps, increased liability exposure, and inconsistent operations across facilities.

Sentinel Suite is a unified platform designed to serve both contract security companies managing multiple client sites and in-house security departments across a wide range of facility types — from commercial office buildings and hotels/casinos to DOE GOCO-operated National Laboratories.

## Users & Stakeholders

| Role | Primary Needs |
| --- | --- |
| Security Officers / Guards | Patrol management, incident reporting, daily activity logs, mobile/offline access |
| Dispatchers | Real-time coordination, call management, officer tracking, assignment dispatch |
| Security Managers / Directors | Oversight, scheduling, KPI tracking, compliance management |
| Safety Officers / Managers | Inspections, hazard tracking, OSHA compliance, SDS management |
| Emergency Management Coordinators | Emergency plans, exercises, NIMS/ICS coordination, EOC operations |
| Risk Managers | Risk assessments, registers, mitigation tracking, trend analysis |
| Executive Leadership / C-Suite | Dashboards, strategic reporting, liability oversight |
| Contract Company Account Managers | Client management, contract compliance, multi-site oversight |
| Facility / Client Stakeholders | Client portal access, reports, service verification |
| Compliance / Audit Teams | Audit trails, regulatory reporting, documentation review |
| Training Coordinators | Certification tracking, training records, license compliance |
| IT / Systems Administrators | Platform configuration, tenant management, integrations, deployment |

## Goals & Success Criteria

1. **Replace disconnected tools** with a single, unified platform across all security and safety disciplines
2. **Reduce incident response times** through real-time dispatch, situational awareness, and streamlined workflows
3. **Improve compliance and audit readiness** with built-in regulatory tracking and comprehensive audit trails
4. **Standardize operations** across diverse facility types with configurable workflows
5. **Enable real-time situational awareness** via command center dashboards, GIS/mapping, and officer tracking
6. **Reduce training burden** with a consistent interface across all modules
7. **Scale from single building to multi-site enterprise** with flexible organizational hierarchy
8. **Provide actionable analytics and reporting** with KPI dashboards and custom reports
9. **Meet regulatory requirements** including DOE Orders, FISMA, OSHA, NIMS/ICS, and facility-specific standards
10. **Reduce liability exposure** through thorough documentation, audit trails, and compliance enforcement
11. **Improve incident documentation quality** including AI-assisted report writing

## Constraints

### Tech Stack

- **Frontend:** React / Next.js
- **Mobile:** React Native (with possible native iOS/Android if warranted)
- **Backend:** TBD (to be determined in technical spec)
- **Database:** TBD
- **Containerization:** Docker

### Compliance & Regulatory (Day-One Blockers marked with *)

- DOE Orders (470 series) *
- FISMA / NIST 800-53 *
- FedRAMP (future SaaS offering)
- CJIS, ITAR
- OSHA (29 CFR 1910/1926)
- NFPA codes
- NIMS/ICS, FEMA CPG 101, HSEEP
- ASIS standards, Joint Commission, Gaming Commission
- SOC 2, HIPAA, GDPR, state privacy laws
- Section 508 / WCAG 2.1 (day one)

### Timeline

- Greenfield project, no hard deadlines
- Phased rollout, module by module

### Budget

- Self-funded
- Solo developer with AI tools, planning to grow as needed

### Deployment Model

- **Primary (commercial):** SaaS
- **DOE / secure facilities (P1):** Self-hosted Docker deployment with API calls to web where permitted
- **Future:** FedRAMP-authorized SaaS
- **Future:** Air-gapped mode for most restricted environments

### Integration (Future Roadmap)

- Physical security systems (access control, CCTV/VMS, alarm panels)
- Dispatch/CAD and 911/PSAP
- Fire/life safety systems
- IT security (SIEM, IdP, SSO via SAML)
- HR/personnel systems
- Facilities/BMS/CMMS
- Government reporting systems
- Mass notification and communication systems
- GIS/mapping (likely first integration)

### Accessibility

- Desktop, tablet, and smartphone support
- Offline capability: minimal capture subset (append-only outbox), degraded performance accepted — not 1:1 offline parity; dispatcher radio-relay (on-behalf-of) is the primary disconnected workflow, and air-gapped facilities are served by self-hosted local deployment rather than client-side offline operation
- Section 508 / WCAG 2.1 compliance from day one

### Multi-Tenancy

- Tiered data isolation: full database isolation for DOE/secure clients, logical isolation for commercial
- Flexible-depth organizational hierarchy (Company → Region → Site → configurable sub-levels)
- Subcontractor support within the hierarchy

### Other

- White-labeling for contract security companies
- Data retention policies per regulatory requirements
- Internationalization: English first, multi-language on future roadmap
- Open-source licensing compliance for government use

## System Overview

### Modules

| # | Module | Description |
| --- | --- | --- |
| 0 | Platform Core | Auth, logging, offline sync, notifications, GIS, API, integrations (future), event bus, command bus, settings, preferences |
| 0.5 | Master Records | Canonical entity registry — base person record from which employees, visitors, guests, and other person types inherit or link; vehicle records; location records |
| 1 | Security Operations | Daily activity reports, guard tour, patrol management, courtesy patrol, tickets/citations/traffic safety interventions, incident reporting/management, AI-assisted incident report writing |
| 2 | Dispatch / CAD | Lite-to-mid dispatch and computer-aided dispatch (not 911 CAD) |
| 3 | Command Center / Dashboard / EOC | Real-time operational dashboards, emergency operations center view |
| 4 | Access Control | Access control management, key & lock management |
| 5 | Emergency Management | Emergency response coordination, NIMS/ICS support |
| 6 | Emergency Planning | Preplanning by location, incident type, and event |
| 7 | Safety Management | Safety inspections, hazard tracking, chemical/hazmat/SDS management |
| 8 | Personnel | Post & shift scheduling, training & certification tracking, officer licensing & compliance |
| 9 | Facility & Zone Management | Location hierarchy management, work orders |
| 10 | Equipment, Assets, Vehicles & Resources | Asset tracking, vehicle management, armory/weapons management |
| 11 | Subcontractor Management | Subcontractor oversight, compliance, and coordination |
| 12 | Performance & KPI Reporting | Analytics, dashboards, custom reports, trend analysis |
| 13 | Investigation Management | Case management, evidence tracking, investigation workflows |
| 14 | Policy & Document Management | Post orders, emergency notification sheets, policy distribution |
| 15 | Contract & Client Management | Contract tracking, client portal, service level management |
| 16 | Lost & Found | Found property logging, chain of custody, owner claim tracking |
| 17 | Mass Notification & Crisis Communications | Outbound emergency notification dispatching, check-ins, desktop alerts |
| 18 | Threat Intelligence & OSINT Ingestion | Public safety feeds, geofenced keyword scans, threat assessment radar |
| 19 | Physical Security Integration Gateway (IoT / VMS / Alarms) | Integration boundary with existing PSIM/ACS/VMS/alarm platforms (adaptor per upstream system) — escalating their events into documented incidents; never a PSIM/ACS/VMS replacement |
| 20 | K9 & Specialized Unit Operations | Canine profiles, scent registries, training logs, force reviews, tactical operations |
| 21 | Compliance, Self-Assessments & Audits | Control mapping (NIST/FISMA), assessments, evidence vault, audit trails |
| 22 | Business Continuity & Disaster Recovery (BC/DR) | Business impact analysis (BIA), RTO/RPO target registries, disaster checksheets, simulators |
| 23 | Executive Protection & Secure Transit | VIP profiles, route assessment, secure tracking, telemetry, executive panic alerts |
| 24 | Special Event & Incident Action Planning (IAP) | ICS form compiling, GIS layouts, event rosters, crowd checklist logs, permits |
| 25 | Supply Chain & Cargo Security | C-TPAT audits, seal logbooks, GPS tracking, validations, LP logs, yard boards |

### Module 0: Platform Core

**Purpose:** Foundation services shared by all modules
**Scope:** Authentication & authorization, structured logging, offline data sync, push/in-app notifications, GIS/mapping services, REST/GraphQL API layer, event bus and command bus architecture, system settings and user preferences. Future: external integrations framework.

### Module 0.5: Master Records

**Purpose:** Canonical entity registry shared across all modules
**Scope:** Base person record (from which employees, visitors, guests, and other person types inherit or link), vehicle records, and location records. Supports ban/trespass status, incident participants, BOLO subjects, gate pass recipients, and any other module that references a person, vehicle, or place. Acts as the platform's single source of truth for real-world entities.

### Module 1: Security Operations

**Purpose:** Day-to-day security officer workflows and incident management
**Scope:** Daily activity reports (DAR), guard tour with checkpoint verification, patrol route management, courtesy patrol logging, tickets/citations/traffic safety interventions, full incident reporting and management lifecycle, AI-assisted incident report writing.

### Module 2: Dispatch / CAD

**Purpose:** Real-time dispatch and coordination for security operations
**Scope:** Lite-to-mid computer-aided dispatch for security operations. Call taking, unit dispatch, status tracking, priority management. Explicitly not a 911/PSAP-grade CAD system.

### Module 3: Command Center / Dashboard / EOC

**Purpose:** Real-time operational awareness and emergency operations coordination
**Scope:** Configurable dashboards, real-time status boards, emergency operations center (EOC) view, situational awareness displays, live feeds aggregation.

### Module 4: Access Control

**Purpose:** Managing physical access and key/lock inventory
**Scope:** Access credential management, visitor management, clearance tracking, key and lock inventory, key issuance and return tracking.

### Module 5: Emergency Management

**Purpose:** Coordinating emergency response operations
**Scope:** Emergency response coordination, NIMS/ICS structure support, resource management during incidents, after-action reporting, exercise management.

### Module 6: Emergency Planning

**Purpose:** Proactive emergency preparedness documentation
**Scope:** Preplanning by location, incident type, and event. Emergency action plans, evacuation plans, continuity of operations plans.

### Module 7: Safety Management

**Purpose:** Workplace safety compliance and hazard management
**Scope:** Safety inspections and audits, hazard identification and tracking, OSHA recordkeeping, chemical/hazmat/SDS management, safety program administration.

### Module 8: Personnel

**Purpose:** Security workforce management and compliance
**Scope:** Post and shift scheduling, training and certification tracking, officer licensing and state compliance, qualification management.

### Module 9: Facility & Zone Management

**Purpose:** Physical location hierarchy and facility maintenance
**Scope:** Configurable location hierarchy management, zone definitions, facility profiles, work order creation and tracking.

### Module 10: Equipment, Assets, Vehicles & Resources

**Purpose:** Tracking and managing physical assets
**Scope:** Equipment inventory and lifecycle, vehicle fleet management, resource allocation, armory/weapons tracking and management.

### Module 11: Subcontractor Management

**Purpose:** Oversight of subcontracted security services
**Scope:** Subcontractor onboarding, compliance verification, performance monitoring, credential and insurance tracking.

### Module 12: Performance & KPI Reporting

**Purpose:** Operational analytics and performance measurement
**Scope:** KPI dashboards, custom report builder, trend analysis, benchmarking, automated report scheduling and distribution.

### Module 13: Investigation Management

**Purpose:** Structured investigation case management
**Scope:** Case creation and tracking, evidence management, investigation workflows, interview documentation, case disposition and reporting.

### Module 14: Policy & Document Management

**Purpose:** Centralized policy and operational document repository
**Scope:** Post orders management, emergency notification sheets, policy creation and distribution, version control, acknowledgment tracking.

### Module 15: Contract & Client Management

**Purpose:** Managing client relationships and contract obligations
**Scope:** Contract tracking, SLA monitoring, client portal with self-service reporting, billing support, service level management.

### Module 16: Lost & Found

**Purpose:** Tracking found property and managing owner claims
**Scope:** Found property logging, chain of custody tracking, owner claim workflow, disposition recording (returned, donated, destroyed, turned over to law enforcement).

### Module 17: Mass Notification & Crisis Communications

**Purpose:** Outbound emergency notification dispatching to occupants and community cohorts
**Scope:** Multi-channel emergency broadcasts (SMS, email, voice, push), safety status polls and check-in response consoles, opt-in visitor registration, desktop override alert integration, and alert escalation rules.

### Module 18: Threat Intelligence & OSINT Ingestion

**Purpose:** Automated monitoring of external threat vectors to inform proactive responses
**Scope:** Integration of public safety feeds (municipal CAD, weather feeds, USGS seismic logs), geofenced keyword scans, threat radar mapping, dispatcher alerts, social media keyword spikes, and BOLO alert ingestion.

### Module 19: Physical Security Integration Gateway (IoT / VMS / Alarms)

**Purpose:** Integration boundary with the physical-security systems a site already runs — the reporting engine those systems talk to best
**Scope:** Ingest events from existing intrusion/alarm, VMS, access-control, fire-panel, and IoT platforms via each system's own APIs/event streams (one adaptor per upstream platform, per the platform-wide provider-adaptor pattern — never native device protocols), route them through Activity Registry's Signal Disposition valve (display-only → telemetry → activity → auto-incident+dispatch), embed/deep-link upstream video and consoles rather than re-implementing them, monitor upstream-connection health, and own the automated dispatch triggers. Explicitly not a PSIM, VMS, or access-control system — escalation of alarm handling into documented incidents is the entire value proposition.

### Module 20: K9 & Specialized Unit Operations

**Purpose:** Operational tracking and qualifications for dog handlers and special response forces
**Scope:** Canine profiles and health records, scent target registries, training exercise logs, search and deployment logs, weapons certification gates, tactical equipment custody registers, canine bite/use-of-force review packages, and tactical mission timelines.

### Module 21: Compliance, Self-Assessments & Audits

**Purpose:** Continuous monitoring of security standards, policy distribution, and organizational compliance
**Scope:** Compliance standards matrices (FISMA, NIST, SOC 2, DOE Orders), internal self-assessment checklists, auditor evidence vault compilers, cryptographic audit trail logs, policy read auditing/SOP sign-offs, and compliance dashboards.

### Module 22: Business Continuity & Disaster Recovery (BC/DR)

**Purpose:** Disaster preparedness and organizational resilience management
**Scope:** Business Impact Analysis (BIA) matrices and RTO/RPO target registers, disaster checksheets and mobile task monitors, alternate supplier and MOU registries, vital records inventories, supply chain risk logs, recovery workflows, and tabletop exercise simulators.

### Module 23: Executive Protection & Secure Transit

**Purpose:** Securing close protection operations and transport schedules
**Scope:** VIP profiles and medical/threat records, GIS route assessments and safe harbor mapping, real-time vehicle GPS and driver telemetry tracking, armored car registries, arrival/departure flight trackers, and executive panic alarm listeners.

### Module 24: Special Event & Incident Action Planning (IAP)

**Purpose:** Pre-event scheduling, mapping, and multi-agency coordination
**Scope:** FEMA-aligned IAP and ICS form builders, perimeter GIS overlays, event staff scheduling and shift bidding, crowd capacity/gate calculators, temporary access badge QR creators, local agency liaison logs, and demobilization checklists.

### Module 25: Supply Chain & Cargo Security

**Purpose:** Securing freight yards, logistics transits, and warehouses
**Scope:** C-TPAT compliance logs, container seal logbooks, GPS cargo/transit monitoring, warehouse security and CCTV checklist logs, driver ID validations, loss prevention registers, staging area yard management boards, and cargo incident/hijack reports.

## Out of Scope

- Full 911/PSAP CAD system
- Payroll processing (scheduling only, no payroll)
- Legal case management
- Insurance claims processing
- Physical security hardware sales/installation
- Being a PSIM, VMS, alarm-management, or access-control system — excellent platforms exist for all four; Sentinel Suite integrates with them (adaptor per upstream system) as the reporting/records engine they escalate into, and never speaks native device protocols (BACnet, Modbus, Wiegand, RTSP)

## Risks & Mitigations

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Scope creep | High | Strict module-by-module phasing, MVP discipline, clear per-module scope boundaries |
| Offline sync complexity | Low (downgraded) | Scope narrowed to an idempotent append-only outbox — no offline editing of shared records, so no conflict-resolution engine; dispatcher on-behalf-of relay covers everything outside the capture subset |
| DOE/FISMA compliance complexity | High | Build compliance into architecture from day one, not bolted on later |
| Funding runway | Medium | Phased delivery to reach revenue-generating state quickly; commercial SaaS first |
| Multi-tenancy architecture | Medium | Design tiered isolation model upfront; validate with DOE security requirements early |
| Solo developer capacity | Medium | AI-assisted development, modular architecture enabling future team growth, no hard deadlines |
| Market competition | Medium | Differentiate on breadth (unified platform), DOE/lab support, modern UX, AI features |
| Adoption resistance | Medium | Client portal for stakeholder buy-in, gradual migration paths, superior documentation/reporting |
| Regulatory landscape changes | Low | Modular compliance framework that can adapt to new requirements |

## Open Questions

1. Backend language/framework selection (to be determined in technical spec)
2. Database selection (to be determined in technical spec)
3. Module build order / prioritization for phased rollout
4. GIS/mapping provider selection
5. Offline sync strategy (CRDTs, operational transforms, or custom)
6. AI provider/approach for incident report writing assistance
