# Sentinel-Suite Modules

This document outlines the detailed module architecture, sub-modules, and features of Sentinel-Suite.

---

## 0. Platform Core

Foundation services shared by all modules.

### Authentication & Authorization

- **Multi-Factor Authentication (MFA)**: Support for TOTP (Google Authenticator, Duo) and hardware keys (YubiKey).
- **Role-Based Access Control (RBAC)**: Fine-grained permissions mapping to specific module features and roles.
- **Single Sign-On (SSO)**: SAML 2.0 and OIDC integrations for enterprise clients.
- **Tenant Isolation**: Strict logical or database-level isolation depending on client classification.

### Structured Logging & Audit Trails

- **JSON Structured Logging**: Standardized logs for ingestion into SIEM tools.
- **Immutable Audit Logs**: Compliance-ready, tamper-evident logs for security events and records access (NIST 800-53, DOE Orders).
- **Security Event Monitoring**: Automated triggers for unauthorized access attempts or suspicious activities.

### Offline Data Sync

- **Local-First Storage**: Secure local storage (SQLite/IndexedDB) on mobile and client terminals.
- **State Sync Engine**: Store-and-forward queue to handle transactions performed while disconnected.
- **Conflict Resolution**: Conflict-Free Replicated Data Types (CRDTs) to reconcile offline edits cleanly.

### Notifications Engine

- **Delivery Channels**: In-app center, mobile/desktop push, SMS, and email notifications.
- **Priority Routing**: High-priority alert escalation (e.g., panic buttons, missed checkpoints) bypassing standard notification delays.
- **Muting & Fatigue Control**: Customizable alert routing rules to prevent security staff fatigue.

### GIS & Mapping Services

- **Real-Time GPS Tracking**: Live location plotting of active units and guards.
- **Offline Maps**: Vector tile caching for underground or remote operations.
- **Geofencing & Alerts**: Virtual perimeter definition with automated alerts on entry/exit.
- **Custom Overlays**: KML, GeoJSON, and ESRI shapefile integration for site layouts and utility mapping.

### API & Messaging Layer

- **GraphQL & REST APIs**: Flexible endpoints for internal frontend and external client integrations.
- **WebSockets**: Real-time push stream for dispatch updates and EOC dashboards.
- **API Keys & Webhooks**: Self-service key creation and webhook subscription registration for third-party tools.

### Event & Command Bus Architecture

- **CQRS Pattern**: Command Query Responsibility Segregation separating write actions from read views.
- **In-Memory & Distributed Bus**: High-performance in-memory bus with adapters for RabbitMQ, Kafka, or cloud service buses.
- **Event Sourcing Ready**: Structured event generation suitable for future event-sourced auditing.

### Settings & Preferences

- **Hierarchical Config**: Multi-level configuration (global settings, tenant-specific, region/site-specific, and individual device/user preferences).
- **Network Profiles**: Bandwidth-saving options for high-cost or low-bandwidth satellite/cellular links.

---

## 0.5. Master Records

Canonical entity registry serving as the single source of truth across the suite.

### Entity Registry Core

- **Unique Entity IDs**: Global tracking IDs linking entities across scheduling, dispatch, incidents, and access logs.
- **Entity Deduplication**: Algorithmic matching of name, email, and phone to prevent duplicate profiles.

### Person Registry

- **Unified Profiles**: Central profile repository for employees, visitors, contractors, tenants, BOLO/trespass subjects, and incident participants.
- **Metadata Fields**: Photo identification, contact information, emergency contacts, medical alerts, and language fluencies.

### Vehicle Registry

- **Vehicle Profiles**: Make, model, year, color, license plate (and state), and vehicle photos.
- **Permit Association**: Parking passes, authorization tags, and lease agreements.
- **BOLO / Violation Flagging**: Automatic warnings if the vehicle is associated with a ban or outstanding citations.

### Location Registry

- **Spatial Definition**: Geometries, polygons, and coordinate markers for physical sites, zones, buildings, and rooms.
- **Utility & Contact Directory**: Quick access to facility managers, shut-off valve locations, and local law enforcement dispatch numbers.

### Entity Relationships & History

- **Interaction Timeline**: Multi-module chronological timeline showing all scheduled shifts, access events, incidents, or citations associated with the entity.

---

## 1. Security Operations

Day-to-day security officer workflows and incident management.

### Daily Activity Reports (DAR)

- **Chronological Shift Logs**: Real-time logging of guard activities, checkpoint events, and field observations.
- **Visual Log Timelines**: Interactive visual timelines of a guard's daily progress for supervisor reviews.
- **Activity Categories**: Tagging logs (e.g., Maintenance, Security, Safety, Inquiries) for analytical filtering.

### Shift Passdowns & Handover Notes

- **Passdown Log Entries**: Read-only, immutable logs passed from shift to shift containing critical updates.
- **Required Read Confirmations**: Enforced read-acknowledgments for incoming officers before scheduling start.
- **Supervisor Verification**: Electronic sign-off workflows for shift handovers.

### Guard Tour & Checkpoint Verification

- **Multi-technology Checkpoint Scans**: Scan capabilities using NFC tags, QR codes, and geolocated GPS coordinates.
- **Interactive Tour Progress**: Supervisor dashboard displaying active tour progress against expected schedules.
- **Missed Checkpoint Alerts**: Real-time push and email alerts if critical checkpoints are skipped or delayed.

### Patrol Management

- **Route Definitions**: Multi-checkpoint route setup with post-specific instructions per node.
- **Route Randomization**: AI-guided variations in route generation to prevent predictable patrol patterns.
- **Mobile Action Checklists**: Direct presentation of inspection tasks to the guard during patrol rounds.

### Courtesy Patrol

- **Property Violations Log**: Standardized checklists for noise complaints, trash rules, and common area violations.
- **Lighting Surveys**: Focused logs for documenting dark spots, failed security lights, and visibility hazards.
- **Pool & Common Area Closures**: Step-by-step lockup checklist verification with photo logs.

### Tickets, Citations & Traffic Safety

- **Violation Logging**: Speed tracking, parking violations, fire lane blockages, and abandoned vehicle logs.
- **Mobile Citation Generation**: Instant citation compilation with mobile Bluetooth printing support.
- **Towing Coordination**: Direct interfaces to log vehicle specs and coordinate with authorized towing partners.

### Incident Reporting & Management

- **Report Approval Workflows**: Draft creation, peer review, supervisor approval, and final archiving loops.
- **Evidence Vault Integration**: Secure upload and hashing of photos, video clips, and audio files.
- **Dynamic Incident Categories**: Standardized categories aligned with ASIS or customized tenant schemas.

### AI-Assisted Incident Report Writing

- **Narrative Draft Generation**: Converts bulleted timelines and logs into a cohesive, professional narrative.
- **Voice-to-Text Transcription**: Native transcription of voice-recorded field notes into report templates.
- **Policy Compliance Check**: Automated reviews scanning for necessary details (e.g., date, notifications, police case numbers).

---

## 2. Dispatch / CAD (Lite-to-Mid)

Real-time dispatch and coordination for security operations.

### Call Intake & Logging

- **Call-Taker Workspace**: Inbound phone call logging, emergency flag selectors, and caller information retrieval.
- **Caller Context Lookup**: Cross-referencing phone numbers against Master Records to pull visitor or employee profiles.
- **Call Priority Queuing**: Category-based prioritization (Priority 1: Emergency to Priority 4: Routine).

### Unit Dispatch & Proximity Routing

- **Live Location Tracking**: Real-time mapping of patrol vehicles and mobile officers.
- **Closest Unit Recommendation**: Automated system calculation of closest units based on GPS metrics.
- **Route Navigation**: Sending optimized directions directly to the responding unit's mobile app.

### Active Incident Queue (CAD Console)

- **Active CAD Dashboard**: Multi-column board displaying active dispatches, call hold timers, and unit states.
- **Incident Merging**: Ability to link multiple calls related to the same event to a single CAD ticket.
- **Dispatch Notes Feed**: Real-time timeline logs of dispatcher edits and notes.

