## Why

Restaurant.Api cannot safely begin distributed implementation while reservation races, implicit transaction boundaries, unsafe runtime secrets and role assignment, ambiguous database initialization, absent automated tests, and absent reproducible container startup remain open. This change establishes the minimum correct, secure, testable, and reproducible monolith foundation required by `docs/architecture/improvement-baseline.md` before any Investigation II distributed proposal.

## Problem Statement

The current ASP.NET Core monolith performs availability checks and writes through one shared `MyAppDbContext`, but it does not protect the reservation invariant against concurrent requests or explicitly own compound transactions. Public registration accepts caller-supplied roles, JWT and database configuration are versioned or have hardcoded fallbacks, logout semantics are misleading, startup mixes `Migrate()` and `EnsureCreated()`, and no automated or containerized verification baseline exists.

## Current State

**CURRENT FACT:** One .NET project and process implement Controllers -> service interfaces -> service implementations -> `MyAppDbContext` -> EF Core -> SQL Server. Restaurant.Api owns all current tables and behavior. `ReservationService` and `WaitingListService` directly coordinate reservations, table locks, waiting-list entries, statuses, turns, and clients. There are no automated tests, Docker definitions, second deployable service, or inter-service communication.

## What Changes

- Establish SQL Server-backed concurrency protection and explicit application-operation transaction ownership for reservation creation and waiting-list promotion.
- Make waiting-list promotion atomic, deterministic, concurrency-safe, and repeat-safe.
- Externalize JWT signing material and database connection configuration; fail startup when required configuration is absent or invalid.
- Prevent public role self-assignment, align authorization with `Role`, `Permission`, `UserRole`, and `RolePermission`, and define truthful stateless JWT logout semantics for the current scope.
- Use EF Core migrations as the single schema mechanism and separate structural seed data from optional development/demo seed data.
- Add an automated test foundation covering critical correctness, security, persistence, error, concurrency, and promotion behavior against representative SQL Server persistence where relational semantics matter.
- Add one Restaurant.Api Dockerfile plus SQL Server orchestration through Docker Compose, placeholders-only environment documentation, health/startup coordination, deterministic migrations, and reproducible development data.
- Align affected persistence work with the frozen 16-entity Restaurant Relational Model v2.0 without adding entities or redesigning that model, while Restaurant.Api remains sole owner; defer Service A/Service B selection and distributed technology decisions.
- Preserve current HTTP contracts unless a security or correctness correction requires an explicit change. Reservation conflicts will have a deterministic conflict response; public role input will no longer grant privileges; logout documentation will state client-side token disposal rather than server-side revocation.

## Baseline Traceability

