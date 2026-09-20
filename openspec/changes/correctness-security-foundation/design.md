## Context

See `proposal.md` for motivation and traceability. `docs/architecture/improvement-baseline.md` remains the gate source; the frozen relational target and its precedence rules are defined by `docs/database/README.md` and the referenced Restaurant Relational Model v2.0 artifacts.

**CURRENT FACT:** Restaurant.Api is one ASP.NET Core process and project. Controllers call service interfaces; implementations use one scoped `MyAppDbContext`, EF Core, and one SQL Server database. Reservation creation checks availability before insertion. Waiting-list promotion reads an entry, creates a reservation, removes the entry, then saves. Startup invokes `Database.Migrate()` and seed code invokes `EnsureCreated()`. JWT/database configuration is tracked, JWT has hardcoded fallback values, anonymous registration accepts `Role`, and logout does not invalidate a token. No test or Docker baseline exists.

**TARGET REQUIREMENT:** Satisfy only the listed Distributed Readiness Gate blockers while retaining the monolith, current database engine, current deployable boundary, incremental Layered Architecture, and frozen 16-entity Restaurant Relational Model v2.0.

**DESIGN DECISION:** Use direct EF Core/SQL Server mechanisms at the existing application-operation boundary. Do not introduce generic repositories, a Unit of Work wrapper, MediatR, CQRS infrastructure, or a multi-project rewrite.

**DEFERRED DECISION:** Service A/Service B selection, distributed communication technology, final bounded contexts, production platform, token revocation infrastructure, refresh tokens, and broader architecture decomposition.

## Goals / Non-Goals

**Goals:**

- Make reservation creation and waiting-list promotion transactionally correct under concurrency.
- Make security-critical configuration mandatory and external.
- Prevent anonymous privilege assignment and state truthful logout semantics.
- Establish one migration/seed lifecycle.
- Produce repeatable automated gate evidence using representative SQL Server behavior.
- Build and run the current API plus SQL Server reproducibly in containers.
- Record minimum current ownership and future boundary constraints.

**Non-Goals:**

- No Investigation II distributed topic, second service, broker, API Gateway, WebSockets, WebHooks, Quartz, Event Sourcing, Kubernetes, multiple databases, or distributed observability.
- No EF Core or SQL Server replacement, frontend rewrite, complete backend rewrite, four-project split, broad CQRS conversion, or MediatR default.
- No ARC-014 or INF-015 resolution.
- No database redesign, additional target entity, or final canonical restaurant role/permission catalog.

## Design Decisions

### 1. Reservation concurrency strategy

**Decision:** Every operation that can allocate a table interval starts an EF Core transaction through SQL Server's retrying execution strategy, acquires an update/range-stable lock on the target `RestaurantTable` row (`UPDLOCK` + `HOLDLOCK` or an equivalent verified SQL Server primitive), then re-runs lock and active-reservation overlap checks before writing. The transaction uses `Serializable` semantics for the allocation decision. Supporting composite indexes on reservation and table-lock lookup columns are added through an EF migration.

The table row is a stable lock anchor even when no matching reservation row exists. All allocation writers in this monolith must follow this path. A preliminary availability endpoint remains advisory; correctness is enforced only inside the write transaction.

Conflict result:

- Existing active reservation or table lock: preserve intended waiting-list behavior atomically, then return HTTP 409 with a stable safe error code.
- Concurrent loser: after acquiring the lock, it observes the committed winner and follows the same conflict outcome.
- Deadlock/transient transaction failure: retry the complete operation with a small bounded count and jitter using SQL Server/EF execution strategy behavior; no inner partial retry.
- Exhausted retry or unexpected database failure: rollback and return controlled failure without database details.

**Alternatives considered:**

- Check then insert under default isolation: rejected because it preserves the race.
- Unique index over table/date/start/end: rejected because uniqueness cannot enforce arbitrary interval overlap.
- Process-local mutex: rejected because it fails with multiple API instances and test/runtime process boundaries.
- `Serializable` predicate locking without a stable lock anchor: rejected as harder to reason about with missing rows and dependent on suitable indexes/query plans.
- New distributed lock: rejected because distribution has not begun and SQL Server already owns transactional state.

