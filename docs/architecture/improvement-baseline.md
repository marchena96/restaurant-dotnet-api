# Architecture Improvement Baseline v0.1

**Scope:** AS-IS architectural baseline for Investigation II.

**Status vocabulary:** `Confirmed Gap`, `Target Requirement`, `Verified`, `Deferred`.

**Scope guardrails:** This document records baseline requirements and verification criteria only. It does not define implementation tasks, select distributed technologies, change code, change the database, or authorize distributed implementation.

## Architectural Position

- Layered Architecture remains the backend foundation.
- Lightweight, selective CQRS may be used inside Application when a future proposal justifies it.
- CQRS does not imply Event Sourcing, multiple databases, a message broker, or microservices.
- Internal service architecture and distributed architecture between services are separate concerns.
- RabbitMQ, WebSockets, WebHooks, Event Sourcing, API Gateway, and Quartz are not selected by this baseline.

## Summary

| ID | Area | Severity | Status | Gate |
|---|---|---|---|---|
| COR-001 | Correctness | Critical | Confirmed Gap | Distributed Readiness Gate |
| COR-002 | Correctness | High | Confirmed Gap | Distributed Readiness Gate |
| COR-003 | Correctness | High | Confirmed Gap | Investigation II Acceptance Gate |
| COR-004 | Correctness | High | Confirmed Gap | Investigation II Acceptance Gate |
| COR-005 | Correctness | High | Confirmed Gap | Distributed Readiness Gate |
| COR-006 | Correctness | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| COR-007 | Correctness | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| COR-008 | Correctness | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| COR-009 | Correctness | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| COR-010 | Correctness | Low | Confirmed Gap | Investigation II Acceptance Gate |
| SEC-001 | Security | Critical | Confirmed Gap | Distributed Readiness Gate |
| SEC-002 | Security | Critical | Confirmed Gap | Distributed Readiness Gate |
| SEC-003 | Security | Critical | Confirmed Gap | Distributed Readiness Gate |
| SEC-004 | Security | High | Confirmed Gap | Distributed Readiness Gate |
| SEC-005 | Security | High | Confirmed Gap | Distributed Readiness Gate |
| SEC-006 | Security | High | Confirmed Gap | Investigation II Acceptance Gate |
| SEC-007 | Security | High | Confirmed Gap | Distributed Readiness Gate |
| SEC-008 | Security | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| SEC-009 | Security | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| SEC-010 | Security | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-001 | Architecture | Critical | Confirmed Gap | Distributed Readiness Gate |
| ARC-002 | Architecture | Critical | Confirmed Gap | Distributed Readiness Gate |
| ARC-003 | Architecture | High | Confirmed Gap | Distributed Readiness Gate |
| ARC-004 | Architecture | High | Target Requirement | Distributed Readiness Gate |
| ARC-005 | Architecture | High | Target Requirement | Distributed Readiness Gate |
| ARC-006 | Architecture | High | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-007 | Architecture | High | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-008 | Architecture | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-009 | Architecture | Medium | Target Requirement | Investigation II Acceptance Gate |
| ARC-010 | Architecture | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-011 | Architecture | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-012 | Architecture | Medium | Target Requirement | Investigation II Acceptance Gate |
| ARC-013 | Architecture | Low | Confirmed Gap | Investigation II Acceptance Gate |
| ARC-014 | Architecture | Low | Deferred | Investigation II Acceptance Gate |
| INF-001 | Infrastructure | Critical | Confirmed Gap | Distributed Readiness Gate |
| INF-002 | Infrastructure | High | Confirmed Gap | Distributed Readiness Gate |
| INF-003 | Infrastructure | High | Confirmed Gap | Distributed Readiness Gate |
| INF-004 | Infrastructure | High | Confirmed Gap | Investigation II Acceptance Gate |
| INF-005 | Infrastructure | High | Target Requirement | Investigation II Acceptance Gate |
| INF-006 | Infrastructure | High | Target Requirement | Investigation II Acceptance Gate |
| INF-007 | Infrastructure | High | Target Requirement | Investigation II Acceptance Gate |
| INF-008 | Infrastructure | High | Target Requirement | Investigation II Acceptance Gate |
| INF-009 | Infrastructure | High | Target Requirement | Investigation II Acceptance Gate |
| INF-010 | Infrastructure | Medium | Target Requirement | Investigation II Acceptance Gate |
| INF-011 | Infrastructure | Medium | Target Requirement | Investigation II Acceptance Gate |
| INF-012 | Infrastructure | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| INF-013 | Infrastructure | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| INF-014 | Infrastructure | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| INF-015 | Infrastructure | Low | Deferred | Investigation II Acceptance Gate |
| QUA-001 | Quality | Critical | Confirmed Gap | Distributed Readiness Gate |
| QUA-002 | Quality | High | Confirmed Gap | Distributed Readiness Gate |
| QUA-003 | Quality | High | Confirmed Gap | Investigation II Acceptance Gate |
| QUA-004 | Quality | High | Target Requirement | Investigation II Acceptance Gate |
| QUA-005 | Quality | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| QUA-006 | Quality | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| QUA-007 | Quality | Medium | Confirmed Gap | Investigation II Acceptance Gate |
| QUA-008 | Quality | Low | Confirmed Gap | Investigation II Acceptance Gate |

