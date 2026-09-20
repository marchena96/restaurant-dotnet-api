# Relational Data Dictionary v2.0

**Model:** Restaurant Relational Model v2.0

**Scope:** Logical target, not current EF Core schema

**Entity count:** 16

**Bridge table count:** 2

**Status:** `LOGICAL_MODEL = FROZEN`

## Interpretation

- Types use SQL Server-oriented notation to make intended precision and length explicit.
- `identity`, `PK`, `FK`, `UNIQUE`, `CHECK`, defaults, and indexes are logical target requirements. They are not claims about the current physical schema.
- Foreign-key delete actions, constraint names, filtered-index syntax, deployment sequencing, and migration mechanics remain physical-design concerns.
- All timestamps ending in `Utc` represent UTC values.
- `rowversion` columns are concurrency tokens, not date/time values.

## Identity and Access

### 1. Person

Single source of truth for human identity and contact information.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `PersonId` | `int identity` | No | PK | Surrogate person identifier |
| `IdentificationNumber` | `nvarchar(50)` | Yes | Unique when non-null | Optional personal identification value |
| `FirstName` | `nvarchar(100)` | No | | Given name |
| `LastName` | `nvarchar(100)` | No | | Family name |
| `Email` | `nvarchar(254)` | Yes | Not globally unique | Contact email; not a login identifier |
| `PhoneNumber` | `nvarchar(30)` | Yes | | Contact phone number |
| `IsActive` | `bit` | No | Default `true` | Person activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraints**

- `IdentificationNumber` must be unique when present.
- The physical SQL Server target should use non-null uniqueness semantics, such as a filtered unique index where appropriate.
- `Email` is nullable and not globally unique in v2.

**Data classification:** `IdentificationNumber`, `FirstName`, `LastName`, `Email`, and `PhoneNumber` are personal data/PII. Access, logging, display, retention, and export must follow the applicable privacy and security policy. These values must not be duplicated into `UserAccount` or `ClientProfile`.

### 2. UserAccount

Authentication and account state for an optional system account belonging to a person.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `UserId` | `int identity` | No | PK | Surrogate account identifier |
| `PersonId` | `int` | No | FK -> `Person.PersonId`, UNIQUE | Account owner |
| `Username` | `nvarchar(100)` | No | UNIQUE | Login name |
| `PasswordHash` | `nvarchar(255)` | No | | Password verifier |
| `IsActive` | `bit` | No | | Account activation state |
| `FailedLoginAttempts` | `int` | No | Default `0` | Consecutive failed-login count |
| `LockedUntilUtc` | `datetime2(3)` | Yes | | Optional lock expiration |
| `PasswordChangedAtUtc` | `datetime2(3)` | Yes | | Last password-change timestamp |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraints**

- `UNIQUE (PersonId)` establishes the optional one-to-one relationship from `Person`.
- `UNIQUE (Username)`.
- `CHECK (FailedLoginAttempts >= 0)`.
- No `Role`, `RoleId`, name, identification, email, or phone column belongs here.

**Data classification:** `PasswordHash` is security-sensitive. It must never be returned as ordinary profile data or copied to audit details. It is not plaintext, but still requires restricted access and protection.

### 3. Role

Named authorization grouping assigned to users through `UserRole`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `RoleId` | `int identity` | No | PK | Surrogate role identifier |
| `Code` | `nvarchar(80)` | No | UNIQUE | Stable role code |
| `Name` | `nvarchar(100)` | No | | Display name |
| `Description` | `nvarchar(300)` | Yes | | Optional description |
| `IsActive` | `bit` | No | | Role activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp, included consistently for this mutable catalog |

Exact canonical restaurant role names are not frozen by this model.

### 4. Permission

Atomic authorization capability assigned to roles through `RolePermission`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `PermissionId` | `int identity` | No | PK | Surrogate permission identifier |
| `Code` | `nvarchar(120)` | No | UNIQUE | Stable permission code |
| `Name` | `nvarchar(120)` | No | | Display name |
| `Description` | `nvarchar(300)` | Yes | | Optional description |
| `IsActive` | `bit` | No | | Permission activation state |

Illustrative, non-frozen authorization vocabulary may include `reservation.read`, `reservation.create`, `reservation.update`, `reservation.cancel`, `waiting-list.promote`, `table.manage`, `user.manage`, `role.manage`, and `audit.read`. These examples do not establish required seed data or a final permission catalog.

### 5. UserRole