### 2. Transaction ownership

**Decision:** The service/application operation that coordinates the business use case owns transaction begin, commit, rollback, and conflict translation. Controllers remain HTTP adapters. `DbContext` remains the EF unit of work, but no new Unit of Work wrapper is added.

Boundaries:

| Operation | Begins | Commits | Rolls back |
|---|---|---|---|
| Create reservation | Before table lock and authoritative revalidation | Reservation, or intentional waiting-list conflict outcome | All writes on unexpected failure |
| Promote waiting entry | Before source-entry/table locks and revalidation | Reservation plus source promotion state/link | Reservation and waiting-list transition |
| Retry after transient SQL conflict | New complete attempt | Only final successful attempt | Failed attempt entirely |

The operation returns a typed result/known exception that middleware maps to 409. Generic database exceptions are not exposed. Transactions must be short and contain database work only.

**Alternatives considered:**

- Controller-owned transactions: rejected because controllers should not own business atomicity.
- Middleware transaction per HTTP request: rejected because read requests and unrelated writes have different boundaries and long transactions increase contention.
- Generic repository/UoW abstraction: rejected because EF Core already supplies tracking and transaction APIs; a wrapper would not solve the identified race.

### 3. Frozen relational model alignment

**Decision:** Persistence changes in this proposal target the frozen Restaurant Relational Model v2.0 exactly, without changing its 16-entity set.

- `Person` is the single source of human identity and contact data. `ClientProfile` and `UserAccount` each link to `Person` and do not duplicate name, email, phone, or identification attributes.
- The legacy `User.Role` string is not target authorization. Current effective authorization uses `Role`, `Permission`, `UserRole`, and `RolePermission`; authorization-change history is recorded in `AuditLog` rather than bridge history columns.
- `ReservationStatus` and `WaitingListStatus` replace the legacy generic `Status`. Business logic uses each catalog's stable `Code`, never magic numeric IDs.
- `WaitingListEntry.PreferredZoneId` is the optional FK to `Zone`. Successful promotion retains the entry, and `Reservation.SourceWaitingListEntryId` is the only persisted promotion link; no `WaitingListEntry.PromotedReservationId` is added.
- Normal turn classification is derived only when exactly one active `Turn` fully contains the reservation interval: `Turn.StartTime <= Reservation.StartTime AND Reservation.EndTime <= Turn.EndTime`. Derived turn is never persisted. Nullable `Reservation.AssignedTurnId` stores only explicit business/admin assignment or override; legacy `Reservation.TurnId` is not retained.
- The target table entity is `RestaurantTable`, with business uniqueness `UNIQUE (ZoneId, TableNumber)`.

This alignment selects no new physical design beyond the frozen model and does not define final canonical restaurant roles or permissions.

### 4. Waiting-list promotion model

**Decision:** Preserve the waiting-list entry after promotion, transition it to the applicable completed-promotion `WaitingListStatus`, and persist the durable link only as `Reservation.SourceWaitingListEntryId`. Enforce uniqueness of that non-null source link in SQL Server. Promotion locks the waiting-list source row and target `RestaurantTable` row in deterministic order, revalidates status by stable code, capacity, `PreferredZoneId`, turn derivation or explicit assignment, table lock, and active overlap, creates one reservation, and transitions the source in one transaction. No reverse `WaitingListEntry.PromotedReservationId` is added.

Repeated calls inspect the durable link and return the existing successful/already-promoted outcome without creating another reservation. Concurrent requests serialize on the source entry and table; database uniqueness is the final duplicate guard.

Lock order is always waiting-list entry then table for promotion, and table for direct reservation. Any operation needing both follows documented order to reduce deadlocks.

**Alternatives considered:**