| Requirement ID | Problem | Proposed Change | Verification |
|---|---|---|---|
| COR-001 | Concurrent requests can reserve one table and overlapping interval. | Serialize conflicting reservation decisions inside an explicit SQL Server transaction and map exhausted conflicts to HTTP 409. | SQL Server integration test starts simultaneous attempts; exactly one reservation succeeds. |
| COR-002 | Compound reservation/waiting-list work has no explicit transaction boundary. | Application operation owns begin, commit, rollback, and bounded retry. | Failure-injection tests prove no partial state. |
| COR-005 | Promotion can race or be repeated without defined semantics. | Lock/revalidate source and destination in one transaction; define repeat-safe outcome. | Repeated and concurrent promotion tests produce one reservation and deterministic later responses. |
| SEC-001 | JWT secret is versioned. | Remove secret values from tracked settings; use external configuration. | Secret scan plus startup test with injected secret. |
| SEC-002 | JWT fallback is hardcoded. | Remove fallbacks and validate required configuration at startup. | Missing/invalid JWT configuration prevents startup. |
| SEC-003 | Public registration accepts privileged role input. | Remove caller control over `UserRole` membership; anonymous registration cannot provision privileged roles, and trusted role provisioning remains outside public registration without freezing canonical restaurant roles here. | Registration tests prove `Admin` input cannot elevate or create privileged membership. |
| SEC-004 | Logout implies invalidation but leaves JWT valid. | Adopt stateless JWT client-side logout now; document residual validity and defer blacklist/refresh-token infrastructure. | Contract test verifies endpoint semantics and documentation; token behavior matches stated policy. |
| SEC-007 | Database connection details are versioned. | Supply connection through environment/configuration and retain only safe placeholders. | Repository scan plus externally configured startup. |
| ARC-001 | Transaction ownership is implicit. | Reservation and promotion application operations own transactions; controllers and generic infrastructure wrappers do not. | Design review and rollback tests identify boundaries and outcomes. |
| ARC-002 | Shared model has no declared owner. | Declare Restaurant.Api sole owner for this change and prohibit direct ownership claims by future services. | Ownership map covers all current `DbSet` entities. |
| ARC-003 | Folders may be mistaken for deployable services. | Record monolith boundary and defer Service A/Service B and communication technology selection. | Architecture review confirms one deployable application and no second service. |
| INF-001 | No automated test foundation exists. | Add test project(s), fixtures, representative SQL Server integration environment, and reproducible commands. | Clean environment runs documented test commands. |
| INF-002 | Restaurant.Api has no container baseline. | Add multi-stage Dockerfile for the existing API only. | Image builds and API starts with external configuration. |
| INF-003 | No reproducible local orchestration exists. | Compose Restaurant.Api and SQL Server with health/startup coordination. | Clean `docker compose` startup reaches healthy API and database. |
| INF-012 | Startup mixes `Migrate()` and `EnsureCreated()`. | Use migrations only for schema; define structural and demo seed policies by environment. | Fresh/existing/test database scenarios initialize deterministically. |
| QUA-001 | Critical behavior has no automated coverage. | Characterization plus correctness/security/integration tests become gate evidence. | Test report covers listed invariants and security paths. |
| QUA-002 | Reservation concurrency is untested. | Add repeatable SQL Server-backed concurrent reservation and promotion tests. | Multiple runs remain deterministic and leave valid state. |

## Scope

- Current monolith only; incremental edits around affected startup, authentication, reservation, waiting-list, persistence, testing, and local runtime concerns.
- One SQL Server database and one `MyAppDbContext` remain.
- Layered Architecture remains the foundation.
- Restaurant Relational Model v2.0 remains frozen at 16 entities; this change reconciles implementation wording with that target and does not redesign it.
- No automatic introduction of MediatR, repositories, buses, Unit of Work wrappers, or four-project solution split. Any small abstraction added during implementation must solve a traced requirement and remain locally justified.

## Non-Goals

- Investigation II distributed topic implementation, Service B, or complete microservice migration.
- RabbitMQ, WebSockets, WebHooks, Event Sourcing, API Gateway, Quartz, message brokers, Kubernetes, or distributed observability.
- Multiple databases, database-engine replacement, EF Core replacement, frontend rewrite, broad CQRS introduction, or MediatR adoption.
- Resolution of ARC-014, INF-015, final bounded-context decomposition, final Service A/Service B selection, or distributed communication technology.
- Definition of final canonical restaurant roles or permissions beyond the frozen authorization structure.

## Capabilities

### New Capabilities

- `reservation-integrity`: Concurrent reservation and waiting-list promotion invariants, transaction ownership, conflict semantics, and evidence.
- `security-baseline`: Mandatory external JWT/database configuration, safe public registration and trusted role membership semantics, and explicit stateless logout semantics.
- `database-lifecycle`: One migrations-based schema strategy with separated structural and development/demo seed behavior.
- `verification-foundation`: Reproducible automated unit, API, security, SQL Server integration, rollback, and concurrency verification.
- `containerized-local-runtime`: Existing Restaurant.Api and SQL Server container build, configuration, health, startup, and orchestration baseline.
- `architecture-boundaries`: Temporary ownership of the complete current model by Restaurant.Api and explicit deferral of distributed decomposition.

### Modified Capabilities

None. No existing OpenSpec capabilities are registered.

## Impact

Likely implementation impact is limited to `Program.cs`, authentication DTO/controller/service code, reservation and waiting-list service code, `MyAppDbContext`/migrations/seed initialization, exception-to-HTTP conflict mapping, tracked configuration, solution/test projects, Docker/Compose/environment templates, and runtime/test documentation. Current endpoints and SQL Server remain; explicitly corrected security and conflict semantics may alter unsafe or ambiguous behavior. No production code is changed by this proposal.
