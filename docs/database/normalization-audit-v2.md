# Normalization Audit v2.0

**Model:** Restaurant Relational Model v2.0

**Scope:** Frozen logical target only

**Entities audited:** 16

**Result:** `NORMALIZATION_GATE = PASS`

## Audit Method

Each entity is evaluated through Third Normal Form (3NF):

- **1NF:** Every attribute is atomic for this model, each row represents one fact of one entity type, and no repeating groups exist.
- **2NF:** The entity is in 1NF and every non-key attribute depends on a whole candidate key, not part of a composite key.
- **3NF:** The entity is in 2NF and non-key attributes do not depend transitively on a candidate key through another non-key attribute.

Nullable filtered-unique attributes are described as **conditional candidate keys**: uniqueness applies within the subset where the attribute is non-null. Physical SQL Server syntax is outside this logical audit.

## Summary

| # | Entity | 1NF | 2NF | 3NF | Result |
|---:|---|---|---|---|---|
| 1 | `Person` | Pass | Pass | Pass | Pass |
| 2 | `UserAccount` | Pass | Pass | Pass | Pass |
| 3 | `Role` | Pass | Pass | Pass | Pass |
| 4 | `Permission` | Pass | Pass | Pass | Pass |
| 5 | `UserRole` | Pass | Pass | Pass | Pass |
| 6 | `RolePermission` | Pass | Pass | Pass | Pass |
| 7 | `AuditLog` | Pass | Pass | Pass | Pass with deliberate polymorphic tradeoff |
| 8 | `ClientProfile` | Pass | Pass | Pass | Pass |
| 9 | `Zone` | Pass | Pass | Pass | Pass |
| 10 | `RestaurantTable` | Pass | Pass | Pass | Pass |
| 11 | `Turn` | Pass | Pass | Pass | Pass |
| 12 | `TableLock` | Pass | Pass | Pass | Pass |
| 13 | `ReservationStatus` | Pass | Pass | Pass | Pass |
| 14 | `Reservation` | Pass | Pass | Pass | Pass |
| 15 | `WaitingListStatus` | Pass | Pass | Pass | Pass |
| 16 | `WaitingListEntry` | Pass | Pass | Pass | Pass |

## Entity Audits

### 1. Person

- **Primary key:** `PersonId`.
- **Candidate keys:** `PersonId`; conditional candidate key `IdentificationNumber` when non-null.
- **Important functional dependencies:** `PersonId -> IdentificationNumber, FirstName, LastName, Email, PhoneNumber, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`. Within non-null identification values, `IdentificationNumber -> PersonId` and therefore the row.
- **1NF:** Pass. Identity and contact values are scalar; no account or customer-profile repeating groups are embedded.
- **2NF:** Pass. Candidate keys are single-attribute, so partial dependency is impossible.
- **3NF:** Pass. Contact attributes depend on the person key, not on an account or customer profile. Email is not treated as a determinant because it is neither required nor unique.
- **Exception/tradeoff:** Nullable identification uses conditional uniqueness. PII is intentionally centralized here rather than duplicated into `UserAccount` or `ClientProfile`.

### 2. UserAccount

- **Primary key:** `UserId`.
- **Candidate keys:** `UserId`, `PersonId`, `Username`.
- **Important functional dependencies:** Each candidate key determines `PasswordHash, IsActive, FailedLoginAttempts, LockedUntilUtc, PasswordChangedAtUtc, CreatedAtUtc, UpdatedAtUtc, RowVersion` and the other identifiers.
- **1NF:** Pass. Authentication and lockout state is represented by scalar attributes; roles are not stored as a list or text field.
- **2NF:** Pass. All candidate keys are single-attribute.
- **3NF:** Pass. Personal name, identification, email, phone, role names, and permission data are absent. Those facts are reached through normalized relationships.
- **Exception/tradeoff:** `PasswordHash` is security-sensitive but is functionally an account attribute. Role membership is normalized through `UserRole`; no direct `RoleId` exists.

### 3. Role

- **Primary key:** `RoleId`.
- **Candidate keys:** `RoleId`, `Code`.
- **Important functional dependencies:** `RoleId -> Code, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc`; `Code -> RoleId, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc`.
- **1NF:** Pass. Role properties are scalar; users and permissions are not stored as repeating values.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Descriptive attributes depend directly on either candidate key, and permission assignments reside in `RolePermission`.
- **Exception/tradeoff:** Exact restaurant role vocabulary is intentionally not frozen. This is scope control, not a normalization exception.

### 4. Permission

- **Primary key:** `PermissionId`.
- **Candidate keys:** `PermissionId`, `Code`.
- **Important functional dependencies:** `PermissionId -> Code, Name, Description, IsActive`; `Code -> PermissionId, Name, Description, IsActive`.
- **1NF:** Pass. Permission properties are scalar and role assignments are not embedded.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Descriptive attributes depend directly on candidate keys; role membership resides in `RolePermission`.
- **Exception/tradeoff:** Example permission codes are illustrative only and do not constitute frozen seed data.

### 5. UserRole