### Status & State Monitors

- **Unit State Controllers**: Tracking statuses (AVAILABLE, DISPATCHED, ENROUTE, ONSCENE, COMPLETED, OUT_OF_SERVICE).
- **Time-on-Scene Watchdogs**: Visual alert timers highlighting units on scene longer than thresholds.
- **Officer Check-in Safety Timers**: Automated pings requiring officers to check in during hazardous calls.

### Silent Mobile Dispatching

- **Mobile CAD Notifications**: Delivery of CAD data directly to mobile devices, bypassing voice radio.
- **Tactical Message Threading**: Two-way silent chat between the dispatcher and responding officers.
- **Premises Hazard Alerts**: Automated notifications of site-specific hazards (e.g., hazmat, aggressive dogs).

### Multi-Incident Console

- **Split-Screen Workspace**: Workspace layouts for dispatchers managing multiple concurrent emergencies.
- **Incident Escalation Control**: One-click EOC alert activation for fast-developing crises.
- **External Agency Handoff Logs**: Record templates documenting handoffs to local police or fire departments.

### Active Call Alerts & Timers

- **Pending Call Alarms**: Sound alerts in the dispatch room if critical calls are not dispatched in time.
- **Enroute Timers**: System warnings if units do not arrive on scene within expected time windows.
- **Supervisor Notifications**: Automated escalations to managers for unresolved critical events.

### Historical CAD Log Reconstruction

- **Log Timestamps**: High-precision audit logs recording every state change and command.
- **Audio Integration Links**: Linking radio logs or recorded phone calls to CAD incident files.
- **Regulatory Reporting Exports**: Standardized exports compiling CAD dispatch logs for legal audits.

---

## 3. Command Center / Dashboard / EOC

Real-time operational awareness and emergency operations coordination.

### Unified Operational Picture (UOP) Map

- **Live GIS Map Layout**: Dynamic map displaying GPS positions, active calls, alarm zones, and camera locations.
- **Icon Clustering**: Visual clustering of incidents and assets in high-density areas.
- **Geofence Overlays**: Displaying virtual boundaries, site boundaries, and patrol sectors.

### Command Center Wallboard View

- **Multi-Monitor Display Profiles**: Layout presets designed for wall-mounted TV arrays.
- **System Health Monitor**: Live charts displaying connectivity, active tours, and integration statuses.
- **Alarm Alert Banners**: Flashing banner alerts at the top of screens when critical events occur.

### ICS Role Mapping & Visual Org Chart

- **ICS Org Chart Builder**: Interactive organizational chart defining incident command roles.
- **Role Logins**: Linking logged-in security users to active ICS roles (e.g., Incident Commander).
- **Incident Command Post Registry**: Defining coordinates and facilities acting as the command posts.

### Situation Reports (SITREPs)

- **Collaborative Writing Editor**: Real-time text collaboration for EOC staff to compile updates.
- **SITREP Distribution Rules**: Automatic email and push lists for sending updates to executives.
- **Incident Timeline Ingestion**: Direct inclusion of CAD timeline logs into situation summaries.

### Live Camera Feed Ingestion

- **CCTV Map Links**: Clicking camera icons on the map to stream RTSP/WebRTC feeds.
- **Camera Auto-Popup**: System triggers popping up camera streams when alarm sensors near the camera trip.
- **PTZ Mobile Controls**: Remote controls for camera pan, tilt, and zoom directly inside the EOC console.

### EOC Logistics Hub

- **Resource Booking Panels**: Board managing available emergency resources (generators, barricades, vehicles).
- **Resource Staging Logs**: Tracking logistics orders from request through transport and staging.
- **Mutual Aid Registries**: Directory of contact info and capabilities for external response organizations.

### Alarm Panel Monitors & Panic Alerts

- **Panic Alert Watchdogs**: Audio alerts when panic buttons are clicked in client offices.
- **Intrusion Alarm Console**: Direct visual monitors showing tripped door sensors or glass break alerts.
- **Fire Panel Status Indicators**: High-priority alert indicators syncing with facility fire panel outputs.

### Environmental & Weather Map Overlays

- **Radar Ingestion**: Real-time weather radar maps overlaid directly on the GIS layout.
- **Severe Weather Alert Ingest**: Automatic notifications from the National Weather Service.
- **Traffic Congestion Logs**: Traffic maps displaying road blocks and evacuation speeds.

### Historical Playback Console

- **GPS Path Playbacks**: Replaying the historical tracking of patrol vehicles during an incident.
- **EOC Event Replays**: Syncing video feeds, CAD dispatches, and alarm logs on a historical timeline.
- **Post-Incident Analysis Review**: System logs documenting training evaluations and speed studies.

---

## 4. Access Control

Managing physical access and visitor check-in.

### Pre-Registration Portal

- **Tenant Portal Access**: Portal for building tenants to log scheduled visitors and contractors.
- **Email QR Access Passes**: Automatic generation and emailing of visitor QR passes.
- **Guest Verification Rules**: Configurable limits on visitor stays and site access windows.

### Visitor Kiosk App

- **Self-Service Check-In**: Kiosk application supporting QR scans, name lookups, and manual entries.
- **Digital Document Signing**: Document templates for signing NDAs, safety disclosures, and liability waivers.
- **Badge Printing Output**: Direct printing of thermal visitor badges upon check-in.

### Host Arrival Notifications

- **Automated Text & Email Alerts**: Real-time notifications to site hosts upon guest arrival.
- **Host Confirmation Responses**: Ability for host to reply via SMS to confirm check-in approval.
- **Escalation Rules**: Automated alerts to lobby guards if a host is unreachable.

### Access Credential Management

- **Keycard & Fob Assigners**: Tool mapping physical credentials (HID, Desfire) to master profiles.
- **Biometric Templates Enrollment**: Enrolling fingerprint or facial recognition parameters.
- **Badge Print Layout Tool**: Custom builder for designing company ID badges.

### Clearance Profiles

- **Time-restricted Clearance Levels**: Group boundaries limiting access to specific hours and days.
- **Zone Boundaries**: Restricting card clearances to specific buildings, rooms, or zones.
- **Temporary Clearances**: Expiration rules automatically removing access credentials for contractors.

### Key Ring Registry

- **Physical Key Inventory**: Directory of physical master keys, sub-masters, and key rings.
- **Lock Cylinder Mapping**: Cross-referencing specific keys to physical lock numbers and locations.
- **Replacement Schedules**: Database of lock replacement cycles and locksmith orders.

### Key Custody & Auditing

- **Digital Custody Signatures**: Signature pads capturing custody transfers on key handouts.
- **Overdue Key Alarms**: System alerts if critical keys are not returned before shift end.
- **Lockbox Access Logs**: Integration linking keys to physical electronic key boxes (e.g., Traka).

### Lock Core & Cylinder Tracking

- **Core Replacement Logs**: Documenting structural changes to cores, pins, and cylinders.
- **Keyway Design System**: Charts tracking keyway hierarchy patterns and key restrictions.
- **Lock Maintenance Logs**: Work records tracking locksmith tasks, repairs, and audits.

### BOLO & Trespass Alerts

- **BOLO Photo Logs**: File records of banned, trespassed, or flagged individuals.
- **Automated Kiosk Block**: Automated check-in blocks if visitor matches a BOLO profile.
- **Instant Silent Alarm**: Push notifications to dispatch and lobby guards during matches.

### Remote Gate & Barrier Controls

- **Remote Open Controls**: Desktop buttons to trigger gates, dock doors, or lobby turnstiles.
- **Intercom Ingest**: Audio/video links populating gate controls when visitors press gate buzzers.
- **Override Auditing**: Immutable log tracking every remote gate override action.

---

## 5. Emergency Management

Coordinating emergency response operations and compliance.

### ICS Forms Engine (FEMA-aligned)

