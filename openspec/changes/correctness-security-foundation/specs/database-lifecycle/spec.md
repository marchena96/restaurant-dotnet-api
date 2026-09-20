## Purpose

Defines one deterministic EF Core migrations and seed lifecycle for local startup, container startup, and automated SQL Server integration tests.

## ADDED Requirements

### Requirement: EF Core migrations are the only schema mechanism
The system SHALL use EF Core migrations to create and update relational schema and SHALL NOT combine migrations with `EnsureCreated()` for the same database lifecycle.

#### Scenario: Fresh database initialization
- **WHEN** the explicit migration operation runs against an empty supported SQL Server database
- **THEN** all migrations are applied in order and the resulting schema matches the current model

#### Scenario: Existing database initialization
- **WHEN** the explicit migration operation runs against an up-to-date database
- **THEN** it completes without recreating or deleting schema

### Requirement: Schema migration is explicit and precedes API readiness
The runtime SHALL complete a single explicit migration operation before the API is considered ready.

#### Scenario: Migration succeeds
- **WHEN** SQL Server is reachable and all migrations apply successfully
- **THEN** the API may start or become ready

#### Scenario: Migration fails
- **WHEN** a migration cannot be applied
- **THEN** startup fails visibly and the API is not reported ready

### Requirement: Structural seed data is migration-safe
Required structural reference data SHALL be deterministic, idempotent, and compatible with the migration lifecycle. Reservation and waiting-list status catalogs SHALL remain separate, and application semantics SHALL use their stable `Code` values rather than numeric IDs.

#### Scenario: Repeated structural initialization
- **WHEN** structural initialization executes more than once
- **THEN** required records remain unique and stable

### Requirement: Development demo data is explicit
Demo people with optional `UserAccount`/`ClientProfile`, zones, turns, `RestaurantTable` records, and trusted privileged memberships SHALL be loaded only through an explicit development/test configuration and SHALL NOT be silently created in every environment.

#### Scenario: Development demo seed enabled
- **WHEN** the documented development seed option is enabled after migration
- **THEN** reproducible demo data is available without duplicate records

#### Scenario: Development demo seed disabled
- **WHEN** the option is absent or disabled
- **THEN** no demo accounts or business records are created automatically

### Requirement: Migrations preserve the frozen target boundary
Forward EF Core migrations created during implementation SHALL align with the frozen 16-entity Restaurant Relational Model v2.0 and SHALL NOT add a seventeenth entity or redesign its documented relationships.

#### Scenario: Target model inspection
- **WHEN** the migrated EF model is compared with the frozen relational target
- **THEN** identity, authorization, status, waiting-list, turn, and restaurant-table structures match the documented 16-entity model

### Requirement: Tests use isolated migrated databases
Persistence integration tests SHALL initialize isolated SQL Server databases through migrations and controlled test seed data.

#### Scenario: Integration test setup
- **WHEN** an integration test suite starts
- **THEN** it receives an isolated migrated schema and known seed state

#### Scenario: Integration test cleanup
- **WHEN** the suite completes
- **THEN** its database state cannot affect a later test run
