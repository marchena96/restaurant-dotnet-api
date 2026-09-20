## Purpose

Defines reproducible automated evidence required to verify correctness, security, persistence, rollback, and concurrency before distributed work begins.

## ADDED Requirements

### Requirement: Automated verification has one documented entry point
The repository SHALL document commands that build the solution and run all baseline unit and integration tests from a clean checkout.

#### Scenario: Clean verification run
- **WHEN** an operator provides documented prerequisites and executes the verification commands
- **THEN** build and test results complete without undocumented local setup

### Requirement: Pure rules have focused tests
Business rules that can execute without infrastructure SHALL have deterministic focused tests.

#### Scenario: Rule boundary coverage
- **WHEN** valid, invalid, and boundary inputs are supplied to an extracted pure rule
- **THEN** expected decisions are verified without SQL Server or HTTP startup

### Requirement: Relational behavior uses representative SQL Server persistence
Concurrency, transaction, migration, constraint, and relational tests SHALL execute against SQL Server and SHALL NOT rely on EF Core InMemory as evidence for those behaviors.

#### Scenario: Concurrency suite persistence provider
- **WHEN** reservation concurrency tests execute
- **THEN** they use a migrated SQL Server database

#### Scenario: Relational rollback test
- **WHEN** an injected failure interrupts a compound operation
- **THEN** SQL Server state proves the transaction rolled back all related writes

### Requirement: Authentication and authorization are covered
Automated API tests SHALL verify login, required JWT configuration, anonymous registration role constraints, target `UserRole`/`RolePermission` authorization, protected endpoints, and privileged endpoints without depending on legacy `User.Role` strings or freezing canonical restaurant role names.

#### Scenario: Public role escalation test
- **WHEN** registration supplies a privileged role value
- **THEN** the test proves the resulting identity is not privileged

#### Scenario: Protected endpoint matrix
- **WHEN** anonymous, unprivileged registered, and separately trusted privileged identities access representative endpoints
- **THEN** each receives the expected authorization outcome

### Requirement: Relevant errors are covered
Automated tests SHALL verify safe and stable responses for reservation conflicts, unauthorized access, invalid configuration, and unexpected persistence failures.

#### Scenario: Reservation conflict response test
- **WHEN** a reservation conflict is induced
- **THEN** the API returns the documented status and does not expose database internals

### Requirement: Reservation concurrency evidence is repeatable
The suite SHALL include repeated concurrent reservation and promotion tests that assert final persisted state, not only HTTP responses.

#### Scenario: Repeated concurrency run
- **WHEN** the concurrency suite runs repeatedly against clean isolated databases
- **THEN** each run produces one valid allocation and no duplicate or partial state

### Requirement: Waiting-list promotion behavior is covered
Automated tests SHALL verify promotion success, retained `WaitingListEntry`, `WaitingListStatus` transition, sole `Reservation.SourceWaitingListEntryId` link, rollback, repeat, and concurrent execution.

#### Scenario: Promotion verification matrix
- **WHEN** promotion tests execute across success, failure, repeated, and concurrent cases
- **THEN** persisted reservations and waiting-list state satisfy the promotion contract