- **ICS Form Templates**: Generating standard ICS forms 201, 202, 203, and 204.
- **Digital Distribution**: Compiling plans into PDF briefs sent to command structures.
- **Archiving Regulations**: Storage database keeping past incident plans for compliance audits.

### Resource Logistics Catalog

- **Logistics Registries**: FEMA-typed lists of safety equipment, emergency vehicles, and supplies.
- **Deployment Status Tracking**: Dashboard tracking if resources are AVAILABLE, STAGED, DEPLOYED, or DECOMMISSIONED.
- **Reorder Warnings**: Automatic notifications if emergency supply counts fall below thresholds.

### After-Action Reports (AAR)

- **Timeline Reconstruction Tool**: Aggregating data from CAD logs, incident records, and notes.
- **Improvement Action Plans**: registers detailing recommendations and mitigation owners.
- **Executive AAR Briefings**: Compiling report templates for safety directors.

### Improvement Plan (IP) Tracking

- **Action Item Registry**: Central registry tracking corrective tasks from drills or incidents.
- **Task Deadlines**: Automated notification reminders as completion deadlines approach.
- **Evidence Files**: Document logs documenting implementation proof (photos, training sign-in sheets).

### Exercise & Drill Planner (HSEEP-aligned)

- **HSEEP Form Templates**: Forms for designing tabletop, functional, and full-scale exercises.
- **Scenario Inject Engines**: Database storing simulated scenario inputs (injects) for training.
- **Evaluation Criteria**: Scorecards evaluating participant performance during events.

### Drill Compliance Logging

- **Compliance Registries**: Logs recording regulatory fire drills, evacuations, and shelter drills.
- **Occupant Exit Reports**: Documenting evacuation speeds and exit times.
- **Corrective Actions**: Automatic work order triggers for failed alarm components or exit blocks.

### Mutual Aid Agreements Tracker

- **MOU/MOA Database**: Document archive for memorandums of understanding with nearby agencies.
- **Resource Directory**: Contact lists and capabilities of local fire, police, and private medical units.
- **Review Reminders**: Automatic notifications when agreement renewals are needed.

### EOC Activation Checklists

- **Activation Checklist Logs**: Step-by-step instructions for escalating from normal operations to EOC activation.
- **Staff Call-up Templates**: Broadcast templates notifying EOC personnel to report to stations.
- **System Checklists**: Verification steps for EOC backup power, radios, and emergency systems.

---

## 6. Emergency Planning

Proactive emergency preparedness documentation and assessments.

### Pre-Incident Plans (Preplans)

- **Preplan binders**: Interactive records detailing building profiles, shut-off locations, and safety structures.
- **Utility Hazard Mapping**: GIS markers showing main water, gas, electricity, and sprinkler controls.
- **Printable Briefs**: Emergency summaries designed to print quickly for responding municipal fire crews.

### Incident Action Checklists

- **Checklist Databases**: Pre-configured checklists for active shooters, spills, weather events, and outages.
- **Mobile Task Assignments**: Assigning checklist actions to field personnel via mobile app.
- **Status Progress Monitors**: Dispatch indicators tracking completed tasks during response plans.

### Continuity of Operations Plans (COOP)

- **Essential Function Registry**: Prioritized list of business functions that must survive disruptions.
- **Succession Hierarchies**: Legally aligned delegation maps defining leadership orders.
- **Backup Data Repositories**: Lists detailing cloud backup paths and vital records locations.

### Muster Check-in App

- **NFC/Barcode Muster Check**: Scanning employee badges at muster assembly points during evacuations.
- **Search Registries**: Direct link to CAD listing individuals marked "missing" or "not checked in".
- **Roster Updates**: Live muster list syncing with access control swipe logs.

### Evacuation Roster Reconciliation

- **Evacuation Dashboard**: Live statistics showing muster progress percentages.
- **Access Reconciliation logs**: Cross-referencing check-in numbers with access system swipe databases.
- **Incident Command Links**: Instant notification to the command post if employees are not checked in.

### Hazard Identification & Risk Assessment (HIRA)

- **Risk Assessment Matrix**: Evaluating threat types against probability and impact scores.
- **Threat Directories**: Assessment histories for weather threats, human threats, and structural failures.
- **HIRA Reports**: Compilation templates exporting risk summaries for executive planning.

### Mitigation Task Tracker

- **Remediation Task Boards**: Project board assigning security adjustments to facility technicians.
- **Budget Tracking Logs**: Cost monitoring for safety updates (e.g., adding barriers, gates).
- **Audit Trails**: Documenting risk reduction updates to prove safety improvements.

### Alternate Site Registries

- **Relocation Facility Directories**: Detail sheets for designated backup EOC and recovery offices.
- **Resource Specifications**: Verification charts showing power, data, and workspace details at alternate sites.
- **Transit Plan Guides**: Pre-configured routing maps detailing transport steps for staff.

---

## 7. Safety Management

Workplace safety compliance and hazard management.

### Safety Inspections & Audits

- **Auditing Templates**: Checklist engine for fire extinguisher checks, exit sign walks, and OSHA compliance.
- **Inspection Schedules**: Calendar interface managing daily, weekly, or monthly safety audits.
- **Photo Evidence Capture**: Image attachments showing safety issues directly in audit files.

### Corrective Action Pipelines

- **Work Order Generation**: Automated ticketing for failed safety items.
- **Tracking Boards**: Interface tracking repairs from report to technician sign-off.
- **Verification Logs**: Supervisor review process confirming hazards are remediated.

### Hazmat & Chemical Registries

- **SDS Database Search**: Search portal indexing Safety Data Sheet PDFs by chemical name or manufacturer.
- **Inventory Logs**: Database tracking storage sites, quantities, and CAS registration numbers.
- **Regulatory Limit Warning**: Automatic notices if chemical weights exceed EPA threshold limits.

### NFPA 704 Placard Mapping

- **NFPA Diamond GIS Markers**: Mapping placards showing toxicity, flammability, instability, and special hazards.
- **Safety Preplans Integration**: Linking placard details directly to emergency preplans.
- **Emergency Spill Procedures**: First responder guidelines linked to chemical hazard markers.

### OSHA Incident Loggers

- **OSHA 300/300A/301 Logs**: Automatic form generation based on safety and injury incidents.
- **Electronic Submissions**: Preparing XML formatted data files for OSHA injury reporting sites.
- **Worker Comp Integration**: Form registers linking safety events to insurance systems.

### Safety Metrics Calculators

- **DART Metric Calculator**: Auto-calculating Days Away, Restricted, or Transferred incidence metrics.
- **TRIR Calculator**: Real-time calculation of Total Recordable Incident Rates.
- **KPI Trends Dashboard**: Analytics charting hazard counts and injury frequencies over time.

### Industrial Hygiene Tracking

- **IH Monitoring logs**: Recording indoor air quality, noise exposures, radiation levels, and chemical tests.
- **Calibration Databases**: Expiration alerts for noise decibel meters and radiation detectors.
- **Exceedance Alerts**: Push notifications if noise or air tests exceed permissible limits.

### PPE Inventory & Issuance

- **PPE Asset Registers**: Stock inventory levels for hard hats, safety glasses, vests, and respirators.
- **Issuance Logs**: Tracking safety equipment assignments to specific guards and employees.
- **Fit-Testing Logs**: Recording respirator fit-testing dates and results.

### Near-Miss Incident Portal

- **Anonymous Reporting Portal**: Web form allowing employees to report near-miss safety hazards.
- **Risk Assessment Board**: Screening near-miss submissions to assign priority levels.
- **Safety Notice Alerts**: Generating safety bulletin emails for staff warning of near-miss issues.

---

## 8. Personnel

Security workforce management, compliance, and scheduling.

### Post Schedule Builder

- **Visual Roster Calendars**: Drag-and-drop schedule builder with post coverage filters.
- **Roster Templates**: Preset schedule templates designed for repetitive shift rotations.
- **Overtime Alert Monitors**: System warnings if schedule assignments exceed weekly work limits.