## 1. CORRECTNESS

### COR-001 - Reservation concurrency integrity

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** Reservation availability is checked with separate queries and no visible concurrency protection. Concurrent requests may reserve the same table and interval.
- **Target Requirement:** Reservation acceptance must preserve the no-overlapping-active-reservations invariant under concurrent requests.
- **Verification:** Execute concurrent reservation attempts for one table and interval; at most one succeeds and database state remains valid.
- **Related Files:** `Services/Implementations/ReservationService.cs`, `Services/Implementations/TableService.cs`, `Data/MyAppDbContext.cs`
- **Gate:** Distributed Readiness Gate

### COR-002 - Transaction boundaries for compound operations

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Reservation and waiting-list flows perform multiple reads/writes with no explicit transaction boundary.
- **Target Requirement:** Each compound business operation must define atomicity and rollback behavior.
- **Verification:** Force failure between related writes and verify no partial reservation, waiting-list, or status state remains.
- **Related Files:** `Services/Implementations/ReservationService.cs`, `Services/Implementations/WaitingListService.cs`
- **Gate:** Distributed Readiness Gate

### COR-003 - Consistent reservation validation

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Create and update paths do not apply identical validation rules; update omits several create-time checks.
- **Target Requirement:** Create and update operations must enforce the same domain invariants unless an explicit exception is documented.
- **Verification:** Test invalid capacity, inactive zone, invalid turn, overlap, lock, and missing references through both endpoints.
- **Related Files:** `Services/Implementations/ReservationService.cs`, `Controllers/ReservationsController.cs`
- **Gate:** Investigation II Acceptance Gate

### COR-004 - Valid status handling

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Reservation logic uses hardcoded status IDs and English names while DTOs expose translated names.
- **Target Requirement:** Status transitions must use stable domain semantics and reject invalid or incompatible states.
- **Verification:** Change status ordering or add status records in a controlled test database; reservation behavior remains correct.
- **Related Files:** `Data/MyAppDbContext.cs`, `Services/Implementations/ReservationService.cs`, `Services/Implementations/WaitingListService.cs`
- **Gate:** Investigation II Acceptance Gate

### COR-005 - Safe reservation promotion

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Waiting-list promotion creates a reservation and removes the waiting entry through one shared context without an explicit transaction or idempotency rule.
- **Target Requirement:** Promotion must be atomic and repeat-safe.
- **Verification:** Repeat and concurrently invoke promotion; exactly one valid reservation is produced and source state is deterministic.
- **Related Files:** `Services/Implementations/WaitingListService.cs`, `Controllers/WaitingListController.cs`
- **Gate:** Distributed Readiness Gate

### COR-006 - Lock validation consistency

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Create lock validates existence, time order, and overlap; update does not apply equivalent validation.
- **Target Requirement:** Lock lifecycle operations must enforce table existence, valid intervals, and non-overlap consistently.
- **Verification:** Exercise invalid update cases and verify rejection without state mutation.
- **Related Files:** `Services/Implementations/LockService.cs`, `DTOs/TableLockDto.cs`
- **Gate:** Investigation II Acceptance Gate

