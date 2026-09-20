## 1. Characterization and Test Foundation

- [x] 1.1 Add one test project to the solution with documented restore, build, unit-test, and integration-test commands (INF-001, QUA-001).
- [x] 1.2 Add external test configuration and an isolated SQL Server database lifecycle that applies EF Core migrations and never depends on developer SQL Express state (INF-001, INF-012).
- [ ] 1.3 Characterize current successful authentication, registration, reservation, conflict-to-waiting-list, and promotion HTTP behavior before changing it (QUA-001).
- [ ] 1.4 Add reusable synchronization and persisted-state assertion helpers for deterministic concurrent SQL Server tests without treating EF InMemory as relational evidence (QUA-002).
- [ ] 1.5 Document clean setup, execution, timeout, cleanup, and repeat commands for the full test baseline (INF-001, QUA-001, QUA-002).

## 2. Secure Configuration and Identity Semantics

- [ ] 2.1 Bind and validate mandatory JWT settings once at startup and use the same validated settings for issuance and validation (SEC-001, SEC-002).
- [ ] 2.2 Remove tracked JWT secret values and all hardcoded JWT fallbacks; add startup negative tests for missing or invalid settings (SEC-001, SEC-002).
- [ ] 2.3 Externalize SQL Server connection configuration, remove machine-specific/deployable credentials from tracked settings, and add startup/configuration tests (SEC-007).
- [ ] 2.4 Add a placeholders-only `.env.example`, ignore real `.env` values, and document ASP.NET Core environment variable names (SEC-001, SEC-007).
- [ ] 2.5 Align registration with `Person` as identity source and `UserAccount` as its optional account, without duplicating personal/contact fields; reject or ignore caller role input and create no caller-selected or privileged `UserRole` membership (SEC-003).
- [ ] 2.6 Add registration and authorization matrix tests proving anonymous callers cannot obtain privileged access while separately trusted privileged authorization through `Role`, `Permission`, `UserRole`, and `RolePermission` still works without freezing canonical role names (SEC-003, QUA-001).
- [ ] 2.7 Make logout response/documentation explicitly describe client-side token disposal and add tests proving behavior remains stateless until token expiry (SEC-004).
- [ ] 2.8 Make development privileged-account creation explicit and externally configured, provision membership through `UserRole`, record authorization changes in `AuditLog`, and ensure no universal privileged credential is silently created (SEC-001, SEC-003).

## 3. Migration and Seed Lifecycle

- [x] 3.1 Remove `EnsureCreated()` from seed initialization and make EF Core migrations the sole schema creation/update mechanism (INF-012).
- [ ] 3.2 Add an explicit migration execution mode with clear success/failure exit behavior and no normal request serving (INF-012).
- [ ] 3.3 Keep required structural reference data deterministic and migration-safe; use separate `ReservationStatus` and `WaitingListStatus` catalogs and resolve business semantics by stable `Code`, never magic numeric IDs (INF-012).
- [ ] 3.4 Separate people with optional client/account profiles, zones, turns, restaurant tables, and privileged-account demo data into an explicit idempotent development/test seed path (INF-012).
- [ ] 3.5 Align affected EF model and forward migration work with the frozen 16-entity target: `Person` owns human data; `ClientProfile` and `UserAccount` reference it; authorization uses `Role`, `Permission`, `UserRole`, and `RolePermission`; `RestaurantTable` enforces `UNIQUE (ZoneId, TableNumber)`; no extra entity is added (INF-012).
- [ ] 3.6 Represent normal turns only by exact unique active-turn containment, persist only nullable explicit `Reservation.AssignedTurnId`, and add neither legacy `Reservation.TurnId` nor `DerivedTurnId` (INF-012).
- [ ] 3.7 Add fresh database, current database, migration failure, structural seed repeat, and demo-seed enabled/disabled integration tests (INF-001, INF-012, QUA-001).

## 4. Reservation Transaction and Concurrency Integrity