### Open-Shift Bidding

- **Shift Notification Alerts**: Push alerts notifying qualified officers of available open shifts.
- **Bidding Rules**: Assigning priority based on seniority, qualification levels, and overtime limits.
- **Automated Approvals**: Auto-approvals and roster updates upon successful shift claims.

### Geofenced Time & Attendance

- **Geofenced Clock-in**: Enforcing GPS boundaries for shift clock-ins on the mobile app.
- **Time Clock Registers**: Database tracking timesheets, adjustments, and shift codes.
- **No-Show Warnings**: Real-time dispatch alerts if an officer fails to clock in.

### Training Curriculum Library

- **Training Course Databases**: Managing course profiles, training videos, and requirements.
- **Certificates Registry**: Digital archive storing uploaded copies of training certificates.
- **Renewal Alert Alerts**: SMS and email warnings prior to certificate expirations.

### Licensing & Guard Card Tracking

- **Guard Card Registry**: Database tracking state security license registrations and documents.
- **Application Workflows**: Checklist tracking new hire background checks, drug screenings, and card orders.
- **Expiration Alarms**: System blocks locking out guards with expired card records.

### Armed Qualifications Registry

- **Weapons Permit Logs**: Database tracking armed guard card statuses and weapons registrations.
- **Range Score Logs**: Tracking qualification dates, range scores, and weapon types.
- **Re-qualification Alerts**: Automated calendar reminders for required range times.

### Compliance Schedule Lockouts

- **Roster Compliance Checker**: Automated audit blocks checking rosters against credential files.
- **Post-Level Checks**: Restricting schedules to ensure shifts require specific credentials (e.g., CPR card).
- **Audit Logs**: Immutable log tracking scheduling overrides and authorizations.

### Skills & Capabilities Profiles

- **Skills Directory**: Employee profiles indexing language fluencies, EMT cards, and secure clearances.
- **Duty Availability Filters**: Searching personnel by skills during emergencies.
- **Security Clearances Registers**: Database tracking employee federal, DOE, or client clearances.

---

## 9. Facility & Zone Management

Physical location hierarchy, assets, and maintenance.

### Location Hierarchy Designer

- **Visual Tree Builder**: Hierarchy manager mapping relationships of sites, buildings, floors, and rooms.
- **Hierarchy Mapping**: Direct associations linking physical sites to responsible account managers.
- **Location Status indicators**: Colors showing normal operations, maintenance lockdowns, or alarms.

### Access Point Bindings

- **Access Point Mapping**: Associating card readers, lock cores, and access gates to specific rooms.
- **Camera Association Maps**: Linking camera streams to access readers to verify entry profiles.
- **Hardware Profile Registers**: Technical specs, IP addresses, and locations for readers.

### Zone Mapping (GIS)

- **Patrol Zone Geofences**: Drawing boundary zones for specific security patrols.
- **First Responder Zones**: Mapping fire response sectors and evacuation assembly areas.
- **GIS Layout Overlays**: Dynamic layer settings for custom maps.

### Work Order Dispatching

- **Work Request Portal**: Service forms for reporting doors, lights, or locks needing repairs.
- **Work Order Tracking**: Progress updates, assignment files, and technician logs.
- **Completion Checkpoints**: Supervisor review processes confirming repairs meet standards.

### Tenant & Occupant Registry

- **Occupant Registers**: Directory of residents, building tenants, and key contacts.
- **Business Hours Directories**: Operating hours, phone listings, and parking spots.
- **Special Assistance Logs**: Confidential register of tenants requiring assistance during evacuations.

### Utility Control Tracking

- **Utility Shut-off Directories**: Location database for main water, gas, electricity, and fire systems.
- **Operational Guides**: Step-by-step safety steps for utility shut-offs.
- **GIS Markers**: Utility control locations marked on map interfaces.

### Spatial Asset Inventory

- **Asset Placement Maps**: Map mapping assets (laptops, panels, tools) to specific rooms.
- **Transfer Logs**: Documenting asset movement from one building zone to another.
- **Audit Checklists**: Location checklists to verify assets during inventory rounds.

---

## 10. Equipment, Assets, Vehicles & Resources

Tracking and managing physical resources.

### Asset Barcode Registry

- **Asset Registers**: Catalog of serial numbers, tag numbers, brands, models, and purchase details.
- **Category Tags**: Tag filters (Radios, Body Cams, Armor, Laptops, Mobile Terminals).
- **Calibration Schedules**: Record logs managing maintenance dates and warranties.

### Barcode Checkout Station

- **Rapid Check-out/Check-in**: Scan portal utilizing barcodes or RFID to transfer asset custody.
- **Officer Custody Timelines**: Active registers tracking which guard holds specific assets.
- **Overdue Return Alarms**: Daily notices if check-out times exceed shift length.

### Calibration & Maintenance Alerts

- **Maintenance Records**: Logs tracking diagnostics, repairs, and calibration tests.
- **Service Alert Warnings**: Push reminders when equipment is due for scheduled calibration.
- **Downtime Logs**: Documenting offline assets and replacement schedules.

### Vehicle Logbooks

- **Mileage Tracking Logs**: Log registers tracking vehicle odometer readouts per shift.
- **Shift Inspection Checklists**: Standard vehicle checklists (tires, fluids, damage checks) completed by guards.
- **Damage Photos Vault**: Digital archive of photos documenting vehicle scrapes or dents.

### Fuel Card & Expense Log

- **Transaction Logs**: Log registry for fuel purchases, locations, and gallon amounts.
- **Economy Analytics**: Reports tracking miles-per-gallon metrics per vehicle.
- **Odometer Validation Check**: Checking fuel entry odometer readings against vehicle logbooks.

### Weapons Inventory

- **Armory Registers**: High-security database of firearm serials, models, and storage spots.
- **Weapon Inspections logs**: Recording cleaning records, firing pin checks, and test logs.
- **Custody Audit Trails**: Complete chain of custody logs tracking weapon transfers.

### Ammunition Registry

- **Ammunition Inventory Logs**: Stock logs tracking caliber quantities and box counts.
- **Issuance Logs**: Recording rounds issued and returned per shift.
- **Disposal & Range logs**: Tracking ammo counts spent during training courses.

### Armory Compliance Gate

- **Qualification Checkers**: Automatic blocks checking if a guard has valid weapons certifications.
- **Roster Checkups**: Cross-checking weapons issuance against scheduled armed shifts.
- **Override Audit Logs**: Recording security exceptions authorized by managers.

### Depreciation & Asset Retirement Logs

- **Depreciation Logs**: Valuation logs tracking lifecycle worth of assets.
- **Decommissioning Checklists**: Standardized processes for wiping memory and disposing of assets.
- **Destruction Evidence Vault**: Certificates of destruction and disposal receipts.

---

## 11. Subcontractor Management

Oversight of subcontracted security agencies.

### Vendor Profiles

- **Vendor Registry**: Directory of subcontractor agencies, contracts, and contact lists.
- **Service Area Registers**: Mapping subcontractor services to specific sites and regions.
- **Agreement Archives**: Document library for master service agreements.

### Insurance Compliance Audits

- **COI Database**: tracking Certificate of Insurance documents with expiration notifications.
- **Renewal Alert Reminders**: Email reminders to vendors 60 and 30 days prior to policy expirations.
- **Compliance Lockouts**: Automated notices to scheduling if a vendor's insurance expires.

### Roster Verifications

- **Vendor Schedule Auditing**: System checking vendor schedule submissions against contracts.
- **Attendance Verification logs**: Match records comparing card reader logs to vendor timesheets.
- **Unauthorized Guard Alerts**: Real-time warnings if a vendor employee checks in without a profile.

### SLA Scorecard Generator

- **Performance scorecards**: Dashboard scoring subcontractors on shift coverage and tour completion rates.
- **Penalty Fee Calculators**: Auto-calculating credits or deductions for missed shifts.
- **Executive Evaluations**: Standard evaluation reports for vendor contract renewals.