### COR-007 - Cancellation-aware availability

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Some availability queries do not visibly exclude cancelled reservations.
- **Target Requirement:** Availability calculations must use an explicit, consistent definition of active reservation states.
- **Verification:** Create cancelled and active reservations over the same interval; only active reservations block availability.
- **Related Files:** `Services/Implementations/TableService.cs`, `Services/Implementations/ReservationService.cs`
- **Gate:** Investigation II Acceptance Gate

### COR-008 - Time and timezone consistency

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Code mixes `DateTime.UtcNow`, `DateTime.Today`, and local runtime assumptions.
- **Target Requirement:** Business date and time calculations must use one documented timezone policy.
- **Verification:** Run around UTC/local midnight and compare table status, reservation date, and dashboard output.
- **Related Files:** `Services/Implementations/TableService.cs`, `Controllers/DashboardController.cs`, `Models/Reservation.cs`
- **Gate:** Investigation II Acceptance Gate

### COR-009 - Referential and input validation

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** DTOs rely heavily on strings and service parsing; validation is not uniformly enforced across endpoints.
- **Target Requirement:** Invalid dates, times, ranges, capacities, identifiers, and required fields must receive deterministic validation responses.
- **Verification:** Submit malformed and boundary payloads to every write endpoint and verify no invalid persistence occurs.
- **Related Files:** `DTOs/`, `Controllers/`, `Services/Implementations/`
- **Gate:** Investigation II Acceptance Gate

### COR-010 - Documentation and request example consistency

- **Severity:** Low
- **Status:** Confirmed Gap
- **Confirmed Gap:** README and `.http` examples differ from code, including credentials, status values, and fields.
- **Target Requirement:** Documented API contracts and executable examples must match implemented behavior.
- **Verification:** Replay documented examples against the API and compare routes, payloads, status codes, and responses.
- **Related Files:** `README.md`, `RestauranteAPI.http`, `Controllers/`, `DTOs/`
- **Gate:** Investigation II Acceptance Gate

## 2. SECURITY

### SEC-001 - Remove versioned JWT secrets

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** `appsettings.json` contains a JWT secret value tracked with application configuration.
- **Target Requirement:** Secrets must be supplied through an approved secret-management or runtime configuration mechanism and excluded from version control.
- **Verification:** Repository scan finds no live secret; application starts only with externally supplied secret configuration.
- **Related Files:** `appsettings.json`, `Program.cs`, `Services/Implementations/AuthService.cs`
- **Gate:** Distributed Readiness Gate

### SEC-002 - Remove hardcoded JWT fallback

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** `Program.cs` and `AuthService` contain fallback JWT secrets.
- **Target Requirement:** Missing signing configuration must fail startup or use an approved injected secret; no hardcoded fallback may exist.
- **Verification:** Start without JWT secret and verify explicit configuration failure; source scan finds no fallback secret.
- **Related Files:** `Program.cs`, `Services/Implementations/AuthService.cs`
- **Gate:** Distributed Readiness Gate

### SEC-003 - Prevent self-assigned privileged roles

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** Public registration accepts a caller-provided `Role`.
- **Target Requirement:** Public registration cannot assign privileged roles; role provisioning requires trusted authorization.
- **Verification:** Register with `Admin` and other elevated values; request is rejected or role is constrained to an approved non-privileged role.
- **Related Files:** `Controllers/AuthController.cs`, `DTOs/AuthDto.cs`, `Services/Implementations/AuthService.cs`
- **Gate:** Distributed Readiness Gate

### SEC-004 - Token revocation and logout semantics

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Logout returns success but does not revoke or invalidate JWTs.
- **Target Requirement:** Logout behavior and token lifetime/revocation expectations must be explicit and enforced.
- **Verification:** Invoke logout and test whether the documented post-logout access behavior occurs.
- **Related Files:** `Controllers/AuthController.cs`, `Program.cs`, `Services/Implementations/AuthService.cs`
- **Gate:** Distributed Readiness Gate

### SEC-005 - Configurable token lifetime

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** `JwtSettings:ExpiryInDays` exists but token generation uses a hardcoded seven-day lifetime.
- **Target Requirement:** Token lifetime must be validated configuration, not a conflicting hardcoded value.
- **Verification:** Set a test lifetime and inspect issued token expiration; invalid or missing values fail predictably.
- **Related Files:** `appsettings.json`, `Services/Implementations/AuthService.cs`
- **Gate:** Investigation II Acceptance Gate