- [ ] 4.1 Add EF migration indexes supporting `RestaurantTable`/date/`ReservationStatusId`/time overlap queries for reservations and table locks (COR-001).
- [ ] 4.2 Implement the per-`RestaurantTable` SQL Server lock-anchor protocol and authoritative overlap revalidation using `ReservationStatus.Code`/`BlocksAvailability` inside a short serializable transaction (COR-001, ARC-001).
- [ ] 4.3 Make reservation creation own transaction begin, commit, rollback, and complete-operation retry through the EF Core SQL Server execution strategy (COR-002, ARC-001).
- [ ] 4.4 Preserve intended conflict-to-waiting-list behavior as one atomic conflict outcome and rollback every write on unexpected failure (COR-002).
- [ ] 4.5 Introduce a safe typed reservation-conflict result and map it consistently to HTTP 409 without exposing SQL details (COR-001, COR-002).
- [ ] 4.6 Add bounded deadlock/transient retry behavior and verify retry exhaustion leaves no partial or duplicate state (COR-001, COR-002).
- [ ] 4.7 Add synchronized SQL Server tests for overlapping winners/losers, non-overlap, cancelled reservations, table locks, rollback, retry, and repeated runs (COR-001, COR-002, QUA-001, QUA-002).

## 5. Waiting-List Promotion Integrity

- [ ] 5.1 Add unique nullable `Reservation.SourceWaitingListEntryId` as the only persisted promotion link; do not add `WaitingListEntry.PromotedReservationId` (COR-005).
- [ ] 5.2 Retain promoted entries, transition them through `WaitingListStatus` identified by stable `Code`, and preserve existing DTO/status translation compatibility (COR-005).
- [ ] 5.3 Implement deterministic source-entry then `RestaurantTable` lock ordering, full promotion revalidation including `PreferredZoneId` FK and turn derivation/explicit assignment semantics, reservation creation, and waiting-state transition in one transaction (COR-005, ARC-001).
- [ ] 5.4 Return the existing result or a stable already-promoted outcome for repeated promotion without creating another reservation (COR-005).
- [ ] 5.5 Add SQL Server tests for promotion success, validation failure, injected rollback, repeated calls, concurrent calls, and contention with direct reservation creation (COR-005, QUA-001, QUA-002).

## 6. Base Containerization and Local Reproducibility

- [ ] 6.1 Add a multi-stage Dockerfile and `.dockerignore` for the existing Restaurant.Api only, with no embedded secrets (INF-002).
- [ ] 6.2 Add Docker Compose services for SQL Server, one-shot migrations, and Restaurant.Api without creating Service B or distributed infrastructure (INF-003, ARC-003).
- [ ] 6.3 Add SQL Server health checks, migration completion dependency, API liveness/readiness evidence, and bounded startup failure behavior (INF-003, INF-012).
- [ ] 6.4 Wire external JWT/database configuration and optional explicit demo seed into Compose using untracked values and the placeholders-only template (SEC-001, SEC-007, INF-003).
- [ ] 6.5 Document and verify clean build, start, inspect, stop, persistent restart, and destructive local cleanup commands (INF-002, INF-003).
- [ ] 6.6 Verify a clean container startup applies migrations once, loads demo data only when enabled, and reaches healthy API/database state (INF-002, INF-003, INF-012).

## 7. Ownership, Compatibility, and Gate Evidence

- [ ] 7.1 Document an ownership inventory assigning every current entity, table, migration, and write operation exclusively to Restaurant.Api for this change (ARC-002).
- [ ] 7.2 Verify the final topology remains one deployable Restaurant.Api and that folders/classes are not described as microservices (ARC-003).
- [ ] 7.3 Review all new abstractions and retain only those tied to a traced requirement; confirm no default MediatR, generic repository, Unit of Work wrapper, bus, or four-project split was introduced (ARC-002, ARC-003).
- [ ] 7.4 Update API/runtime documentation for explicit compatibility changes: HTTP 409 conflicts, no public role self-assignment, target identity/authorization semantics, stateless logout, retained promoted waiting entries, migrations, tests, and containers.
- [ ] 7.5 Run the complete documented verification suite and collect requirement-specific evidence for all seventeen in-scope IDs.
- [ ] 7.6 Scan tracked files for real JWT/database secrets and prohibited distributed technologies; record clean results.
- [ ] 7.7 Review evidence against exact Distributed Readiness Gate exit criteria and only then update baseline statuses for requirements proven `Verified`.