Bridge table representing current effective many-to-many membership between `UserAccount` and `Role`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `UserId` | `int` | No | PK, FK -> `UserAccount.UserId` | Assigned user |
| `RoleId` | `int` | No | PK, FK -> `Role.RoleId` | Assigned role |
| `AssignedAtUtc` | `datetime2(3)` | No | | Assignment timestamp |
| `AssignedByUserId` | `int` | Yes | FK -> `UserAccount.UserId` | Optional assigning account |

**Constraint and semantics:** `PRIMARY KEY (UserId, RoleId)`. No surrogate `UserRoleId` exists. A revoked membership may remove this current-state row when the authorization change is recorded in `AuditLog` under final implementation policy; repeated assignment history is not stored here.

### 6. RolePermission

Bridge table representing current effective many-to-many authorization state between `Role` and `Permission`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `RoleId` | `int` | No | PK, FK -> `Role.RoleId` | Role |
| `PermissionId` | `int` | No | PK, FK -> `Permission.PermissionId` | Permission |

**Constraint and semantics:** `PRIMARY KEY (RoleId, PermissionId)`. No surrogate `RolePermissionId` exists. Grant/revoke history belongs in `AuditLog`, not this current-state bridge.

### 7. AuditLog

Append-only operational audit record during normal application operation.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `AuditLogId` | `bigint identity` | No | PK | Surrogate audit identifier |
| `UserId` | `int` | Yes | FK -> `UserAccount.UserId` | Actor when known; null supports system or anonymous events |
| `ActionCode` | `nvarchar(120)` | No | | Stable action identifier |
| `EntityName` | `nvarchar(100)` | No | Polymorphic target part | Target entity type |
| `EntityId` | `nvarchar(100)` | Yes | Polymorphic target part | Target identifier serialized as text |
| `OccurredAtUtc` | `datetime2(3)` | No | | Event timestamp |
| `CorrelationId` | `uniqueidentifier` | Yes | | Optional operation correlation identifier |
| `IpAddress` | `varchar(45)` | Yes | | Optional IPv4 or IPv6 text |
| `Details` | `nvarchar(max)` | Yes | | Optional sanitized event details |

**Rules and tradeoffs**

- Audit rows are append-only during normal application operation.
- Retention and archival are configurable; no legal duration is invented here.
- `EntityName + EntityId` is intentionally polymorphic and is not a normal relational FK.
- `Details` and every other audit column must never contain plaintext passwords, password hashes, JWTs, connection strings, or secrets.
- Authorization-change evidence belongs here rather than in current-state bridge rows. Illustrative, non-frozen action codes include `USER_ROLE_ASSIGNED`, `USER_ROLE_REVOKED`, `ROLE_PERMISSION_GRANTED`, and `ROLE_PERMISSION_REVOKED`.

## Customer

### 8. ClientProfile

Optional customer participation profile for one person.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `ClientId` | `int identity` | No | PK | Surrogate customer identifier |
| `PersonId` | `int` | No | FK -> `Person.PersonId`, UNIQUE | Person represented by this profile |
| `IsActive` | `bit` | No | | Customer participation state |
| `CustomerSinceUtc` | `datetime2(3)` | No | | Customer relationship start |
| `Notes` | `nvarchar(500)` | Yes | | Optional customer-specific notes |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |

**Constraint:** `UNIQUE (PersonId)` establishes the optional one-to-one relationship from `Person`. Personal identity and contact attributes remain in `Person`.

## Restaurant Operations

### 9. Zone

Canonical restaurant-area entity. There is exactly one `Zone` entity in v2; waiting-list preferences reference it.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `ZoneId` | `int identity` | No | PK | Surrogate zone identifier |
| `Code` | `nvarchar(50)` | No | UNIQUE | Stable zone code |
| `Name` | `nvarchar(100)` | No | | Display name |
| `Description` | `nvarchar(300)` | Yes | | Optional description |
| `IsActive` | `bit` | No | | Zone activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

### 10. RestaurantTable

Physical restaurant table. The entity name is `RestaurantTable`, not `Table`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `TableId` | `int identity` | No | PK | Surrogate table identifier |
| `ZoneId` | `int` | No | FK -> `Zone.ZoneId` | Containing zone |
| `TableNumber` | `nvarchar(20)` | No | Business-key part | Number or label unique within a zone |
| `Capacity` | `int` | No | | Supported guest count |
| `IsActive` | `bit` | No | | Table activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraints**

- `UNIQUE (ZoneId, TableNumber)`.
- `CHECK (Capacity > 0)`.
- `TableNumber` is not globally unique; `Main Hall / Table 1` and `Terrace / Table 1` may coexist.

### 11. Turn