- Continue deleting source entry: rejected because completed intent cannot be distinguished from never-existing intent, preventing reliable repeat behavior.
- In-memory idempotency cache: rejected because it is not durable and fails across restarts or instances.
- Require client idempotency keys now: deferred; useful more broadly, but a durable source identity already exists for this operation.

### 5. JWT and runtime configuration

**Decision:** Bind and validate strongly typed JWT options at startup. `JwtSettings:SecretKey`, issuer, audience, and valid expiry are mandatory. Remove all fallback secrets and tracked secret values. Database connection configuration is also mandatory. Standard ASP.NET Core environment variable keys are documented, including `JwtSettings__SecretKey` and `ConnectionStrings__ConnectionSql`.

Tracked `appsettings*.json` contains only non-secret defaults where safe. `.env.example` contains placeholders. Real `.env` files and local secret variants are ignored. Startup fails before serving requests when critical configuration is missing or invalid. The same validated options instance drives token issuance and validation to prevent drift.

**Alternatives considered:**

- Keep development fallback secrets: rejected because accidental deployment remains possible.
- Commit a working `.env`: rejected because it would version secrets.
- Add an external vault now: deferred because environment configuration satisfies current local/test scope without selecting deployment infrastructure.

### 6. Role assignment

**Decision:** Anonymous registration creates the required `Person` and `UserAccount` identity/account state but never lets caller-supplied `role` influence `UserRole` persistence. Public registration either rejects legacy role input or ignores it for backward wire compatibility, and it never self-assigns privileged membership. Any trusted initial membership policy or later role change is outside the anonymous flow and must use `UserRole` with authorization-change evidence in `AuditLog`. This change does not invent final canonical restaurant roles, permissions, or a role-management endpoint.

Characterization and authorization tests cover anonymous, unprivileged registered, and separately trusted privileged access without treating legacy `Employee`/`Admin` strings as the target model. Development privileged-account seed is enabled only explicitly, provisions authorization through target bridge records, and receives credentials through external development configuration; no universal production privileged credential is created.

**Alternatives considered:**

- Allow an enum of public roles: rejected because any caller-controlled privilege choice violates SEC-003.
- Add full admin user management now: rejected as unnecessary for the gate.

### 7. Logout/token semantics

**Decision:** Keep stateless access JWTs for current scope. Logout means the client deletes its token; the API response and README explicitly state that no server-side revocation occurs and an unexpired token remains valid. Token lifetime remains bounded and configuration-driven, but refresh-token implementation is deferred.

**Alternatives considered:**

- Token blacklist: provides immediate revocation but adds state, cleanup, availability, and every-request lookup concerns; deferred until explicit revocation requirements exist.
- Refresh tokens with short access tokens: improves session lifecycle but requires secure persistence, rotation, reuse detection, and new endpoints; deferred.
- Claim token invalidation today: rejected as false behavior.

### 8. Migrations and seed lifecycle

**Decision:** EF Core migrations are the only schema mechanism. Remove `EnsureCreated()` from application seed flow. Introduce an explicit migration execution mode used once before API readiness; Compose models it as a one-shot migration service. Bare local startup documents migration command then normal API command. Integration fixtures apply migrations to isolated databases.

Structural reference data required by implemented behavior, including separate `ReservationStatus` and `WaitingListStatus` catalogs, stays deterministic in migrations/model seed; application semantics resolve stable `Code` values rather than numeric IDs. Demo people with optional client/account profiles, zones, turns, restaurant tables, and privileged accounts are separated into idempotent development/test seed executed only through an explicit flag/environment. Real demo credentials are external.

API readiness requires successful migrations. Migration failure is terminal and visible; API does not silently create schema.

**Alternatives considered:**

- Keep `Migrate()` plus `EnsureCreated()`: rejected because mechanisms have incompatible lifecycle assumptions.
- Use `EnsureCreated()` in tests: rejected because it does not verify migrations.
- Always migrate in every API replica: avoided for the container baseline because parallel startup can create operational ambiguity; one-shot migration has clear ownership.