### SEC-006 - Secure transport and deployment configuration

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Local HTTP launch profile is the only visible runtime profile; production transport policy is not defined.
- **Target Requirement:** Deployment must define HTTPS, proxy, certificate, and secure-cookie/credential policies where applicable.
- **Verification:** Production-like startup and security scan confirm encrypted transport and documented termination behavior.
- **Related Files:** `Properties/launchSettings.json`, `Program.cs`, `README.md`
- **Gate:** Investigation II Acceptance Gate

### SEC-007 - Controlled database credentials

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** SQL Server connection string is versioned in `appsettings.json`.
- **Target Requirement:** Database credentials and environment-specific connection details must be externally managed.
- **Verification:** Repository scan finds no deployable credential; runtime configuration supplies a valid connection in a controlled environment.
- **Related Files:** `appsettings.json`, `Program.cs`, `README.md`
- **Gate:** Distributed Readiness Gate

### SEC-008 - Consistent authorization policy

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Authorization is mainly controller/role attributes; resource-level ownership or data scope is not defined.
- **Target Requirement:** Each protected capability must have an explicit policy and data-access scope.
- **Verification:** Matrix-test anonymous, authenticated, non-admin, admin, and cross-resource access.
- **Related Files:** `Controllers/`, `Program.cs`, `Services/Implementations/`
- **Gate:** Investigation II Acceptance Gate

### SEC-009 - Safe error disclosure

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Several controllers return raw exception messages and catch broad exceptions as 400 responses.
- **Target Requirement:** Client errors expose safe, stable messages; internal details remain server-side.
- **Verification:** Trigger validation, database, and unexpected failures; responses contain no stack traces, SQL, or secrets.
- **Related Files:** `Controllers/`, `Middleware/ExceptionMiddleware.cs`
- **Gate:** Investigation II Acceptance Gate

### SEC-010 - Security auditability

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** No visible audit trail exists for login, role changes, reservations, locks, or privileged mutations.
- **Target Requirement:** Security-relevant actions must have attributable, timestamped audit evidence where required by the target scope.
- **Verification:** Perform privileged actions and verify actor, action, resource, result, and timestamp evidence.
- **Related Files:** `Controllers/AuthController.cs`, `Services/Implementations/`, `Models/User.cs`
- **Gate:** Investigation II Acceptance Gate

## 3. ARCHITECTURE

### ARC-001 - Explicit transaction ownership

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** Transaction responsibility is implicit in the shared EF context and `SaveChangesAsync` calls.
- **Target Requirement:** Each business operation must identify its transaction boundary, isolation expectation, and failure behavior.
- **Verification:** Architecture review plus failure-injection tests demonstrate atomicity for compound flows.
- **Related Files:** `Data/MyAppDbContext.cs`, `Services/Implementations/ReservationService.cs`, `Services/Implementations/WaitingListService.cs`
- **Gate:** Distributed Readiness Gate

### ARC-002 - Data ownership boundaries

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** All functional areas share one `DbContext` and database without declared ownership boundaries.
- **Target Requirement:** Every future service or module must have explicit data ownership and allowed access paths.
- **Verification:** Approved context map identifies owner, readers, writers, and integration contract for every aggregate/table.
- **Related Files:** `Data/MyAppDbContext.cs`, `Models/`, `Services/Implementations/`
- **Gate:** Distributed Readiness Gate

### ARC-003 - Distributed boundary decision

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Current folders are not independent services or bounded contexts.
- **Target Requirement:** Any distributed decomposition must be based on documented functional boundaries, not folder names.
- **Verification:** Architecture decision records define Service A, Service B, responsibilities, ownership, and non-goals before implementation.
- **Related Files:** `RestauranteAPI.slnx`, `RestauranteAPI.csproj`, `Controllers/`, `Services/`
- **Gate:** Distributed Readiness Gate

### ARC-004 - Layer dependency direction

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** Service implementations depend directly on EF Core and `MyAppDbContext`.
- **Target Requirement:** Layer direction must be explicit; Application logic must not depend on transport concerns, and infrastructure dependencies must be isolated at defined boundaries.
- **Verification:** Dependency review or architecture test confirms permitted references and rejects forbidden references.
- **Related Files:** `Services/`, `Data/`, `Controllers/`, `RestauranteAPI.csproj`
- **Gate:** Distributed Readiness Gate