Named time range used for normal turn derivation or an explicit reservation assignment.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `TurnId` | `int identity` | No | PK | Surrogate turn identifier |
| `Code` | `nvarchar(50)` | No | UNIQUE | Stable turn code |
| `Name` | `nvarchar(100)` | No | | Display name |
| `StartTime` | `time(0)` | No | | Range start |
| `EndTime` | `time(0)` | No | | Range end |
| `IsActive` | `bit` | No | | Turn activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |

**Constraints and invariant**

- `CHECK (StartTime < EndTime)`.
- A reservation has a derived turn only when exactly one active turn fully contains it: `Turn.StartTime <= Reservation.StartTime AND Reservation.EndTime <= Turn.EndTime`.
- Zero matches produce no derived turn; one match produces that turn; more than one match is invalid/ambiguous configuration.
- Active turn definitions used for derivation must not create ambiguous containment. Exact enforcement is deferred to physical/application design.

### 12. TableLock

Operational unavailability interval for a restaurant table.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `TableLockId` | `int identity` | No | PK | Surrogate lock identifier |
| `TableId` | `int` | No | FK -> `RestaurantTable.TableId` | Locked table |
| `LockDate` | `date` | No | | Business date of lock |
| `StartTime` | `time(0)` | No | | Interval start |
| `EndTime` | `time(0)` | No | | Interval end |
| `Reason` | `nvarchar(300)` | No | | Operational reason |
| `CreatedByUserId` | `int` | No | FK -> `UserAccount.UserId` | Creating account |
| `IsActive` | `bit` | No | | Lock activation state |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraint:** `CHECK (StartTime < EndTime)`. `TableLock` has no `TurnId`; an obsolete DTO shape is not a reason to add one.

## Reservations

### 13. ReservationStatus

Reservation-specific status catalog.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `ReservationStatusId` | `int identity` | No | PK | Surrogate status identifier |
| `Code` | `nvarchar(50)` | No | UNIQUE | Stable semantic code |
| `Name` | `nvarchar(100)` | No | | Display name |
| `BlocksAvailability` | `bit` | No | | Whether reservations in this status block a table interval |
| `IsTerminal` | `bit` | No | | Whether status ends normal lifecycle progression |
| `SortOrder` | `int` | No | | Display ordering |
| `IsActive` | `bit` | No | | Status activation state |

**Constraint:** `CHECK (SortOrder >= 0)`. `Code` replaces magic numeric status semantics. Exact status IDs are not frozen.

### 14. Reservation

Customer reservation for one table and time interval.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `ReservationId` | `int identity` | No | PK | Surrogate reservation identifier |
| `ClientId` | `int` | No | FK -> `ClientProfile.ClientId` | Customer profile |
| `TableId` | `int` | No | FK -> `RestaurantTable.TableId` | Reserved table |
| `ReservationStatusId` | `int` | No | FK -> `ReservationStatus.ReservationStatusId` | Current reservation status |
| `AssignedTurnId` | `int` | Yes | FK -> `Turn.TurnId` | Explicit assignment or override only |
| `SourceWaitingListEntryId` | `int` | Yes | FK -> `WaitingListEntry.WaitingListEntryId`, unique when non-null | Promotion source |
| `ReservationDate` | `date` | No | | Business date |
| `StartTime` | `time(0)` | No | | Interval start |
| `EndTime` | `time(0)` | No | | Interval end |
| `GuestCount` | `int` | No | | Reserved party size |
| `Notes` | `nvarchar(500)` | Yes | | Optional reservation notes |
| `CreatedByUserId` | `int` | Yes | FK -> `UserAccount.UserId` | Creating account when applicable |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraints and semantics**

- `CHECK (StartTime < EndTime)`.
- `CHECK (GuestCount > 0)`.
- `SourceWaitingListEntryId` must be unique when non-null.
- `AssignedTurnId` is an optional explicit business/admin assignment or override. When null, use the uniquely derived turn when one exists; when non-null, the explicit assignment is authoritative for business classification.
- Derived turn uses exact active-turn containment: `Turn.StartTime <= Reservation.StartTime AND Reservation.EndTime <= Turn.EndTime`. Zero matches means null, one means that turn, and multiple matches mean invalid/ambiguous configuration.
- Derived turn is never persisted. No `DerivedTurnId` or `TurnAssignmentMode` exists.
- `SourceWaitingListEntryId` is the only persisted promotion link. Do not add `WaitingListEntry.PromotedReservationId`.
- A cancelled reservation remains stored with its cancelled status.
- Do not duplicate `ZoneName`, `TableNumber`, person names, status names, or other referenced attributes.

## Waiting List

### 15. WaitingListStatus