### Subcontractor License Checks

- **Subcontractor License Database**: Tracking agency security license registrations.
- **Guard Card Audits**: Database validating guard card records for vendor staff.
- **Compliance Checks**: System checking vendor guard profiles before granting check-in rights.

### Billing Verification Logs

- **Time Reconciliation Console**: System matching vendor shift records with platform check-in logs.
- **Discrepancy Flags**: Highlighting timesheet differences exceeding threshold limits.
- **Export Invoices**: Document summaries preparing verified shift hours for accounting approvals.

### Vendor Audit Checklists

- **Audit Checklist Logs**: Review forms managing subcontractor training files and background checks.
- **Corrective Action Requests**: Task board assigning contract modifications to vendors.
- **Audit Scores Registry**: Historical database tracking vendor audit results.

---

## 12. Performance & KPI Reporting

Operational analytics and performance measurement.

### Custom Query & Report Builder

- **Drag-and-Drop Query Editor**: Custom report layout tool allowing managers to compile fields.
- **Export format settings**: Formatting formats (PDF, CSV, Excel, XML, JSON).
- **Report Templates Registry**: Catalog of saved query layouts for team access.

### Visual Dashboard Configurator

- **KPI Dashboards**: Custom charts (bar, line, pie) and geographic widgets.
- **Dashboard Templates**: Layout profiles for executives, dispatchers, and clients.
- **System Theme Options**: Visual display options including sleek dark modes.

### Automated Emailed Reports

- **Report Scheduling Console**: Interface managing daily, weekly, or monthly report distributions.
- **Recipients Groups Directory**: Directory managing email rosters for client groups.
- **Trigger-based Reports**: Automatic reports compiled and sent if specific alarm events occur.

### Response Time Analytics

- **Response Speed Metrics**: Average call response times calculated by location and priority.
- **Dispatch Times Dashboard**: Tracking times from call creation to unit dispatch.
- **SLA Threshold Compliance**: Analytics measuring compliance against contract response speeds.

### Tour Completeness Logs

- **Tour Analytics Dashboards**: Reporting percentages of completed guard tours against schedules.
- **Missed Checkpoint Trends**: Charting skip rates by checkpoint locations.
- **Officer Tour Comparison**: Rankings tracking tour completion speeds by guard.

### Incident Location Heatmaps

- **GIS Heatmap Layer**: Visual overlays displaying incident densities on site maps.
- **Incident Category Filters**: Filtering maps to show specific trends (e.g., vandalism vs alarms).
- **Time-of-Day Heatmaps**: Time filters mapping incident locations by shift.

### Officer Performance Logs

- **Guard Activity scorecards**: Reporting shift activities, checkpoints scanned, and dispatches handled.
- **Incident Quality Scores**: Metrics tracking incident report return rates for corrections.
- **Commendation & Complaint Registries**: Database tracking client feedback and guard records.

### Tenant SLA Metrics

- **Contract Fulfillment Reports**: Verification reports confirming security coverage against SOW rules.
- **Client KPI Briefs**: Clean dashboards designed for quarterly client reviews.
- **Billing Auditing Registers**: Document listings mapping service metrics to invoice summaries.

---

## 13. Investigation Management

Structured case management and evidence collection.

### Case Files

- **Case Registry**: Directories tracking case IDs, investigators, statuses, and classifications.
- **Cross-Reference Links**: Linking related incident reports, CAD logs, and person profiles.
- **Case Access Logs**: Audit logs tracking which users view case files.

### Digital Evidence Locker

- **Cryptographic File Hash Vault**: Hashing uploaded photos, video files, and documents to confirm file integrity.
- **Evidence Uploader**: Secure upload portal for digital evidence.
- **Chain of Custody Logs**: Immutable logs tracking digital evidence access and downloads.

### Physical Evidence Tracking

- **Evidence Label Creator**: Generating barcode labels for physical storage boxes.
- **Evidence Locker Maps**: Map tracking bin, shelf, and locker storage spots.
- **Audit Checklist Logs**: Checklists managing periodic evidence inventories.

### Interview Statement Logs

- **Statement Templates**: Form templates for recording statements.
- **Witness Registers**: Directory tracking witnesses, contact details, and dates.
- **Audio File Attachment Vault**: Securing recorded interview audio files within case files.

### Audio/Video Transcription

- **Transcription Engines**: Tool converting audio interview files into text documents.
- **Text Sync Player**: Audio player syncing audio playback with matching text lines.
- **Correction Workspace**: Editor interface allowing investigators to adjust automated transcriptions.

### Case Dispositions

- **Disposition Registries**: Case closures (Referred to Police, Closed - No Action, Pending Prosecution).
- **Prosecution Prep Checklists**: Verification checklists tracking files prepared for prosecutors.
- **Expungement Workflows**: Schedules managing deletion of records per data privacy laws.

### Task Logs & Leads Tracker

- **Leads Tracker Board**: Board managing investigative leads and assignments.
- **Task Deadlines**: Reminders and alerts for critical case tasks.
- **Case Progress Indicators**: Status indicators showing open task percentages.

### Search & Seizure Registers

- **Warrant Log files**: Record vaults tracking search warrants, issuing judges, and dates.
- **Seized Property Inventories**: Barcode logs tracking property seized during warrant executions.
- **Return of Property Logs**: Documenting return transactions and owner signatures.

---

## 14. Policy & Document Management

Centralized repository for policies, SOPs, and notification lists.

### Post Orders Management

- **Post Order Editors**: Site-specific post order documents with visual map links.
- **Version Control Registries**: History files tracking edits, approvers, and modification dates.
- **Post Association Trees**: Mapping post orders to specific location hierarchy nodes.

### Digital Acknowledgment Logs

- **Read & Acknowledge Gates**: Restricting clock-ins until guards check off new post orders.
- **Compliance Dashboards**: Reporting percentages of team members who acknowledged updates.
- **Auditor Report Generators**: Compilation tools preparing policy sign-off records.

### Emergency Call Lists

- **Call Chain Directories**: Priority contact directories listing staff call orders.
- **Escalation Rules**: Rules shifting calls to backup managers if numbers do not answer.
- **GIS Integration**: Linking local police/fire emergency numbers to specific facility profiles.

### Standard Operating Procedures (SOP) Library

- **SOP Search Portals**: Search tool indexing corporate safety policies and guidelines.
- **Category Folders**: Folders managing access by department (Security, EOC, Fire).
- **Access Restrictions**: Restricting SOP views based on role and clearance level.

### Document Revision Auditing

- **Revision Logs**: Audit records tracking edits, editors, and approval signatures.
- **Document Compare Views**: Side-by-side text comparisons highlighting draft changes.
- **Approval Workflow Pipelines**: Routing drafts from authors to safety directors for approval.

### Shared Library Access

- **Tenant Folders**: Client-accessible folders containing shared building guides.
- **File Uploader**: Secure uploader for publishing newsletters or safety updates.
- **Download Tracking logs**: System logs tracking guest file downloads.

### Policy Quiz Engine

- **Quiz Builders**: Test tool allowing managers to draft comprehension quizzes for new policies.
- **Passing Requirements**: Enforcing minimum scores to unlock schedule assignments.
- **Score Registries**: Database tracking employee quiz scores.

---

## 15. Contract & Client Management

Managing relationships, contracts, and service obligations.

### SOW Staffing Rules

- **Contract Rules Registers**: SOW tables defining billing codes, shift requirements, and roles.
- **Coverage Auditing Checkpoints**: System tracking scheduled posts against SOW mandates.
- **Discrepancy Alerts**: Real-time alerts to managers if posts are under-staffed.

### Billing Rates Matrices

- **Rate Calculators**: Matrices managing billing rates, holiday pay rules, and overtime adjustments.
- **Client Exemption Registers**: Database tracking custom pricing agreements by site.
- **Rate Change Timelines**: Scheduled updates to billing rates.