### ARC-005 - Selective CQRS boundary

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** Current services mix command, query, mapping, and persistence responsibilities.
- **Target Requirement:** Lightweight CQRS may separate read and write use cases selectively inside Application, without implying Event Sourcing, multiple databases, brokers, or microservices.
- **Verification:** Future design review identifies justified command/query boundaries and confirms no accidental distributed pattern selection.
- **Related Files:** `Services/Interfaces/`, `Services/Implementations/`, `Controllers/`
- **Gate:** Distributed Readiness Gate

### ARC-006 - Domain rule ownership

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Reservation, availability, and promotion rules are spread across services and controllers.
- **Target Requirement:** Each business invariant must have one identifiable owner and one authoritative implementation path.
- **Verification:** Rule-to-owner matrix covers reservation overlap, locks, capacity, status, turns, and promotion.
- **Related Files:** `ReservationService.cs`, `TableService.cs`, `WaitingListService.cs`, `DashboardController.cs`
- **Gate:** Investigation II Acceptance Gate

### ARC-007 - Application versus transport contracts

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** Service interfaces return and accept HTTP-oriented DTOs.
- **Target Requirement:** Application use cases must have stable application contracts, with HTTP mapping isolated at the API boundary.
- **Verification:** Contract review confirms transport DTO changes do not require domain/application changes without an explicit compatibility decision.
- **Related Files:** `DTOs/`, `Services/Interfaces/`, `Controllers/`
- **Gate:** Investigation II Acceptance Gate

### ARC-008 - Cross-module orchestration

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** `DashboardController` and `TablesController` orchestrate multiple services directly.
- **Target Requirement:** Cross-module read composition must have an explicit application-level owner and consistency expectation.
- **Verification:** Trace each composite endpoint to one documented use case and defined read consistency.
- **Related Files:** `Controllers/DashboardController.cs`, `Controllers/TablesController.cs`
- **Gate:** Investigation II Acceptance Gate

### ARC-009 - Stable internal contracts

- **Severity:** Medium
- **Status:** Target Requirement
- **Confirmed Gap:** Statuses, roles, and time values rely heavily on strings and hardcoded IDs.
- **Target Requirement:** Internal contracts must use stable identifiers or validated enums/value semantics with explicit external mappings.
- **Verification:** Contract tests prove behavior is stable when display text or database ordering changes.
- **Related Files:** `Models/`, `DTOs/`, `Services/Implementations/`, `Data/MyAppDbContext.cs`
- **Gate:** Investigation II Acceptance Gate

### ARC-010 - Persistence isolation

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Every implementation directly queries and mutates the shared EF context.
- **Target Requirement:** Persistence access must be isolated behind the approved application/infrastructure boundary.
- **Verification:** Architecture review identifies all persistence entry points and verifies no unauthorized direct context access.
- **Related Files:** `Services/Implementations/`, `Data/MyAppDbContext.cs`
- **Gate:** Investigation II Acceptance Gate

### ARC-011 - API error contract

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Middleware and controllers emit different error shapes and status mappings.
- **Target Requirement:** API errors must have one documented, versionable contract.
- **Verification:** Contract tests cover validation, unauthorized, not found, conflict, and internal error cases.
- **Related Files:** `Middleware/ExceptionMiddleware.cs`, `Controllers/`, `README.md`
- **Gate:** Investigation II Acceptance Gate

### ARC-012 - Service communication decision record

- **Severity:** Medium
- **Status:** Target Requirement
- **Confirmed Gap:** No current inter-service communication exists and no technology is selected.
- **Target Requirement:** Before distributed implementation, communication needs, sync/async semantics, failure behavior, and technology selection must be documented separately from this baseline.
- **Verification:** Approved decision record exists before implementation and explicitly evaluates non-selection of unapproved patterns.
- **Related Files:** `RestauranteAPI.slnx`, `Program.cs`, `README.md`
- **Gate:** Investigation II Acceptance Gate

### ARC-013 - API versioning and compatibility

- **Severity:** Low
- **Status:** Confirmed Gap
- **Confirmed Gap:** Routes have no visible API versioning or compatibility policy.
- **Target Requirement:** Public and future inter-service contracts must define versioning and breaking-change rules.
- **Verification:** API inventory identifies version strategy and compatibility tests cover supported clients.
- **Related Files:** `Controllers/`, `README.md`, `RestauranteAPI.http`
- **Gate:** Investigation II Acceptance Gate