### 9. Test strategy

**Decision:** Add a focused test project, not a production project split. Use the repository's .NET version, a mainstream .NET test runner, and ASP.NET Core test hosting where needed. Exact package versions follow compatible stable versions at implementation time.

Test layers:

- Characterization API/service tests before behavior changes.
- Unit tests only for pure rules extracted because they are reused or difficult to verify through integration; no forced abstraction campaign.
- API authentication/authorization/configuration/error tests.
- SQL Server integration tests for migrations, constraints, transaction rollback, reservation concurrency, and waiting-list promotion.
- Concurrency tests use synchronized starts, isolated migrated databases, bounded timeouts, persisted-state assertions, and repeated runs. EF InMemory is not gate evidence for relational or concurrency behavior.

Reproducible commands cover restore/build, unit tests, SQL Server startup, migrations, integration tests, and cleanup. Tests do not depend on developer SQL Express instance names.

**Alternatives considered:**

- EF InMemory for all tests: rejected because it does not reproduce SQL Server locking, isolation, migration, or relational behavior.
- Mock every service: rejected because critical defects occur at transaction/persistence boundaries.
- Testcontainers dependency by default: not required; Docker Compose plus externally supplied test connection is sufficient. It may be reconsidered only if lifecycle reliability justifies it.

### 10. Base Docker strategy

**Decision:** Add one multi-stage Dockerfile for the current Restaurant.Api and Compose services for SQL Server, one-shot migrations, and Restaurant.Api. This is reproducible packaging, not distributed decomposition.

Compose behavior:

- SQL Server receives credentials from untracked environment values and exposes a health check.
- Migrator waits for SQL health, applies migrations, optionally applies explicit development seed, then exits successfully.
- API waits for successful migrator completion and exposes liveness/readiness evidence; readiness includes database accessibility.
- API/JWT/database settings are external. `.env.example` has placeholders only.
- Named volume preserves local data; documented cleanup can remove it for a known clean scenario.

**Alternatives considered:**

- Embed SQL Server in API container: rejected because one process/container responsibility and lifecycle would be obscured.
- Add Service B now: explicitly forbidden and unrelated to base reproducibility.
- Treat Compose as microservices architecture: rejected; Compose only orchestrates current app and infrastructure.

### 11. Data ownership and distributed boundary

**Decision:** Restaurant.Api remains sole owner of all current entities, tables, migrations, and writes. Current folders and service classes are internal layers, not deployable services. No database or ownership split occurs.

Future distributed proposal must identify Service A, Service B, ownership transfer, integration contracts, consistency, and technology independently. Internal selective CQRS remains optional and does not imply Event Sourcing, messaging, multiple databases, MediatR, or microservices.

**Alternatives considered:**

- Treat `ReservationService` and `WaitingListService` as microservices: rejected because both directly share context, transactions, and data.
- Split solution into four production projects now: rejected because gate requirements can be met incrementally in the current project; project separation is not itself correctness.

## Compatibility

- Existing routes remain.
- Existing successful request/response behavior remains unless listed below.
- Reservation allocation conflicts change to deterministic HTTP 409 instead of broad 400 behavior; intended waiting-list side effect remains atomic and documented.
- Public registration may continue accepting legacy JSON containing `role`, but supplied value cannot create `UserRole` membership or affect authorization. Responses do not claim a hardcoded `Employee` assignment.
- Logout route remains but no longer implies revocation.
- Waiting-list promotion retains source history under `WaitingListStatus`; `Reservation.SourceWaitingListEntryId` is the sole persisted link and existing success acknowledgement remains compatible where possible.
- Database schema changes use forward EF migrations. Existing rows receive nullable/source-safe migration behavior.

## Verification Strategy