- **Primary key:** `(UserId, RoleId)`.
- **Candidate keys:** `(UserId, RoleId)` only.
- **Important functional dependencies:** `(UserId, RoleId) -> AssignedAtUtc, AssignedByUserId`.
- **1NF:** Pass. One row represents one user-role assignment; assignment metadata is scalar.
- **2NF:** Pass. `AssignedAtUtc` and `AssignedByUserId` describe the complete user-role assignment, not only `UserId` or only `RoleId`.
- **3NF:** Pass. User and role properties are not copied into the bridge, and assigning-user properties remain in `UserAccount`.
- **Exception/tradeoff:** No surrogate key is introduced. Composite identity prevents duplicate assignment pairs and preserves bridge semantics.

### 6. RolePermission

- **Primary key:** `(RoleId, PermissionId)`.
- **Candidate keys:** `(RoleId, PermissionId)` only.
- **Important functional dependencies:** The full key identifies the role-permission membership fact; there are no non-key attributes.
- **1NF:** Pass. One row represents one atomic role-permission membership.
- **2NF:** Pass. No non-key attributes can depend on only part of the key.
- **3NF:** Pass. Role and permission descriptions remain in their respective entities.
- **Exception/tradeoff:** No surrogate key is introduced. Composite identity prevents duplicate membership pairs.

### 7. AuditLog

- **Primary key:** `AuditLogId`.
- **Candidate keys:** `AuditLogId` only.
- **Important functional dependencies:** `AuditLogId -> UserId, ActionCode, EntityName, EntityId, OccurredAtUtc, CorrelationId, IpAddress, Details`.
- **1NF:** Pass. Every stored field is one scalar audit attribute; `Details` is an opaque optional value, not a modeled repeating group.
- **2NF:** Pass. The primary key is single-attribute.
- **3NF:** Pass. All audit event attributes depend directly on the audit record key. No modeled non-key determinant creates a transitive dependency.
- **Exception/tradeoff:** `EntityName + EntityId` is intentionally polymorphic and cannot be a normal relational FK to multiple target tables. This controlled referential-integrity tradeoff is not a 3NF failure: the pair describes the audit event target and does not determine duplicated target attributes in `AuditLog`. Normal operations are append-only, and `Details` must be sanitized of secrets.

### 8. ClientProfile

- **Primary key:** `ClientId`.
- **Candidate keys:** `ClientId`, `PersonId`.
- **Important functional dependencies:** `ClientId -> PersonId, IsActive, CustomerSinceUtc, Notes, UpdatedAtUtc`; `PersonId -> ClientId, IsActive, CustomerSinceUtc, Notes, UpdatedAtUtc`.
- **1NF:** Pass. Customer-participation attributes are scalar; reservations and waiting-list entries are separate relations.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Personal identity and contact information depend on `PersonId` in `Person` and are not duplicated here.
- **Exception/tradeoff:** `Notes` is free text scoped to the customer profile. It must not become a substitute for structured personal identity fields.

### 9. Zone

- **Primary key:** `ZoneId`.
- **Candidate keys:** `ZoneId`, `Code`.
- **Important functional dependencies:** `ZoneId -> Code, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`; `Code -> ZoneId, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`.
- **1NF:** Pass. Zone attributes are scalar; tables and waiting-list requests are separate relations.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Zone descriptions depend directly on a zone candidate key.
- **Exception/tradeoff:** None. One canonical `Zone` domain serves both restaurant tables and waiting-list preferences.

### 10. RestaurantTable

- **Primary key:** `TableId`.
- **Candidate keys:** `TableId`, `(ZoneId, TableNumber)`.
- **Important functional dependencies:** `TableId -> ZoneId, TableNumber, Capacity, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`; `(ZoneId, TableNumber) -> TableId, Capacity, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`.
- **1NF:** Pass. Table number and capacity are scalar; reservations and locks are separate relations.
- **2NF:** Pass. Non-key attributes depend on the complete composite business key, not on `ZoneId` or `TableNumber` alone.
- **3NF:** Pass. Zone name and description are not duplicated. They are determined by `ZoneId` in `Zone`.
- **Exception/tradeoff:** Surrogate `TableId` is retained for references while `(ZoneId, TableNumber)` enforces the scoped business key. Global `TableNumber` uniqueness is intentionally rejected.

### 11. Turn

- **Primary key:** `TurnId`.
- **Candidate keys:** `TurnId`, `Code`.
- **Important functional dependencies:** `TurnId -> Code, Name, StartTime, EndTime, IsActive, CreatedAtUtc, UpdatedAtUtc`; `Code -> TurnId, Name, StartTime, EndTime, IsActive, CreatedAtUtc, UpdatedAtUtc`.
- **1NF:** Pass. Each time bound is scalar and each row represents one named turn range.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. All turn attributes depend directly on candidate keys.
- **Exception/tradeoff:** Non-overlap among active derivable ranges is a cross-row invariant, not a normal-form issue. Its enforcement mechanism is deferred to physical implementation design.

### 12. TableLock