### ARC-014 - Broader domain decomposition

- **Severity:** Low
- **Status:** Deferred
- **Confirmed Gap:** The current audit does not establish whether identity, reservations, operations, waiting list, or dashboard should become independent bounded contexts.
- **Target Requirement:** Domain decomposition remains an explicit future decision after Investigation II evidence and requirements are complete.
- **Verification:** Future proposal records decision, alternatives, consequences, and rejected boundaries.
- **Related Files:** `Controllers/`, `Services/`, `Models/`
- **Gate:** Investigation II Acceptance Gate

## 4. INFRASTRUCTURE

### INF-001 - Automated test foundation

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** No test project or automated test suite is present.
- **Target Requirement:** A repeatable automated test foundation must exist before distributed readiness is declared.
- **Verification:** Clean checkout runs unit/integration/contract test commands and produces repeatable results.
- **Related Files:** `RestauranteAPI.csproj`, repository root
- **Gate:** Distributed Readiness Gate

### INF-002 - Base containerization

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** No Dockerfile or container definition is present.
- **Target Requirement:** Investigation II must have a reproducible container baseline for each investigated service.
- **Verification:** Required images build from a clean checkout and start using documented configuration.
- **Related Files:** repository root, `RestauranteAPI.csproj`
- **Gate:** Distributed Readiness Gate

### INF-003 - Reproducible local orchestration

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** No Docker Compose or equivalent local multi-process orchestration is present.
- **Target Requirement:** The accepted distributed scenario must start from a documented, reproducible local orchestration definition.
- **Verification:** One documented command starts required components from a clean environment.
- **Related Files:** repository root, `README.md`
- **Gate:** Distributed Readiness Gate

### INF-004 - Environment configuration contract

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** No `.env`, `.env.example`, or complete environment variable contract is present.
- **Target Requirement:** Runtime configuration must be externalized, documented, and reproducible without committing secrets.
- **Verification:** `.env.example` is sufficient to derive a working local configuration; secret scan remains clean.
- **Related Files:** `appsettings.json`, `appsettings.Development.json`, repository root
- **Gate:** Investigation II Acceptance Gate

### INF-005 - Service A container definition

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** No Service A exists in the current monolith baseline.
- **Target Requirement:** If Investigation II defines Service A, it must have a documented Dockerfile and deterministic startup contract.
- **Verification:** Service A image builds, starts, exposes its documented port, and passes its health/startup check.
- **Related Files:** Future service path, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-006 - Service B container definition

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** No Service B exists in the current monolith baseline.
- **Target Requirement:** If Investigation II defines Service B, it must have a documented Dockerfile and deterministic startup contract.
- **Verification:** Service B image builds, starts, exposes its documented port, and passes its health/startup check.
- **Related Files:** Future service path, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-007 - Compose topology

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** No distributed runtime topology is currently defined.
- **Target Requirement:** The accepted scenario must define both services, required infrastructure, networks, ports, dependencies, and configuration in Compose.
- **Verification:** `docker compose config` validates and the topology starts from a clean checkout.
- **Related Files:** Future `compose` file, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-008 - Run script

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** No `run.ps1` or `run.sh` is present.
- **Target Requirement:** The accepted scenario must provide at least one documented cross-platform-appropriate run script.
- **Verification:** Script starts the reproducible scenario and returns failure when prerequisites or startup fail.
- **Related Files:** repository root, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-009 - Reproducible distributed scenario

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** Current repository has only a single-process API scenario.
- **Target Requirement:** Investigation II must define one end-to-end scenario demonstrating the selected research topic without changing the production baseline implicitly.
- **Verification:** A fresh operator can execute documented steps and obtain the expected result.
- **Related Files:** `README.md`, future scenario files
- **Gate:** Investigation II Acceptance Gate

### INF-010 - Observable inter-service communication

- **Severity:** Medium
- **Status:** Target Requirement
- **Confirmed Gap:** No inter-service communication exists in the current repository.
- **Target Requirement:** The accepted scenario must expose observable evidence that Service A and Service B communicate through their documented contract.
- **Verification:** Logs, response correlation, test output, or equivalent evidence identifies both participants and the exchange.
- **Related Files:** future service paths, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-011 - Runtime health evidence

