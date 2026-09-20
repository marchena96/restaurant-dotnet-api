# Relational Design Decisions v2.0

**Model:** Restaurant Relational Model v2.0

**Scope:** Frozen logical target only

**Status:** `LOGICAL_MODEL = FROZEN`

**Implementation:** `PHYSICAL_IMPLEMENTATION = NOT_STARTED`

These decisions take precedence over the data dictionary, DBML, normalization audit, and generated diagrams.

## DBD-001 - Human Identity vs System Profiles

**Decision**

`Person` is the single source of truth for human identity and contact information. `UserAccount` represents authentication and account state. `ClientProfile` represents a person's participation as a restaurant customer.

**Cardinality**

- `Person 1 -> 0..1 UserAccount`
- `Person 1 -> 0..1 ClientProfile`

`UserAccount.PersonId` and `ClientProfile.PersonId` are required foreign keys and individually unique. A person can exist without either profile, while each profile belongs to exactly one person.

**Consequences**

- Personal name, identification number, email, and phone belong only to `Person`.
- `UserAccount` and `ClientProfile` must not duplicate those attributes.
- Authentication changes do not redefine human identity.
- Customer participation and system access remain independent optional roles of one person.

## DBD-002 - Email Policy

**Decision**

`Person.Email` is nullable contact information and is not globally unique in v2. `UserAccount.Username` remains unique.

**Consequences**

- The logical model does not assume login by email.
- Multiple people may share a contact email.
- A missing email does not prevent a person or customer profile from existing.
- Any future login-by-email requirement needs a separate explicit decision and impact analysis.

## DBD-003 - Historical Records and Deletion

**Decision**

Soft deletion is not applied indiscriminately. `IsActive` is used where deactivation is part of the domain.

Transactional or historical records must not normally be physically deleted when historical evidence matters. This includes reservations, table locks, and audit logs. A cancelled reservation remains stored with a cancelled status.

`UserRole` and `RolePermission` represent current effective authorization state, not repeated assignment or grant history. Revoking a current user-role membership may remove its `UserRole` row, provided the authorization change is recorded in `AuditLog` according to final implementation policy. The same current-state interpretation applies to `RolePermission`.

`AuditLog` provides historical evidence of authorization changes. Illustrative action vocabulary includes `USER_ROLE_ASSIGNED`, `USER_ROLE_REVOKED`, `ROLE_PERMISSION_GRANTED`, and `ROLE_PERMISSION_REVOKED`; these examples are not frozen API constants.

**Consequences**

- Deactivation and historical retention are domain-specific, not a universal table pattern.
- Security bridge rows carry current effective membership facts; authorization-change history belongs in `AuditLog`.
- No `UserRoleId`, `UserRole.IsActive`, `RevokedAtUtc`, or assignment-history table is added. Any such structure requires a separate future design decision.
- Historical snapshots are persisted only when a real business requirement exists.
- No speculative customer, table, zone, or status snapshots are added to this model.

## DBD-004 - Audit Log

**Decision**

`AuditLog` is append-only during normal application operation. Retention and archive policy is configurable; this logical model assigns no arbitrary legal retention duration.

`AuditLog` uses a deliberate polymorphic target:

```text
EntityName + EntityId
```

This pair is not a normal relational foreign key. It is an explicit audit-model tradeoff that permits records about different entity types without one nullable foreign key per target type.

**Security Boundary**

`AuditLog` must never persist:

- plaintext passwords;
- `PasswordHash` values;
- JWTs;
- connection strings;
- secrets.

`Details` is not an exception to this rule. `UserId` is nullable so system or anonymous events can be represented when needed.

## DBD-005 - Personal Identification

**Decision**

`Person.IdentificationNumber` is nullable. When present, its value must be unique.

**Physical Design Direction**

SQL Server implementation should use semantics that enforce uniqueness only for non-null identification values, such as a filtered unique index where appropriate. This is a physical implementation requirement, not a claim about the current schema.

**Consequences**

- Identification is not required for every restaurant customer.
- DBML notes the filtered uniqueness requirement because standard DBML cannot express the SQL Server filter exactly.

## DBD-006 - Table Numbering

**Decision**

`RestaurantTable.TableNumber` is not globally unique. Its business key is:

```text
UNIQUE (ZoneId, TableNumber)
```

For example, both `Main Hall / Table 1` and `Terrace / Table 1` are valid.

**Consequences**

- `RestaurantTable.TableId` remains the surrogate primary key.
- Table lookup by number alone is not guaranteed to identify one table.
- References use `TableId`; user-facing table identity can use zone plus table number.

## DBD-007 - Turn Derivation and Explicit Assignment

**Decision**

A reservation has a derived turn only when exactly one active `Turn` fully contains its interval:

```text
Turn.StartTime <= Reservation.StartTime
AND
Reservation.EndTime <= Turn.EndTime
```

Interpret matching active turns as follows:

```text
0 matches  => DerivedTurn = NULL
1 match    => DerivedTurn = that Turn
>1 matches => invalid/ambiguous Turn configuration
```

The derived turn is never persisted.

`Reservation.AssignedTurnId` is nullable and has these semantics:

```text
AssignedTurnId IS NULL
=> use the derived Turn when one exists.

AssignedTurnId IS NOT NULL
=> explicit business/admin assignment or override is authoritative for business classification.
```

`AssignedTurnId` is not redundant with reservation times. It stores a separate administrative or business fact. The explicit name is required instead of generic `TurnId`.

**Invariant**

Active `Turn` definitions used for derivation must not create ambiguous containment.

Exact enforcement belongs to physical/application design and is not selected by this documentation change.

**Consequences**

- Do not add `DerivedTurnId`.
- Do not add `TurnAssignmentMode`.
- Do not infer that every reservation must have `AssignedTurnId`.