### Client Portal Dashboard

- **Client Dashboards**: Secure client portal showing active calls, tours, and shift logs.
- **Client Feedback Portal**: Client contact templates for submitting inquiries or service requests.
- **Report Archives**: Secure download area for approved client reports.

### Invoicing Reconciliation Export

- **Billing Export Tool**: Compiling shift timesheets into invoicing templates.
- **Verified Invoice Logs**: Billing summaries for audit cross-checks.
- **Accounting Integration Links**: Exports format matching accounting software formats.

### Service SLA Trackers

- **SLA Dashboard**: Scorecards tracking response speeds, tour scores, and staffing averages.
- **SLA Breach Warnings**: Automated warning notifications to account managers if scores drop.
- **Audit Logs**: Recording service metrics compiled for SLA audits.

### Client Feedback Log

- **Survey Builders**: Survey tool generating feedback forms for client representatives.
- **Complaint Registers**: Logging customer complaints, resolution owners, and progress logs.
- **Satisfaction scorecards**: Analytics tracking customer review ratings over time.

### Site Start/Stop Management

- **Onboarding Checklists**: Tasks managing keys, maps, and schedules for new properties.
- **Offboarding Workflows**: De-registering credentials, returning keys, and archive tasks.
- **Site Status Timelines**: Timeline log of site openings, pauses, and closures.

### Rate Adjustment Calculators

- **Budget Simulators**: Financial tools modeling margin impact from wage edits.
- **Wage Adjuster Worksheets**: Setting target pays and margins by role.
- **SOW Cost Estimators**: Cost estimates for prospective clients.

---

## 16. Lost & Found

Property cataloging, chain of custody, and claim workflows.

### Found Item Intake

- **Item Intake Forms**: Forms cataloging found dates, item descriptions, brands, and colors.
- **Categorization Trees**: Classification dropdowns (Electronics, Wallets, Keys, Luggage).
- **Finder Registers**: Logging finder name, phone, and employee ID.

### Barcode Tag Generator

- **Barcode Printers**: Integration with thermal label printers to print property tags.
- **Unique Item IDs**: System generating tracking barcodes.
- **Scan Workspace**: Quick barcode search tool for retrievals.

### Storage Location Mapping

- **Locker Directories**: Mapping item locations to specific bins, shelves, and safe compartments.
- **Vault Access Logs**: Tracking custody access to high-value lost storage cabinets.
- **Capacity Monitors**: Dashboard tracking available storage space in lockers.

### Owner Claim Processing

- **Claim Matcher**: System searching inventory based on lost item descriptions.
- **Verification Question Logs**: Questions used by staff to verify claim details.
- **Approval Actions**: Supervisor approval checklists for releasing high-value items.

### Return Signature Logs

- **Receipt Signature Panels**: Digital signature capture for owner handovers.
- **ID Verification Checks**: Verifying photo identification checks during pickups.
- **Release Confirmation Email**: Automatic email receipt confirmation to claim owners.

### Disposition Manager

- **Disposition Logs**: Documenting final item statuses (Returned, Donated, Destroyed, Police Handover).
- **Donation Receipts Vault**: Digital database storing charitable donation paperwork.
- **Destruction Witnesses logs**: Sign-off logs documenting witnessed destructions of items.

### Inbound Lost Inquiries

- **Lost Inquiry Forms**: Portal for occupants and guests to file lost item reports.
- **Automatic Matching Engine**: Checking lost inquiry forms against new found item logs.
- **Notification Alerts**: Automatic SMS/Email alerts if matches are found.

---

## 17. Mass Notification & Crisis Communications

Outbound emergency notification dispatching to occupants and community cohorts.

### Multi-channel Broadcasts

- **Broadcast Consoles**: Outbound notification hubs sending alerts via SMS, email, voice call, and push.
- **Language Translations**: Translating alerts to multi-language options based on profiles.
- **High-volume Delivery Logs**: Dashboards tracking sent, delivered, and failed delivery counts.

### Safety Status Polls

- **Check-in Workflows**: Dynamic text polling asking occupants to reply with safety updates.
- **Safety Status Registers**: Database matching replies to employee directory profiles.
- **Follow-up Escalations**: Automated reminders to users failing to reply to check-in texts.

### EOC Response Dashboard

- **Check-in Consoles**: EOC status board displaying safety statistics.
- **Assistance Map Coordinates**: Map displaying GPS coordinates for users requesting aid.
- **Group Filtering Tools**: Filtering response statuses by facility, department, or supervisor.

### Pre-configured Notification Templates

- **Template Library**: Pre-built alerts for active threats, fires, gas leaks, and weather.
- **Custom Variables Editor**: Dynamic text fields (e.g., location, time, evacuation route).
- **Drill Broadcast Selectors**: Flagging alerts to clearly indicate training exercises.

### User Group Directory

- **Dynamic Group Builders**: Auto-building lists by facility, card scan histories, or shifts.
- **Access Group Sync**: Syncing distribution lists with tenant registries.
- **Opt-in Visitor Registers**: Guest sign-up lists for temporary emergency alerts.

### Desktop Override Alerts

- **Desktop Alerts Integration**: popups flashing hazard warnings onto corporate screens.
- **Audible Alert Controls**: Remote controls triggering audible alarms on client systems.
- **Lockscreen Overrides**: Restricting desktop tasks until safety alerts are checked off.

### Alert Escalation Rules

- **Escalation Timers**: Rules sending alerts to senior management if EOC responses delay.
- **Delivery Channel Escalation**: Shifting SMS messages to voice calls if delivery fails.
- **Supervisor Verification Logs**: Audit trail logs of escalations.

### Call Inbound Info Line

- **Interactive Voice Response (IVR)**: Pre-recorded hotline directories for incoming crisis calls.
- **Emergency Message Recorder**: Voice recorder tools for safety managers to update hotline lines.
- **Call Volume Dashboards**: Inbound call trackers measuring hotline usage.

---

## 18. Threat Intelligence & OSINT Ingestion

Automated monitoring of external threat vectors to inform proactive responses.

### Public Safety Feed Ingestion

- **Municipal CAD Ingestors**: Ingesting police, fire, and medical dispatches near properties.
- **Weather Feed Watchdogs**: Live storm tracking integrations using NWS API feeds.
- **Seismic Alert Monitors**: Global USGS feed listeners warning of earthquakes.

### Geofenced Keyword Monitoring

- **OSINT Scanner Engines**: Real-time keywords scanners checking social posts and threat boards.
- **Site Name Filters**: Keyword filters specific to client facilities and sites.
- **BOLO Feeds Ingest**: Automated ingestion of law enforcement BOLO alerts.

### Threat Assessment Radar

- **Threat Map Panels**: GIS radar showing active threat alerts near properties.
- **Radius Buffer Controls**: Adjusting geofence warnings (e.g., 5-mile vs 15-mile warnings).
- **Threat Scoring Widgets**: Gauges estimating incident threat levels (Critical, Elevating, Low).

### Proactive Alerts & Alarms

- **Dispatcher Warning Consoles**: Alarms alerting dispatchers when threats enter warning radii.
- **Playbook Autoloader**: Auto-suggesting response playbooks based on threat categories.
- **SMS Alarm Alerts**: Immediate SMS notices to security managers when threats trigger.

### Social Media Listening

- **Hashtag Monitoring logs**: Real-time scanners tracking local emergency tags.
- **Keyword Alert Alerts**: Push alerts if threat keywords spike (e.g., "fire", "shooting").
- **Image Recognition Scanners**: scanning online images for matching site logos or landmarks.

### BOLO Alert Ingest

- **Police Circular Ingestion**: Parsing state police databases for wanted persons lists.
- **Facial Verification Sync**: Syncing BOLO images to lobby check-in camera systems.
- **BOLO Archive Directories**: Searchable indices of BOLO details.

### Threat Intelligence Archiving