- **Severity:** Medium
- **Status:** Target Requirement
- **Confirmed Gap:** No health checks are visible.
- **Target Requirement:** Required runtime components must expose enough health/startup evidence for local orchestration and troubleshooting.
- **Verification:** Health or readiness checks distinguish startup failure from application failure.
- **Related Files:** `Program.cs`, future service paths, Compose definition
- **Gate:** Investigation II Acceptance Gate

### INF-012 - Startup migration policy

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** `Program.cs` invokes `Database.Migrate()` and `SeedData.Initialize()` invokes `EnsureCreated()`.
- **Target Requirement:** Schema initialization and seed policy must use one explicit strategy per environment.
- **Verification:** Fresh and existing database startup tests show deterministic schema and seed behavior without conflicting initialization paths.
- **Related Files:** `Program.cs`, `Data/SeedData.cs`, `Migrations/`
- **Gate:** Investigation II Acceptance Gate

### INF-013 - Deployment profile separation

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Only local launch configuration is visible; production deployment profile is not defined.
- **Target Requirement:** Runtime profiles must define environment-specific configuration, secrets, transport, database, logging, and startup behavior.
- **Verification:** Local and production-like profiles are independently documented and do not rely on local machine names.
- **Related Files:** `Properties/launchSettings.json`, `appsettings*.json`, `README.md`
- **Gate:** Investigation II Acceptance Gate

### INF-014 - Observability baseline

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** Logging exists only through default ASP.NET logging and exception middleware; no correlation, metrics, or tracing baseline is visible.
- **Target Requirement:** Investigation II evidence must make request flow and inter-service communication diagnosable.
- **Verification:** A scenario failure can be traced from request to both services using documented observable evidence.
- **Related Files:** `Program.cs`, `Middleware/ExceptionMiddleware.cs`, `appsettings.json`
- **Gate:** Investigation II Acceptance Gate

### INF-015 - Production deployment platform

- **Severity:** Low
- **Status:** Deferred
- **Confirmed Gap:** Target deployment platform, scaling model, and operational ownership are not specified by the audit.
- **Target Requirement:** Deployment platform decisions remain deferred until Investigation II scope and acceptance evidence are approved.
- **Verification:** Future proposal records platform assumptions and operational constraints before deployment work begins.
- **Related Files:** `README.md`, repository root
- **Gate:** Investigation II Acceptance Gate

## 5. QUALITY

### QUA-001 - Automated correctness coverage

- **Severity:** Critical
- **Status:** Confirmed Gap
- **Confirmed Gap:** No automated tests cover reservation, availability, locks, waiting list, authentication, or authorization.
- **Target Requirement:** Critical business and security invariants must have automated coverage before distributed readiness.
- **Verification:** Test report demonstrates coverage for success, failure, boundary, and concurrency cases.
- **Related Files:** `Services/Implementations/`, `Controllers/`, repository root
- **Gate:** Distributed Readiness Gate

### QUA-002 - Concurrency test coverage

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** No concurrency tests verify reservation conflict behavior.
- **Target Requirement:** Concurrent reservation and promotion scenarios must be tested against the chosen persistence behavior.
- **Verification:** Repeatable concurrent test proves deterministic conflict handling and no duplicate allocation.
- **Related Files:** `ReservationService.cs`, `WaitingListService.cs`, `TableService.cs`
- **Gate:** Distributed Readiness Gate

### QUA-003 - Integration test environment

- **Severity:** High
- **Status:** Confirmed Gap
- **Confirmed Gap:** No integration test harness or reproducible SQL Server test environment is present.
- **Target Requirement:** Integration tests must run against a representative persistence environment, not only mocks or unverified in-memory behavior.
- **Verification:** Clean checkout creates the test environment, applies schema, seeds controlled data, and runs API tests.
- **Related Files:** `RestauranteAPI.csproj`, `Migrations/`, repository root
- **Gate:** Investigation II Acceptance Gate

### QUA-004 - Contract test coverage

- **Severity:** High
- **Status:** Target Requirement
- **Confirmed Gap:** No contract tests verify current or future HTTP/inter-service payloads.
- **Target Requirement:** API and inter-service contracts must have executable compatibility tests.
- **Verification:** Contract suite detects route, payload, status-code, and compatibility regressions.
- **Related Files:** `Controllers/`, `DTOs/`, `RestauranteAPI.http`, future service paths
- **Gate:** Investigation II Acceptance Gate

