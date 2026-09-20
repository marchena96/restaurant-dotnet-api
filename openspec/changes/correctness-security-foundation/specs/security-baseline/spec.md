## Purpose

Defines mandatory runtime secret configuration, non-privileged public registration, target identity/authorization boundaries, and truthful JWT logout behavior for the current monolithic API.

## ADDED Requirements

### Requirement: JWT signing configuration is external and mandatory
The system SHALL obtain JWT signing material from runtime configuration, SHALL NOT contain a tracked real secret or hardcoded fallback, and SHALL fail startup when signing configuration is missing or invalid.

#### Scenario: Valid external JWT configuration
- **WHEN** valid JWT settings are supplied through runtime configuration
- **THEN** the API starts and issues tokens signed with those settings

#### Scenario: Missing JWT secret
- **WHEN** the JWT signing secret is absent
- **THEN** startup fails with a configuration error before serving requests

#### Scenario: Repository configuration inspection
- **WHEN** tracked configuration and source are inspected
- **THEN** no usable JWT signing secret or fallback value is present

### Requirement: Database connection configuration is external and mandatory
The system SHALL obtain its SQL Server connection configuration from runtime configuration and SHALL NOT track deployable database credentials or machine-specific connection details.

#### Scenario: Valid external database configuration
- **WHEN** a valid external connection string is supplied
- **THEN** database initialization and API startup use that connection

#### Scenario: Missing database configuration
- **WHEN** the required connection string is absent
- **THEN** startup or the explicit migration command fails clearly before normal request processing

#### Scenario: Placeholder environment template
- **WHEN** the environment template is inspected
- **THEN** it contains documented placeholders and no real secret values

### Requirement: Public registration cannot assign privileges
The public registration endpoint SHALL treat `Person` as the human identity source and `UserAccount` as account state, SHALL NOT duplicate personal/contact attributes into account/profile records, and SHALL ignore or reject caller-controlled role input. Anonymous registration SHALL NOT create caller-selected or privileged `UserRole` membership and SHALL NOT assume a hardcoded `Employee` role.

#### Scenario: Caller requests Admin role
- **WHEN** an anonymous registration payload supplies `Admin` or another privileged role
- **THEN** the created account is not privileged

#### Scenario: Normal public registration
- **WHEN** a valid caller registers without trusted role provisioning
- **THEN** the resulting person/account has no caller-selected or privileged role membership

### Requirement: Authorization uses target current-state bridges
The system SHALL represent current effective authorization through `Role`, `Permission`, `UserRole`, and `RolePermission`. Authorization-change history SHALL be preserved in `AuditLog`, and this change SHALL NOT define final canonical restaurant roles.

#### Scenario: Trusted role membership changes
- **WHEN** an authorized workflow assigns or revokes role membership
- **THEN** current membership is reflected by `UserRole` and authorization-change evidence is appended to `AuditLog`

### Requirement: Privileged role assignment requires trusted authorization
The system SHALL NOT provide anonymous privileged-role provisioning. Any future role-assignment operation SHALL require an authenticated authorized administrator and is outside the public registration flow.

#### Scenario: Anonymous privileged assignment attempt
- **WHEN** an anonymous caller attempts to assign or change a privileged role
- **THEN** the operation is unavailable or denied

### Requirement: Logout semantics match stateless JWT behavior
For this change, the system SHALL define logout as client-side token disposal. The API SHALL state that an already issued access token remains valid until expiration and SHALL NOT claim server-side revocation.

#### Scenario: Logout acknowledgement
- **WHEN** an authenticated client invokes logout
- **THEN** the API acknowledges logout without claiming that the access token was revoked

#### Scenario: Token used after logout
- **WHEN** a previously issued unexpired token is presented after logout
- **THEN** authentication behavior remains consistent with stateless JWT validation