- **Primary key:** `TableLockId`.
- **Candidate keys:** `TableLockId` only.
- **Important functional dependencies:** `TableLockId -> TableId, LockDate, StartTime, EndTime, Reason, CreatedByUserId, IsActive, CreatedAtUtc, UpdatedAtUtc, RowVersion`.
- **1NF:** Pass. Date, interval, reason, actor, and lifecycle values are scalar.
- **2NF:** Pass. The primary key is single-attribute.
- **3NF:** Pass. Table number, zone, creator details, and turn details are not duplicated.
- **Exception/tradeoff:** No `TurnId` is stored because lock intervals are directly represented by date and times. Historical locks are normally retained rather than physically deleted.

### 13. ReservationStatus

- **Primary key:** `ReservationStatusId`.
- **Candidate keys:** `ReservationStatusId`, `Code`.
- **Important functional dependencies:** `ReservationStatusId -> Code, Name, BlocksAvailability, IsTerminal, SortOrder, IsActive`; `Code -> ReservationStatusId, Name, BlocksAvailability, IsTerminal, SortOrder, IsActive`.
- **1NF:** Pass. Status behavior and presentation values are scalar.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Reservation-specific status semantics depend directly on status candidate keys.
- **Exception/tradeoff:** Stable `Code` replaces magic numeric semantics; exact generated IDs are deliberately not frozen. This domain remains separate from `WaitingListStatus` because their lifecycle semantics differ.

### 14. Reservation

- **Primary key:** `ReservationId`.
- **Candidate keys:** `ReservationId`; conditional candidate key `SourceWaitingListEntryId` when non-null.
- **Important functional dependencies:** `ReservationId -> ClientId, TableId, ReservationStatusId, AssignedTurnId, SourceWaitingListEntryId, ReservationDate, StartTime, EndTime, GuestCount, Notes, CreatedByUserId, CreatedAtUtc, UpdatedAtUtc, RowVersion`. Within promoted rows, `SourceWaitingListEntryId -> ReservationId` and therefore the row.
- **1NF:** Pass. Reservation date, interval, party size, references, and notes are scalar.
- **2NF:** Pass. Candidate keys are single-attribute within their applicable domains.
- **3NF:** Pass. `ZoneName`, `TableNumber`, person names, contact information, status names, turn names, and waiting-list request details are not copied into the reservation.
- **Exception/tradeoff:** `AssignedTurnId` is semantically independent from the turn normally derived from `StartTime` and `EndTime`; it records an explicit assignment or override. No derived turn is persisted, so there is no derived-data dependency. `SourceWaitingListEntryId` is the sole promotion link, avoiding a duplicated bidirectional fact.

### 15. WaitingListStatus

- **Primary key:** `WaitingListStatusId`.
- **Candidate keys:** `WaitingListStatusId`, `Code`.
- **Important functional dependencies:** `WaitingListStatusId -> Code, Name, IsTerminal, SortOrder, IsActive`; `Code -> WaitingListStatusId, Name, IsTerminal, SortOrder, IsActive`.
- **1NF:** Pass. Status properties are scalar.
- **2NF:** Pass. Candidate keys are single-attribute.
- **3NF:** Pass. Waiting-list status semantics depend directly on status candidate keys.
- **Exception/tradeoff:** This catalog is intentionally separate from `ReservationStatus`; shared labels do not imply one functional domain.

### 16. WaitingListEntry

- **Primary key:** `WaitingListEntryId`.
- **Candidate keys:** `WaitingListEntryId` only.
- **Important functional dependencies:** `WaitingListEntryId -> ClientId, PreferredZoneId, WaitingListStatusId, RequestedDate, StartTime, EndTime, PartySize, Notes, CreatedByUserId, CreatedAtUtc, UpdatedAtUtc, RowVersion`.
- **1NF:** Pass. Request interval, party size, and foreign keys are scalar. Preferred zone is not stored as free text.
- **2NF:** Pass. The primary key is single-attribute.
- **3NF:** Pass. Person details, zone name, status name, and creator details remain in referenced entities.
- **Exception/tradeoff:** `PreferredZoneId` is nullable because a zone preference is optional. It references the same canonical `Zone` table used by `RestaurantTable`; no duplicate preference-zone domain exists.

## Preserved Normalization Findings

- Personal data belongs to `Person`, not `UserAccount` or `ClientProfile`.
- `UserRole` and `RolePermission` use proper composite primary keys.
- `Reservation` does not duplicate zone, table, person, status, turn, or waiting-list descriptive values.
- Waiting-list zone preference is a nullable FK, not a string.
- Role membership is not stored as text or a direct `RoleId` on `UserAccount`.
- `ReservationStatus` and `WaitingListStatus` are separate semantic domains.
- Derived turn is not persisted.
- Nullable `AssignedTurnId` represents a separate explicit business fact and is not redundant with interval-derived turn semantics.
- `AuditLog.EntityName + EntityId` is a deliberate polymorphic audit target and not a 3NF failure.

## Gate Decision

All 16 entities satisfy 1NF, 2NF, and 3NF for the frozen logical model. Deliberate tradeoffs are explicit and do not introduce hidden transitive or partial dependencies.

```text
NORMALIZATION_GATE = PASS
```