| ID | Required evidence |
|---|---|
| COR-001 | Repeated simultaneous overlap test against SQL Server; one committed active reservation. |
| COR-002 | Failure injection for reservation conflict/wait-list path; only defined atomic outcome persists. |
| COR-005 | Success, rollback, repeated, and concurrent promotion tests; one linked reservation maximum. |
| SEC-001 | Repository secret scan and startup with external JWT secret. |
| SEC-002 | Startup fails with absent/invalid JWT settings; source scan finds no fallback. |
| SEC-003 | Registration with privileged role input creates no caller-selected or privileged `UserRole`; endpoint authorization matrix passes. |
| SEC-004 | Logout contract test and documentation prove client-side semantics; token behavior matches policy. |
| SEC-007 | Tracked configuration has no deployable DB credentials; external connection startup passes. |
| ARC-001 | Design/implementation review plus transaction rollback tests identify application-operation ownership. |
| ARC-002 | Ownership inventory maps every current `DbSet` and migration to Restaurant.Api. |
| ARC-003 | Built topology contains one deployable API and no Service B/distributed technology. |
| INF-001 | Documented clean build/unit/integration command sequence passes. |
| INF-002 | Restaurant.Api image builds and starts without embedded secrets. |
| INF-003 | Compose clean-start/health/stop/clean scenario succeeds. |
| INF-012 | Fresh, current, and failed migration scenarios plus explicit demo seed behavior pass. |
| QUA-001 | Test report covers reservation, promotion, auth, authorization, configuration, migration, and rollback behavior. |
| QUA-002 | Repeated synchronized SQL Server concurrency tests remain deterministic. |

No requirement becomes `Verified` during proposal. Verification occurs only after implementation and evidence review.

## Gate Exit Criteria

`Distributed Readiness Gate = PASS` only when all seventeen in-scope IDs are marked `Verified` against evidence above:

`COR-001`, `COR-002`, `COR-005`, `SEC-001`, `SEC-002`, `SEC-003`, `SEC-004`, `SEC-007`, `ARC-001`, `ARC-002`, `ARC-003`, `INF-001`, `INF-002`, `INF-003`, `INF-012`, `QUA-001`, `QUA-002`.

Additionally:

- No real JWT/database secret is tracked.
- No second deployable service or prohibited distributed technology exists.
- SQL Server-backed concurrency evidence passes repeatedly.
- Docker Compose reproduces API + SQL Server + migration lifecycle from clean state.
- Baseline status is updated only after evidence is reviewed; proposal completion alone is not verification.

## Proposed Implementation Phases

### Phase A - Characterization and verification harness

Create test project/harness, external test configuration, isolated SQL Server lifecycle, migration fixture, and characterization tests before changing behavior. This provides regression detection for every later phase.

### Phase B - Configuration, identity/authorization, and token semantics

Remove tracked/fallback secrets, validate startup options, externalize connection configuration, align registration with `Person`/`UserAccount` and target authorization bridges, prevent public role self-assignment, make development privileged-account seed explicit, and document/test stateless logout.

### Phase C - Migration and seed lifecycle

Remove `EnsureCreated()`, establish explicit migration mode, align affected schema/seed work with the frozen 16-entity target and stable status codes, separate structural/demo seed, and verify fresh/current/failure database paths. This must precede container orchestration and final concurrency schema indexes.

### Phase D - Reservation transaction and concurrency hardening

Add migration indexes, table-lock allocation protocol, explicit operation transaction, bounded deadlock retry, typed conflict, HTTP 409 mapping, rollback tests, and synchronized concurrency tests.

### Phase E - Waiting-list promotion hardening

Add unique `Reservation.SourceWaitingListEntryId`, retain the source entry with `WaitingListStatus`, avoid a reverse promotion link, implement atomic/repeat-safe promotion and deterministic lock ordering, and add promotion concurrency/rollback tests. This follows Phase D because it reuses allocation protocol.

### Phase F - Base containerization and reproducibility

Add API Dockerfile, SQL Server/one-shot migrator/API Compose topology, placeholder environment template, health/readiness coordination, explicit demo seed option, commands, and clean-start evidence. This follows migration/config stabilization to avoid encoding obsolete startup behavior.