- **Alert History Registers**: Database storing past threat alerts and details.
- **Security Audit Logs**: Cross-referencing threat alerts against site adjustments.
- **Threat Vulnerability Reports**: Analysis templates mapping historical threat frequencies.

---

## 19. Physical Security Integration Gateway (IoT / VMS / Alarms)

Hardware connector layer linking physical devices directly to operational logging and dispatching.

### Intrusion Alarm IP Listeners

- **Sensor Port Listeners**: Processing telemetry inputs from motion, door, and glass-break boards.
- **Alarm Status Maps**: GIS maps displaying active sensor alerts by zone.
- **Alarm Reset Controls**: Remote reset controls to clear alarms from integration consoles.

### VMS Camera Stream Ingestion

- **RTSP/WebRTC Player Panels**: Player widgets displaying camera feeds in EOC/CAD maps.
- **Video Event Linkers**: Deep links to VMS servers to review playback recordings.
- **Camera Health Watchdogs**: Pinging cameras and logging connectivity dropouts.

### Access Control Panel Ingestion

- **Badge Swipe Receivers**: Real-time parsing of badge read data.
- **Door Status Watchdogs**: Alarms triggering for "Door Held Open" or "Door Forced Open".
- **Hardware Sync Workflows**: Reconciling panel configurations with platform databases.

### Fire Panel Watchdogs

- **Fire Alarm Registers**: Integration logs monitoring smoke detectors, heat sensors, and fire pumps.
- **Evacuation System Links**: Automatic triggers activating muster applications.
- **Panel Ingestion logs**: Detailed diagnostics logs of fire panel events.

### IoT Sensor Gateway

- **Environmental Sensor Logs**: Tracking temperature, humidity, water leak, and gas telemetry.
- **Sensor Threshold Alarms**: Automated dispatches if temperatures or gas counts exceed limits.
- **Battery Status Dashboards**: Tracking battery charge levels on wireless IoT sensors.

### Automated Dispatch Generation

- **CAD Integration Triggers**: System auto-creating dispatches based on panel alarms.
- **Playbook Links**: Direct links to safety manuals on active CAD dispatches.
- **Dispatches Auditing logs**: Logs documenting automated dispatches.

### Device Status Heartbeats

- **Device Ping Registers**: Status boards tracking integration panel connectivity.
- **Offline Alert Engines**: System notifications when panels disconnect.
- **Maintenance Ticketing Links**: Auto-creating work orders for failed integration devices.

### Custom Integration Profiles

- **Protocol Mapping Tools**: Protocol mapping tool (BACnet, Modbus, MQTT, Wiegand).
- **API Endpoint Integrations**: Interface managing REST and Webhook endpoints.
- **Hardware Schema Editors**: Custom fields defining hardware parameters.

---

## 20. K9 & Specialized Unit Operations

Operational tracking and qualifications for dog handlers and special response forces.

### Canine Profiles

- **K9 Roster Panels**: Profiles tracking canine names, chips, vaccinations, and ages.
- **K9 Health Records**: Vet logs tracking medication histories, weight checks, and diet plans.
- **Handler Association Trees**: Mapping K9 partners to dedicated guard handlers.

### Scent Profile Registry

- **Scent Target Catalogs**: Inventory logs managing training target materials (explosives, narcotics).
- **Target Custody Logs**: Tracking target access logs.
- **Detection Success Metrics**: Analytics scoring canine scent detection accuracy.

### Training Logs

- **Exercise Logs**: Documenting K9 training hours, environment types, and scores.
- **Decoy Handler Directories**: Registries of decoy personnel.
- **Weekly Progress Charts**: Dashboards showing training progression trends.

### Deployment & Search Logs

- **Search Incident Reports**: Standardized logs for parcel sweeps, search warrants, and patrols.
- **Search Map GPS Logs**: Map tracks detailing searched search paths.
- **Evidence Seizure Logs**: Linking K9 search finds to investigation case files.

### Weapons Certifications Gates

- **Tactical Qualification Checkers**: System checking special unit firearms certifications.
- **Range Re-qualification Alarms**: Email reminders for SWAT re-qualifications.
- **Scheduling blocks**: Restricting tactical assignments to qualified officers.

### Special Equipment Registers

- **Specialized Asset Directories**: Registries tracking tactical vests, night-vision, and gear.
- **Custody Checkout Station**: Rapid scan-out portal for tactical equipment.
- **Maintenance Records**: Maintenance logs managing checks on body armor and gear.

### Bite & Force Reports

- **Bite Incident Logs**: Detailed forms documenting canine bites, medical treatments, and photos.
- **Use-of-Force Reviews**: Evaluation workflows for tactical deployments.
- **Legal Review Packages**: Exports compiling bite details for corporate risk reviews.

### Tactical Operations logs

- **Mission Planning Checklists**: ICS-style checklists for high-risk warrants or barricade events.
- **Operational Timelines**: Time-stamped history of tactical commands and team updates.
- **After-Action Tactical Reports**: Evaluation reports detailing tactical deployments.

---

## 21. Compliance, Self-Assessments & Audits

Continuous monitoring of security standards, policy distribution, and organizational compliance.

### Compliance Standards Matrices

- **Control Databases**: Mapping system activity to FISMA, NIST, SOC 2, and DOE Orders.
- **Mapping Verification Tools**: Checklists confirming compliance alignments.
- **Gap Analysis Reports**: Dashboard identifying missing safety controls.

### Self-Assessment Checklists

- **Survey Builders**: Checklists managing internal safety self-assessments.
- **Findings Logs**: Documenting compliance issues, mitigation steps, and deadlines.
- **Review Portals**: Interface for safety directors to sign off on self-assessments.

### Auditor Evidence Vault

- **Evidence Repositories**: Folders storing completed drill records, license renewals, and logs.
- **Bulk PDF Compilers**: One-click compilations preparing evidence books.
- **Secure Share Links**: Temporary access folders for external compliance auditors.

### Audit Trail Immutability

- **Log Verification Engines**: Integrity verification tools checking if audit logs have been edited.
- **Cryptographic Hash Logs**: Timestamps hashing database transactions for proof.
- **Database Lock Warnings**: Alerts if attempts are made to alter locked registers.

### Policy Access Auditing

- **Policy Read Monitors**: Dashboard tracking which users opened and signed SOP updates.
- **Outstanding Signatures Lists**: Email reminder rosters for staff with missing sign-offs.
- **Acknowledgment Archives**: Complete registry of employee signatures.

### Compliance Dashboard

- **Compliance Scorecards**: Visual indicators displaying regulatory readiness scores.
- **Incident Severity Calculators**: Metric scoring compliance risks for unresolved issues.
- **Audit Target Calendars**: Calendar view managing upcoming compliance reviews.

### Audit Report Packages

- **Report Template Editors**: Custom templates for compiling safety audits.
- **Export Formats**: File outputs preparing audit materials for state reviews.
- **Digital Signatures**: Digital signatures locking compiled audit books.

### Vulnerability Registers

- **Vulnerability Registries**: Registers tracking security weaknesses and gaps.
- **Mitigation Action Items**: Task boards managing repair assignments.
- **Remediation Timelines**: Analytics displaying average days to close safety gaps.

---

## 22. Business Continuity & Disaster Recovery (BC/DR)

Disaster preparedness and organizational resilience management.

### Business Impact Analysis (BIA)

- **Asset BIA Matrices**: Tool scoring critical assets, data systems, and staff roles on impact.
- **RTO/RPO Registries**: Setting target Recovery Time Objectives and Recovery Point Objectives.
- **BIA Report Compilers**: Generating planning files for executive planning reviews.

### Disaster Checklists

- **Emergency Action Playbooks**: Step-by-step checklists for outages, cyber events, and disasters.
- **Mobile Task Monitors**: Assigning continuity tasks to emergency teams.
- **Step Verification Logs**: Immutable logs recording completed emergency steps.

### Alternative Supplier Registry