Waiting-list-specific status catalog, separate from `ReservationStatus`.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `WaitingListStatusId` | `int identity` | No | PK | Surrogate status identifier |
| `Code` | `nvarchar(50)` | No | UNIQUE | Stable semantic code |
| `Name` | `nvarchar(100)` | No | | Display name |
| `IsTerminal` | `bit` | No | | Whether status ends normal lifecycle progression |
| `SortOrder` | `int` | No | | Display ordering |
| `IsActive` | `bit` | No | | Status activation state |

**Constraint:** `CHECK (SortOrder >= 0)`.

### 16. WaitingListEntry

Customer request awaiting a suitable reservation.

| Column | Type | Null | Key/default | Meaning |
|---|---|---:|---|---|
| `WaitingListEntryId` | `int identity` | No | PK | Surrogate entry identifier |
| `ClientId` | `int` | No | FK -> `ClientProfile.ClientId` | Customer profile |
| `PreferredZoneId` | `int` | Yes | FK -> `Zone.ZoneId` | Optional preference using canonical `Zone` |
| `WaitingListStatusId` | `int` | No | FK -> `WaitingListStatus.WaitingListStatusId` | Current waiting-list status |
| `RequestedDate` | `date` | No | | Requested business date |
| `StartTime` | `time(0)` | No | | Requested interval start |
| `EndTime` | `time(0)` | No | | Requested interval end |
| `PartySize` | `int` | No | | Requested party size |
| `Notes` | `nvarchar(500)` | Yes | | Optional request notes |
| `CreatedByUserId` | `int` | Yes | FK -> `UserAccount.UserId` | Creating account when applicable |
| `CreatedAtUtc` | `datetime2(3)` | No | | Creation timestamp |
| `UpdatedAtUtc` | `datetime2(3)` | No | | Last update timestamp |
| `RowVersion` | `rowversion` | No | | Optimistic concurrency token |

**Constraints and semantics**

- `CHECK (StartTime < EndTime)`.
- `CHECK (PartySize > 0)`.
- There is no `PreferredZone` string and no duplicate zone entity in v2.

## Relationship Cardinalities

| Principal | Cardinality | Dependent/path |
|---|---|---|
| `Person` | `1 -> 0..1` | `UserAccount` via unique `UserAccount.PersonId` |
| `Person` | `1 -> 0..1` | `ClientProfile` via unique `ClientProfile.PersonId` |
| `UserAccount` | `N:M` | `Role` via `UserRole` |
| `Role` | `N:M` | `Permission` via `RolePermission` |
| `UserAccount` | `1 -> 0..N` | `AuditLog` |
| `Zone` | `1 -> 0..N` | `RestaurantTable` |
| `Zone` | `1 -> 0..N` | `WaitingListEntry` as optional preferred zone |
| `ClientProfile` | `1 -> 0..N` | `Reservation` |
| `ClientProfile` | `1 -> 0..N` | `WaitingListEntry` |
| `RestaurantTable` | `1 -> 0..N` | `Reservation` |
| `RestaurantTable` | `1 -> 0..N` | `TableLock` |
| `ReservationStatus` | `1 -> 0..N` | `Reservation` |
| `WaitingListStatus` | `1 -> 0..N` | `WaitingListEntry` |
| `Turn` | `1 -> 0..N` | `Reservation` only through nullable `AssignedTurnId` |
| `WaitingListEntry` | `1 -> 0..1` | `Reservation` through unique non-null `Reservation.SourceWaitingListEntryId` |

`UserRole.AssignedByUserId`, `TableLock.CreatedByUserId`, `Reservation.CreatedByUserId`, and `WaitingListEntry.CreatedByUserId` also reference `UserAccount` as documented in their column definitions.

## Critical Operational Indexes

Minimum target indexes supporting expected access paths:

| Entity | Ordered columns | Purpose |
|---|---|---|
| `Reservation` | `(TableId, ReservationDate, ReservationStatusId, StartTime, EndTime)` | Table/date/status interval availability queries |
| `TableLock` | `(TableId, LockDate, StartTime, EndTime)` | Table/date lock interval queries |
| `WaitingListEntry` | `(WaitingListStatusId, RequestedDate, StartTime, EndTime)` | Waiting-list status/date interval queries |
| `AuditLog` | `(UserId, OccurredAtUtc)` | Actor timeline queries |
| `AuditLog` | `(EntityName, EntityId, OccurredAtUtc)` | Target timeline queries |

These indexes assist query performance and concurrency workflows. They do not by themselves prevent arbitrary time-interval overlap. Reservation overlap, table-lock overlap, and active-turn ambiguity require explicit implementation-level enforcement and transaction design.