### QUA-005 - Reproducible verification commands

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** README documents `dotnet run`, but no complete automated verification command set exists.
- **Target Requirement:** Build, test, run, and scenario verification commands must be documented and repeatable.
- **Verification:** A clean operator can execute documented commands without relying on undocumented local state.
- **Related Files:** `README.md`, `RestauranteAPI.csproj`, repository root
- **Gate:** Investigation II Acceptance Gate

### QUA-006 - Architecture verification

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** No architecture tests or dependency checks verify layer boundaries.
- **Target Requirement:** Declared layer and service dependency rules must be mechanically or procedurally verifiable.
- **Verification:** Architecture check fails when forbidden project, namespace, or dependency references are introduced.
- **Related Files:** `RestauranteAPI.csproj`, `Controllers/`, `Services/`, `Data/`
- **Gate:** Investigation II Acceptance Gate

### QUA-007 - Documentation completeness

- **Severity:** Medium
- **Status:** Confirmed Gap
- **Confirmed Gap:** README documents current API but does not provide a complete architecture, runtime, distributed scenario, or evidence guide.
- **Target Requirement:** Documentation must describe current boundaries, target scenario, setup, execution, verification, and known non-goals.
- **Verification:** Independent reviewer follows README from clean checkout and reproduces the accepted scenario.
- **Related Files:** `README.md`, `RestauranteAPI.http`, future architecture documentation
- **Gate:** Investigation II Acceptance Gate

### QUA-008 - Evidence and acceptance traceability

- **Severity:** Low
- **Status:** Confirmed Gap
- **Confirmed Gap:** No formal mapping exists between requirements, verification commands, observed evidence, and acceptance decisions.
- **Target Requirement:** Investigation II artifacts must preserve traceability from requirement to verification evidence and gate result.
- **Verification:** Acceptance record links every required item to a command, output, screenshot, log, or documented reason for deferral.
- **Related Files:** `docs/architecture/improvement-baseline.md`, `README.md`, future investigation artifacts
- **Gate:** Investigation II Acceptance Gate

## Formal Gates

### 1. Distributed Readiness Gate

Distributed implementation **MUST NOT begin** while any of these baseline conditions remains open:

- Reservation concurrency integrity: `COR-001`, `QUA-002`.
- Transaction boundaries: `COR-002`, `ARC-001`.
- Safe role assignment: `SEC-003`.
- Versioned JWT secrets: `SEC-001`.
- Hardcoded JWT fallback: `SEC-002`.
- Consistent initialization and migrations: `INF-012`.
- Automated test foundation: `INF-001`, `QUA-001`.
- Base Dockerization: `INF-002`.
- Data ownership and boundary decision: `ARC-002`, `ARC-003`.

Gate result is `PASS` only when all blocking requirements are `Verified` or an explicitly approved exception is recorded outside this baseline. This gate does not select a distributed technology.

### 2. Investigation II Acceptance Gate

Investigation II is accepted only when evidence verifies, at minimum:

- Selected research topic.
- Service A.
- Service B.
- Real communication between Service A and Service B.
- Selected technology applied in the investigated scenario.
- Dockerfile for both services.
- Docker Compose definition.
- `.env` and/or `.env.example` configuration contract.
- `run.ps1` and/or `run.sh` execution script.
- Reproducible end-to-end scenario.
- Observable communication evidence.
- README with setup and verification steps.
- Architecture and flow diagrams.
- Relevant correctness, security, architecture, infrastructure, and quality requirements verified or explicitly deferred.

Gate result is `PASS` only when every mandatory acceptance item has reproducible evidence. The gate does not authorize changing the current production architecture or database without a later approved proposal.

## Deferred Decisions

- `ARC-014`: final bounded-context decomposition.
- `INF-015`: production deployment platform and scaling model.
- Communication technology selection remains outside this baseline. No RabbitMQ, WebSockets, WebHooks, Event Sourcing, API Gateway, or Quartz decision is made here.

## Baseline Counts

| Category | Requirements |
|---|---:|
| Correctness | 10 |
| Security | 10 |
| Architecture | 14 |
| Infrastructure | 15 |
| Quality | 8 |
| **Total** | **57** |
