# Sentinel Suite — Requirements Elicitation Tracker

Ordered working list of every feature (sub-heading) enumerated from `docs/MODULES.md`.
One requirements doc per feature, written to `docs/requirements/<module-slug>/<feature-slug>.md`.

Status legend: `[ ]` not started · `[~]` in progress (elicitation underway) · `[x]` doc written

## 0. Platform Core
- [x] Authentication & Authorization
- [x] Structured Logging & Audit Trails
- [x] Offline Data Sync
- [x] Notifications Engine
- [x] GIS & Mapping Services
- [x] API & Messaging Layer
- [x] Event & Command Bus Architecture
- [x] Domain Events *(added during elicitation, split out of Event & Command Bus Architecture)*
- [x] Command/Action Bus *(added during elicitation, split out of Event & Command Bus Architecture)*
- [x] Command Palette *(added during elicitation, invocation surface over Command/Action Bus)*
- [x] CLI-Style Input *(added during elicitation, invocation surface over Command/Action Bus)*
- [x] Settings & Preferences
- [x] Tenant-Defined Types & Custom Fields *(added during elicitation, generalized from a pattern first drafted locally in Courtesy Patrol / Entity Registry Core)*
- [x] AI / LLM Services *(added during elicitation, generalized from a dependency first assumed in AI-Assisted Incident Report Writing / CLI-Style Input)*
- [x] Real-Time Delivery & Server-Side Timers *(added during platform design review — owns the live console channel, server-side escalation timers, alarm state, and the platform's baseline real-time NFR targets)*
- [x] Tenant Management *(added during Module 0 gap review — provisioning, edition/plan, lifecycle, isolation tier; introduces Client Engagements, the contractor↔client dual-tenancy mechanism.)*
- [x] Feature Management *(added during Module 0 gap review — per-tenant/edition entitlement flags (boolean + quota kinds), distinct axis from Settings & Preferences' value-chain; flagged as a gap in mvp.md.)*
- [x] Blob/File Storage *(added during Module 0 gap review — adaptor-pattern file storage backend, malware scanning, tenant-scoped content-hash dedup, orphan cleanup; Document Registry explicitly deferred this as out of scope.)*
- [x] Background Job Processing *(added during Module 0 gap review — durable job/scheduled-task infrastructure, platform-enforced idempotency, retry/dead-letter, isolation-tier-aware placement; already silently assumed by Entity Merge's background rewrite and Offline Sync.)*
- [x] Global Search & Data Indexing *(added during Module 0 second-pass gap review — precomputed adaptor-pattern index, opt-in per record type, permission always re-checked live at query time; Command Palette's "universal search" and CLI-Style Input's identifier lookups both assumed this existed.)*
- [x] Bulk Import & Data Migration *(added during Module 0 second-pass gap review — mandatory dry-run validation, dedicated `bulk_import` permission, duplicates routed through Entity Registry Core's existing dedup/merge review; closes the "tenant exists but has no legacy data" gap left by Tenant Management.)*

## 0.5 Master Records
- [x] Entity Registry Core
- [x] Party Registry *(added during elicitation — nc:EntityType base [Person-or-Organization]; Person and Organization both extend it)*
- [x] Person Registry *(retrofitted to extend Party rather than being a direct base type)*
- [x] Organization Registry *(added during elicitation — second Party extension, rounds out the base type set)*
- [x] Item Registry *(generalized from "Vehicle Registry" — generic nc:ItemType-aligned base entity + custody tracking; Vehicle is its first extension type)*
- [x] Vehicle/Conveyance Registry *(added during elicitation — full depth for Item's Vehicle extension, promoted from stub since it's a cross-cutting core concern like Person)*
- [x] Location Registry
- [x] Activity Registry *(added during elicitation — one of five base entity types, given full first-class treatment)*
- [x] Document Registry *(added during elicitation — nc:DocumentType base, fifth base entity type, with hash/integrity + versioning)*
- [x] Entity Relationships & History

## 1. Security Operations
- [x] Daily Activity Reports (DAR)
- [x] Shift Passdowns & Handover Notes
- [x] Guard Tour & Checkpoint Verification
- [x] Patrol Management
- [x] Courtesy Patrol
- [x] Tickets, Citations & Traffic Safety
- [x] Incident Reporting & Management
- [x] AI-Assisted Incident Report Writing

## 2. Dispatch / CAD
- [x] Call Intake & Logging
- [x] Unit Dispatch & Proximity Routing
- [x] Active Incident Queue (CAD Console)
- [x] Status & State Monitors
- [x] Silent Mobile Dispatching
- [x] Multi-Incident Console
- [x] Active Call Alerts & Timers
- [x] Historical CAD Log Reconstruction

## 3. Command Center / Dashboard / EOC
- [x] Unified Operational Picture (UOP) Map
- [x] Command Center Wallboard View
- [x] ICS Role Mapping & Visual Org Chart
- [x] Situation Reports (SITREPs)
- [x] Live Camera Feed Ingestion
- [x] EOC Logistics Hub
- [x] Alarm Panel Monitors & Panic Alerts
- [x] Environmental & Weather Map Overlays
- [x] Historical Playback Console

## 4. Access Control
- [x] Pre-Registration Portal
- [x] Visitor Kiosk App
- [x] Host Arrival Notifications
- [x] Access Credential Management
- [x] Clearance Profiles
- [x] Key Ring Registry
- [x] Key Custody & Auditing
- [x] Lock Core & Cylinder Tracking
- [x] BOLO & Trespass Alerts
- [x] Remote Gate & Barrier Controls

## 5. Emergency Management
- [x] ICS Forms Engine (FEMA-aligned)
- [x] Resource Logistics Catalog
- [x] After-Action Reports (AAR)
- [x] Improvement Plan (IP) Tracking
- [x] Exercise & Drill Planner (HSEEP-aligned)
- [x] Drill Compliance Logging
- [x] Mutual Aid Agreements Tracker
- [x] EOC Activation Checklists

## 6. Emergency Planning
- [x] Pre-Incident Plans (Preplans)
- [x] Incident Action Checklists
- [x] Continuity of Operations Plans (COOP)
- [x] Muster Check-in App
- [x] Evacuation Roster Reconciliation
- [x] Hazard Identification & Risk Assessment (HIRA)
- [x] Mitigation Task Tracker
- [x] Alternate Site Registries

## 7. Safety Management
- [ ] Safety Inspections & Audits
- [ ] Corrective Action Pipelines
- [ ] Hazmat & Chemical Registries
- [ ] NFPA 704 Placard Mapping
- [ ] OSHA Incident Loggers
- [ ] Safety Metrics Calculators
- [ ] Industrial Hygiene Tracking
- [ ] PPE Inventory & Issuance
- [ ] Near-Miss Incident Portal

## 8. Personnel
- [ ] Post Schedule Builder
- [ ] Open-Shift Bidding
- [ ] Geofenced Time & Attendance
- [ ] Training Curriculum Library
- [ ] Licensing & Guard Card Tracking
- [ ] Armed Qualifications Registry
- [ ] Compliance Schedule Lockouts
- [ ] Skills & Capabilities Profiles

## 9. Facility & Zone Management
- [ ] Location Hierarchy Designer
- [ ] Access Point Bindings
- [ ] Zone Mapping (GIS)
- [ ] Work Order Dispatching
- [ ] Tenant & Occupant Registry
- [ ] Utility Control Tracking
- [ ] Spatial Asset Inventory

## 10. Equipment, Assets, Vehicles & Resources
- [ ] Asset Barcode Registry
- [ ] Barcode Checkout Station
- [ ] Calibration & Maintenance Alerts
- [ ] Vehicle Logbooks
- [ ] Fuel Card & Expense Log
- [ ] Weapons Inventory
- [ ] Ammunition Registry
- [ ] Armory Compliance Gate
- [ ] Depreciation & Asset Retirement Logs

## 11. Subcontractor Management
- [ ] Vendor Profiles
- [ ] Insurance Compliance Audits
- [ ] Roster Verifications
- [ ] SLA Scorecard Generator
- [ ] Subcontractor License Checks
- [ ] Billing Verification Logs
- [ ] Vendor Audit Checklists

## 12. Performance & KPI Reporting
- [ ] Custom Query & Report Builder
- [ ] Visual Dashboard Configurator
- [ ] Automated Emailed Reports
- [ ] Response Time Analytics
- [ ] Tour Completeness Logs
- [ ] Incident Location Heatmaps
- [ ] Officer Performance Logs
- [ ] Tenant SLA Metrics

## 13. Investigation Management
- [ ] Case Files
- [ ] Digital Evidence Locker
- [ ] Physical Evidence Tracking
- [ ] Interview Statement Logs
- [ ] Audio/Video Transcription
- [ ] Case Dispositions
- [ ] Task Logs & Leads Tracker
- [ ] Search & Seizure Registers

## 14. Policy & Document Management
- [ ] Post Orders Management
- [ ] Digital Acknowledgment Logs
- [ ] Emergency Call Lists
- [ ] Standard Operating Procedures (SOP) Library
- [ ] Document Revision Auditing
- [ ] Shared Library Access
- [ ] Policy Quiz Engine

## 15. Contract & Client Management
- [ ] SOW Staffing Rules
- [ ] Billing Rates Matrices
- [ ] Client Portal Dashboard
- [ ] Invoicing Reconciliation Export
- [ ] Service SLA Trackers
- [ ] Client Feedback Log
- [ ] Site Start/Stop Management
- [ ] Rate Adjustment Calculators

## 16. Lost & Found
- [ ] Found Item Intake
- [ ] Barcode Tag Generator
- [ ] Storage Location Mapping
- [ ] Owner Claim Processing
- [ ] Return Signature Logs
- [ ] Disposition Manager
- [ ] Inbound Lost Inquiries

## 17. Mass Notification & Crisis Communications
- [ ] Multi-channel Broadcasts
- [ ] Safety Status Polls
- [ ] EOC Response Dashboard
- [ ] Pre-configured Notification Templates
- [ ] User Group Directory
- [ ] Desktop Override Alerts
- [ ] Alert Escalation Rules
- [ ] Call Inbound Info Line

## 18. Threat Intelligence & OSINT Ingestion
- [ ] Public Safety Feed Ingestion
- [ ] Geofenced Keyword Monitoring
- [ ] Threat Assessment Radar
- [ ] Proactive Alerts & Alarms
- [ ] Social Media Listening
- [ ] BOLO Alert Ingest
- [ ] Threat Intelligence Archiving

## 19. Physical Security Integration Gateway (IoT / VMS / Alarms)
- [ ] Intrusion Alarm IP Listeners
- [ ] VMS Camera Stream Ingestion
- [ ] Access Control Panel Ingestion
- [ ] Fire Panel Watchdogs
- [ ] IoT Sensor Gateway
- [ ] Automated Dispatch Generation
- [ ] Alarm Pattern & Nuisance Analysis *(added during platform design review — the metadata-capture rationale for the integrate-don't-replace boundary, made a first-class feature)*
- [ ] Device Status Heartbeats
- [ ] Custom Integration Profiles

## 20. K9 & Specialized Unit Operations
- [ ] Canine Profiles
- [ ] Scent Profile Registry
- [ ] Training Logs
- [ ] Deployment & Search Logs
- [ ] Weapons Certifications Gates
- [ ] Special Equipment Registers
- [ ] Bite & Force Reports
- [ ] Tactical Operations Logs

## 21. Compliance, Self-Assessments & Audits
- [ ] Compliance Standards Matrices
- [ ] Self-Assessment Checklists
- [ ] Auditor Evidence Vault
- [ ] Audit Trail Immutability
- [ ] Policy Access Auditing
- [ ] Compliance Dashboard
- [ ] Audit Report Packages
- [ ] Vulnerability Registers

## 22. Business Continuity & Disaster Recovery (BC/DR)
- [ ] Business Impact Analysis (BIA)
- [ ] Disaster Checklists
- [ ] Alternative Supplier Registry
- [ ] Vital Records Protection
- [ ] Supply Chain Risk Logs
- [ ] RTO/RPO Metrics Dashboards
- [ ] Business Recovery Workflows
- [ ] BC/DR Tabletop Simulators

## 23. Executive Protection & Secure Transit
- [ ] VIP Travel Profiles
- [ ] Route Assessment Tool
- [ ] Advance Team Reports
- [ ] Secure In-Transit Tracking
- [ ] Armored Car Registry
- [ ] Arrival & Departure Logs
- [ ] Executive Panic Button Integration

## 24. Special Event & Incident Action Planning (IAP)
- [ ] IAP Forms Builder
- [ ] Perimeter GIS Designer
- [ ] Event Staff Schedule Roster
- [ ] Crowd Management Checklists
- [ ] Temporary Access Permits
- [ ] Local Agency Integration Log
- [ ] Post-Event De-registration Checklist

## 25. Supply Chain & Cargo Security
- [ ] C-TPAT Compliance Logs
- [ ] Container Seal Registries
- [ ] High-Value Cargo Tracking
- [ ] Warehouse Security Inspections
- [ ] Delivery Driver Validations
- [ ] Loss Prevention Audits
- [ ] Staging Area Logs
- [ ] Cargo Incident Reports
