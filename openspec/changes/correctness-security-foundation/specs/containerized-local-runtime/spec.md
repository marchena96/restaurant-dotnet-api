## Purpose

Defines a minimal reproducible container runtime for the existing Restaurant.Api monolith and SQL Server without introducing a second deployable service.

## ADDED Requirements

### Requirement: Restaurant.Api has a deterministic container image
The existing Restaurant.Api SHALL build into a container image from the repository using a documented multi-stage Dockerfile.

#### Scenario: Clean API image build
- **WHEN** the documented image build runs from a clean checkout
- **THEN** it produces a runnable Restaurant.Api image without embedding runtime secrets

### Requirement: Local orchestration contains only the current API and required infrastructure
Docker Compose SHALL orchestrate Restaurant.Api, SQL Server, and any one-shot migration step required by this change. It SHALL NOT introduce Service B or imply a distributed application boundary.

#### Scenario: Compose topology inspection
- **WHEN** the Compose configuration is rendered
- **THEN** it contains the current API, SQL Server, and approved initialization components only

### Requirement: Container configuration is external
Compose SHALL obtain JWT and SQL Server credentials from untracked environment values and SHALL provide a tracked placeholders-only example.

#### Scenario: Missing required environment values
- **WHEN** required secrets are not supplied
- **THEN** orchestration fails clearly rather than using fallback credentials

#### Scenario: Valid environment values
- **WHEN** documented local values are supplied outside version control
- **THEN** all containers receive the required configuration

### Requirement: Startup ordering uses health and completion conditions
Orchestration SHALL wait for SQL Server readiness and successful migration completion before treating Restaurant.Api as ready.

#### Scenario: Delayed SQL Server startup
- **WHEN** SQL Server takes longer than the API image to initialize
- **THEN** migration and API startup wait or retry within documented bounds instead of entering an ambiguous state

#### Scenario: Migration failure
- **WHEN** the migration step fails
- **THEN** Restaurant.Api does not become ready and Compose exposes the failed component

### Requirement: Containerized development data is reproducible
The local container scenario SHALL support explicit, idempotent development/demo seed data without creating that data when the option is disabled.

#### Scenario: Fresh local scenario
- **WHEN** a clean local database is started with demo seed enabled
- **THEN** the documented demo data is available after migrations complete

### Requirement: Local runtime is reproducible
The repository SHALL document commands to configure, build, start, inspect, and stop the containerized baseline.

#### Scenario: Independent operator startup
- **WHEN** an operator follows the documentation from a clean checkout with Docker available
- **THEN** the API and SQL Server reach their documented healthy state