### Phase G - Gate evidence review

Run documented full verification, inspect secret/configuration state, review ownership/topology, record evidence per ID, and update baseline statuses only for proven requirements.

## Risks / Trade-offs

- **Deadlocks or excess contention from per-table locking** -> deterministic lock order, short transactions, supporting indexes, bounded retries, and concurrency load tests.
- **Retry duplicates side effects** -> retry complete transactions only; keep external side effects outside transaction; enforce durable promotion uniqueness.
- **Migration startup failure blocks API** -> explicit one-shot migrator, clear logs/exit status, SQL health check, and rollback/restore documentation.
- **Concurrent migration execution** -> Compose has one migration owner; future multi-instance deployment must define separate deployment orchestration.
- **Concurrency test flakiness** -> synchronized barriers, isolated databases, persisted-state assertions, bounded timeouts, and repeated test runs.
- **Docker/SQL Server startup ordering** -> SQL health condition, bounded waits, migrator completion dependency, API readiness check.
- **Accidental behavior changes** -> Phase A characterization, narrow endpoint compatibility exceptions, and contract tests.
- **Security regressions** -> startup negative tests, role escalation tests, repository secret scan, and authorization matrix.
- **Serializable transaction performance** -> lock only one table allocation resource and keep transaction short; measure before broader changes.
- **Retaining promoted waiting entries changes query results** -> preserve status translation through `WaitingListStatus.Code`, document retained history, and test dashboard/list behavior.
- **Environment setup complexity** -> placeholders-only template and one documented local workflow; no external vault required for this scope.

## Files Likely Affected

- `Program.cs`
- `appsettings.json`, `appsettings.Development.json`, `.gitignore`, `.env.example`
- `Controllers/AuthController.cs`, `Controllers/ReservationsController.cs`, `Controllers/WaitingListController.cs`
- `DTOs/AuthDto.cs` and conflict/promotion DTOs only if needed
- `Services/Implementations/AuthService.cs`
- `Services/Implementations/ReservationService.cs`
- `Services/Implementations/WaitingListService.cs`
- Relevant service interfaces only where typed outcomes require contract changes
- `Data/MyAppDbContext.cs`, `Data/SeedData.cs`, `Migrations/`
- Exception middleware or narrowly scoped conflict mapping
- `RestauranteAPI.csproj`, `RestauranteAPI.slnx`
- New test project/files
- New `Dockerfile`, Compose file, `.dockerignore`, runtime scripts/configuration documentation
- `README.md`, `RestauranteAPI.http`, `docs/architecture/improvement-baseline.md` only after evidence review

## Files Expected to Remain Untouched

- Frontend repositories/files, if any outside this repository
- Unrelated dashboard, zone, turn, status, client, and table endpoint behavior except shared initialization/test compatibility
- Database engine and current database ownership topology
- Any nonexistent Service B or distributed infrastructure
- ARC-014 and INF-015 decision artifacts

## Migration Plan

1. Establish tests and capture current compatible behavior.
2. Deploy configuration changes only after required environment values are available; rollback restores prior application package but never restores committed secrets.
3. Apply forward migrations aligned with the frozen 16-entity model, including required indexes and the sole source link, with backup/restore plan and compatibility handling.
4. Switch initialization to explicit migrations; validate fresh and existing database paths.
5. Deploy transaction/concurrency behavior and monitor conflict/deadlock outcomes.
6. Publish Docker/Compose local baseline after non-container startup remains verified.

Database rollback uses migration downgrade only when proven safe; otherwise restore backup and prior application version. Security rollback must not reintroduce hardcoded or versioned secrets.

## Open Questions

- What future operational requirement would justify immediate token revocation or refresh tokens? Current change intentionally uses stateless client-side logout.
- Which deployment system will own production migrations after Investigation II? Current design defines local/test one-shot ownership only.
- Which capabilities become Service A and Service B, and which communication technology fits them? This requires a later proposal after gate passage.