- **Supplier Directories**: Catalog of emergency vendors (backup fuel, generators, logistics).
- **MOU/Contract Archives**: Document library storing backup service agreements.
- **Vendor Health Checkups**: Checklist records verifying emergency suppliers stay ready.

### Vital Records Protection

- **Records Inventories**: Mapping physical and digital vaults containing key legal files.
- **Security Checklists**: Audits verifying backup data stays protected.
- **Recovery Instructions**: Guides detailing how to restore vital corporate files.

### Supply Chain Risk Logs

- **Risk Assessment Registers**: Registries tracking shipping lanes, ports, and delivery vendors.
- **Alternate Transport Plans**: Pre-built plans managing shipping redirects.
- **Delay Notification Alerts**: Messaging templates warning clients of supply bottlenecks.

### RTO/RPO Metrics Dashboards

- **RTO Progress Bars**: Dashboards tracking actual recovery speeds against targets.
- **Data Gap Indicators**: Indicators flagging lost transaction hours.
- **System Outage History**: Database tracking historical business downtime.

### Business Recovery Workflows

- **Recovery Pipelines**: Department pipelines guiding return-to-office tasks.
- **Facility Clearance Logs**: Safety checklists verifying buildings are safe before returns.
- **Executive Recovery Summaries**: Reports documenting continuity completion.

### BC/DR Tabletop Simulators

- **Scenario Guides**: Tabletop guides managing simulated outages or cyber events.
- **Inject Timelines**: Timeline log database triggering scenario events for drills.
- **Participant Feedback Polls**: Feedback forms evaluating employee drill responses.

---

## 23. Executive Protection & Secure Transit

Securing close protection operations and transport schedules.

### VIP Travel Profiles

- **Secure Health Charts**: Profiles holding medical cards, blood types, and allergies.
- **Threat Profiles**: Records tracking specific threat histories and stalkers.
- **Secure Contacts Directories**: Directory storing family, doctor, and advisor numbers.

### Route Assessment Tool

- **GIS Route Assessors**: Route mapping tool evaluating chokepoints and construction.
- **Safe Harbors Mapping**: GIS indicators displaying hospitals, police stations, and safehouses.
- **Threat Radius Scanners**: Scanning routes against threat intelligence databases.

### Advance Team Reports

- **Advance Checklists**: Safety checklists for hotels, airfields, and venues.
- **Emergency Contact Registries**: Phone lists for local police, hospitals, and managers.
- **Venue Floorplan Archives**: Storing layout designs and evacuation exits.

### Secure In-Transit Tracking

- **VIP Vehicle Trackers**: Live GPS tracking displaying transit vehicle positions.
- **Transit Geofencing Alerts**: Automated alarms if vehicle leaves planned routes.
- **Driver Telemetry Logs**: Telemetry monitoring speed, hard braking, and door statuses.

### Armored Car Registry

- **Fleet Specs Registries**: Inventories of armor levels, run-flat tires, and oxygen.
- **Ballistics Certificates Vault**: Digital files storing vehicle armor verification records.
- **Inspections Checklists**: Pre-trip vehicle inspection checklist logs.

### Arrival & Departure Logs

- **Secure Trip Timelines**: Real-time logs recording trip event times.
- **Secure Confirmation Messages**: Secure SMS templates verifying safe arrivals.
- **Flight Trackers Ingestion**: Live trackers updating flight numbers and gate changes.

### Executive Panic Button Integration

- **Mobile Panic Ingestion**: Priority listeners routing executive panic alarms to EOC.
- **Panic GIS Maps**: Map displaying live GPS coordinates of triggered panics.
- **Silent Audio Listeners**: Real-time voice stream ingestion when panic buttons click.

---

## 24. Special Event & Incident Action Planning (IAP)

Pre-event scheduling, mapping, and multi-agency coordination.

### IAP Forms Builder

- **ICS Form Compilers**: Automated tool compiling ICS 201, 202, and 208 plans.
- **IAP Approval Workflows**: Routing incident plans from creators to event directors.
- **Printable Briefing Books**: Generating comprehensive safety briefs for event staff.

### Perimeter GIS Designer

- **Temporary Map Layers**: Drawing tool mapping temporary fences, gates, and cones.
- **Command Post GIS Markers**: Mapping command posts, medical tents, and parking.
- **Evacuation Arrow Markers**: Designing event evacuation paths.

### Event Staff Schedule Roster

- **Staff Rosters Builder**: Schedule builder mapping guards to temporary event posts.
- **Shift Notification Alerts**: Broadcast alerts asking staff to claim event shifts.
- **Roster Compliance Indicators**: Checking guard credentials against event requirements.

### Crowd Management Checklists

- **Capacity Calculators**: Tool calculating gate limits and crowd density metrics.
- **Inspection Checklist Logs**: checklists managing checks on exit gates and emergency lanes.
- **Gate Count logs**: Interface tracking ingress/egress counts at gates.

### Temporary Access Permits

- **QR Code Permit Creators**: Tool printing temporary badges for media, vendors, and VIPs.
- **Permit Limit Rules**: Group boundaries defining temporary badge expiration times.
- **Entry Denied Alerts**: Dispatch alerts if guest scans expired badges.

### Local Agency Integration Log

- **Liaison Directories**: Directory of municipal police, EMS, and transit managers.
- **Channel Frequency Guides**: Document registry listing radio channel maps.
- **Liaison Communication Logs**: Logs documenting coordination meetings and decisions.

### Post-Event De-registration Checklist

- **Demobilization Checklists**: Checklists managing asset collections and fence teardowns.
- **Access Permit Wipes**: System-wide sweeps deleting temporary access credentials.
- **Event Debriefing Templates**: Report forms documenting lessons learned and safety summaries.

---

## 25. Supply Chain & Cargo Security

Securing freight yards, logistics transits, and warehouses.

### C-TPAT Compliance Logs

- **C-TPAT Auditing Checklists**: checklists verifying customs compliance.
- **Self-Assessment Registers**: Logging supply chain security surveys.
- **Evidence File Archives**: Document archives keeping compliance proof.

### Container Seal Registries

- **Bolt Seal Logbooks**: Record tables logging high-security seal numbers and types.
- **Seal Photos Vault**: Digital archive of photos showing intact seals.
- **Discrepancy Alarm Alarms**: Immediate dispatch alarms if seals are broken or don't match manifests.

### High-Value Cargo Tracking

- **Cargo GPS Trackers**: Live trackers displaying cargo vehicle positions.
- **Door Contact Sensors**: Real-time alarms if shipping doors open outside depots.
- **Cargo Environmental Logs**: Telemetry monitoring transit temperature and humidity.

### Warehouse Security Inspections

- **Facility Inspections Checklists**: checklists managing checks on fence lines, gates, and locks.
- **Lighting Surveys**: Focused logs documenting failed yard security lights.
- **CCTV Audit logs**: Audit logs tracking camera directions and coverage checks.

### Delivery Driver Validations

- **Driver Verification Forms**: Form registers checking driver photos, licenses, and manifests.
- **Direct Link to CAD**: Dispatch alerts if driver credential checks fail.
- **Gate Pass Generator**: QR passes granting gate access to validated drivers.

### Loss Prevention Audits

- **Shrinkage Registries**: Database tracking inventory losses, dates, and locations.
- **Investigation Case Links**: Direct integration linking thefts to investigation files.
- **Audit Scores Dashboard**: Analytics charting cargo theft trends.

### Staging Area Logs

- **Yard Management Boards**: Visual boards displaying staging spot occupancy.
- **Dwell Time Watchdogs**: Timers alerting operators if vehicles sit in staging too long.
- **Access Logs Integration**: Syncing gate reader logs to manifest schedules.

### Cargo Incident Reports

- **Hijack Incident Forms**: Specialized forms tracking cargo thefts and location details.
- **Damage Claim Logs**: Log registers tracking freight damages and photo logs.
- **Carrier Discrepancy logs**: Documenting carrier performance issues and contract reviews.
