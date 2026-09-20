# Migration Impact Analysis v2.0

**Purpose:** Compare CURRENT AS-IS EF Core relational model with frozen Restaurant Relational Model v2.0 before any physical design or EF Core migration is generated.

**Scope:** Analysis only. Repository source and migration snapshot were inspected; no live database was queried. Statements about possible legacy values therefore require deployment-database preflight.

```text
AS_IS_ENTITY_COUNT = 9
TARGET_ENTITY_COUNT = 16
MIGRATION_IMPLEMENTATION = NOT_STARTED
EF_CORE_MIGRATIONS_GENERATED = NO
PRODUCTION_SCHEMA_CHANGED = NO
```

## Evidence Base

CURRENT AS-IS facts come from:

- `Data/MyAppDbContext.cs` and `Migrations/MyAppDbContextModelSnapshot.cs` for mapped entities, columns, keys, indexes, relationships, delete behavior, and model seed;
- `Migrations/20260608145656_InitialCreate.cs` for current migration-defined tables and constraints;
- all files in `Models/` for CLR nullability/default semantics;
- `Data/SeedData.cs` and `Program.cs` for initialization and runtime seed behavior;
- schema-sensitive services in `Services/Implementations/`, especially `AuthService`, `ReservationService`, `TableService`, `LockService`, `TurnService`, and `WaitingListService`.

TARGET facts come from frozen `docs/database/decisions/relational-design-decisions-v2.md`, `relational-data-dictionary-v2.md`, and `relational-model-v2.dbml`. This analysis does not redesign TARGET.

## CURRENT AS-IS Relational Model

The CURRENT AS-IS EF model has exactly nine entities/tables.

| # | Entity / table | Columns and PK | FKs and delete behavior | Unique constraints / indexes | Semantic dependencies |
|---:|---|---|---|---|---|
| 1 | `Client` / `Clients` | `Id int identity` PK; required `FirstName nvarchar(max)`, `LastName nvarchar(max)`, `PhoneNumber nvarchar(max)`, `IdCard nvarchar(450)` | None inbound from this row; principal for reservations and waiting entries | Unique `IX_Clients_IdCard` | Service treats `IdCard` as mandatory unique identity; deletion is attempted physically |
| 2 | `Reservation` / `Reservations` | `Id int identity` PK; `Date date`, `StartTime time`, `EndTime time`, `GuestCount int`, `CreatedAt datetime2`, required `ClientId`, `TableId`, `StatusId`, `TurnId` | All four FKs use `Restrict` | Non-unique FK indexes on each FK | `TurnId` mandatory; creation writes `StatusId = 2`, `TurnId = 1`; cancellation fallback uses ID 4; deletion is physical |
| 3 | `WaitingListEntry` / `WaitingLists` | `Id int identity` PK; `Date date`, `StartTime time`, `EndTime time`, `PartySize int`, required `Status nvarchar(max)`, nullable `PreferredZone nvarchar(max)`, required `ClientId` | `ClientId -> Clients.Id`, `Restrict` | Non-unique `IX_WaitingLists_ClientId` | CLR default and creation use `Waiting`; service recognizes `Waiting`, `Assigned`, `Cancelled`; promotion deletes source row |
| 4 | `Table` / `Tables` | `Id int identity` PK; required `TableNumber nvarchar(max)`, `Capacity int`, `ZoneId int` | `ZoneId -> Zones.Id`, `Restrict` | Non-unique `IX_Tables_ZoneId`; no table-number uniqueness | Lookup by table number alone uses first match; no capacity check constraint |
| 5 | `Zone` / `Zones` | `Id int identity` PK; required `Name nvarchar(max)`, `IsAvailable bit` | Principal for tables | No unique constraint or index on name | `IsAvailable` controls whether table operations proceed; names are used by seed lookups and free-text preference matching would depend on them |
| 6 | `Status` / `Statuses` | `Id int identity` PK; required `Name nvarchar(max)` | Principal for reservations; FK uses `Restrict` | No unique constraint on name | Model seed fixes IDs 1-4; services also compare names and accept mutable status CRUD |
| 7 | `TableLock` / `TableLocks` | `Id int identity` PK; `Date date`, `StartTime time`, `EndTime time`, required `Reason nvarchar(max)`, `TableId int` | `TableId -> Tables.Id`, `Restrict` | Non-unique `IX_TableLocks_TableId` | Overlap checked in application on create only; release physically deletes lock |
| 8 | `Turn` / `Turns` | `Id int identity` PK; required `Name nvarchar(max)`, `StartTime time`, `EndTime time`, `IsActive bit` | Principal for reservations; FK uses `Restrict` | No unique constraint or operational range index | Application checks existence of any containing active turn, not exactly one; reservation/promotion writes `TurnId = 1` |
| 9 | `User` / `Users` | `Id int identity` PK; required `Username nvarchar(450)`, `FullName nvarchar(max)`, `Email nvarchar(450)`, `PasswordHash nvarchar(max)`, `Role nvarchar(max)` | None | Unique username and unique email indexes | Login emits one role-string JWT claim; registration accepts arbitrary role text; no FK-backed authorization |

No AS-IS columns exist for notes, created-by users, update timestamps, rowversions, audit events, permission assignments, promotion provenance, status behavior flags, or stable codes. They must not be treated as existing data.

## TARGET Model

Frozen TARGET contains exactly these 16 entities:

1. `Person`
2. `UserAccount`
3. `Role`
4. `Permission`
5. `UserRole`
6. `RolePermission`
7. `AuditLog`
8. `ClientProfile`
9. `Zone`
10. `RestaurantTable`
11. `Turn`
12. `TableLock`
13. `Reservation`
14. `ReservationStatus`
15. `WaitingListEntry`
16. `WaitingListStatus`

`UserRole` and `RolePermission` are the two bridge tables and represent current effective authorization state. `AuditLog` carries historical authorization-change evidence. Derived turn is never persisted.

## AS-IS to TARGET Matrix

| AS-IS object | TARGET object | Change classification | Automatic feasibility | Transformation | Data-loss risk | Compatibility risk | Decision / notes |
|---|---|---|---|---|---|---|---|
| `Client` | `Person` + `ClientProfile` | SPLIT, NORMALIZE, BACKFILL | Partial | Move identity/contact fields to generated `Person`; create profile and preserve/rekey client references deliberately | High if IDs/FKs are changed destructively | High | Prefer additive backfill and explicit old-to-new key map |
| `User` | `Person` + `UserAccount` | SPLIT, NORMALIZE, TRANSFORM | Partial | Create separate person; split `FullName`; move login fields to account | High for name parsing or false person merge | High | Never merge with client by name alone |
| `User.Role` | `Role` + `UserRole`; future `Permission` + `RolePermission` | NORMALIZE, BACKFILL, SEMANTIC-CHANGE | Partial | Distinct role strings become reviewed role rows and effective memberships | Medium | High | Canonical role/permission policy is application/OpenSpec dependency |
| no authorization history | `AuditLog` | ADD | Schema yes; history no | Start recording future events | Existing history unavailable | Medium | Do not fabricate past assign/revoke events |
| no audit table | `AuditLog` | ADD | Yes for empty structure | New append-only records after cutover | None for schema; historical gap remains | Medium | Actor references and retention policy need physical design |
| `Table` | `RestaurantTable` | RENAME, CONSTRAINT, BACKFILL | High after preflight | Preserve table key if feasible; add target metadata | Low | Medium | New uniqueness scope is zone plus number |
| global table-number behavior | `UNIQUE(ZoneId, TableNumber)` | SEMANTIC-CHANGE, CONSTRAINT | Only after preflight | Detect duplicates per zone; update number-only lookups | Medium | High | AS-IS has no global uniqueness either, but service assumes first match |
| `Status` | `ReservationStatus` | SPECIALIZE, BACKFILL | Partial | Map names to stable reviewed codes and behavior flags | High if guessed | High | Numeric IDs are not semantic contracts in TARGET |
| magic status IDs | `ReservationStatus.Code` | SEMANTIC-CHANGE, BREAKING | No code-only automation | Replace ID literals/name branching with code semantics during implementation | High | High | Current literals: pending 2, turn 1, cancelled fallback 4 |
| `WaitingListEntry.Status` text | `WaitingListStatus` FK | NORMALIZE, BACKFILL, DATA-CLEANUP | For known exact values only | Map reviewed text to stable status codes | Medium | High | Unknown values block affected rows; no silent coercion |
| `WaitingListEntry.PreferredZone` text | `PreferredZoneId -> Zone` | NORMALIZE, TRANSFORM, DATA-CLEANUP | Only unique normalized matches | Trim/case-normalize for comparison, then use reviewed zone ID | Medium | Medium | Unknown/ambiguous values remain unresolved; do not create arbitrary zones |
| `Reservation.TurnId` required | nullable `AssignedTurnId` plus derived turn | SEMANTIC-CHANGE, TRANSFORM | Conditional | Compare current FK to exactly-one containment result | High | High | Never bulk-copy every `TurnId` to `AssignedTurnId` |
| promotion deletes waiting row | retained entry in `ASSIGNED` state | BEHAVIOR-CHANGE, BREAKING | Future behavior only | Stop deletion after compatible code deployment | Existing provenance already lost | High | Existing deleted rows cannot be recovered from repository evidence |
| no promotion link | `Reservation.SourceWaitingListEntryId` | ADD, BACKFILL | Only for proven links | Link extant entries only with deterministic evidence | High if guessed | Medium | Legacy source remains null when provenance unproven |
| `TableLock` | target `TableLock` | TRANSFORM, ADD, CONSTRAINT | Partial | Rename date, preserve interval/reason/table; add actor/lifecycle/audit metadata | Medium | High | Required creator has no AS-IS source |
| `Zone` | target `Zone` | TRANSFORM, ADD, CONSTRAINT | Partial | Preserve identity/name; derive reviewed code; map availability to activation | Medium | Medium | Code and metadata need policy/backfill |
| `Turn` | target `Turn` | TRANSFORM, ADD, CONSTRAINT | Partial | Preserve interval/activity; derive reviewed code and metadata | Medium | High | Preflight exact containment and ambiguity |
| no RBAC bridges | `UserRole`, `RolePermission` | ADD, BACKFILL | UserRole partial; permissions no | Build current role membership; seed reviewed permissions later | Medium | High | No invented permission catalog |
| no compatibility structures | transitional old/new structures | COMPATIBILITY-LAYER | Feasible | Dual-read/write or bounded maintenance window | Low if controlled | High if omitted | Expand/migrate/contract recommended |

## Column-Level Data Mapping

Classification for target columns without an AS-IS source: `DEFAULT`, `GENERATED`, `DERIVED`, `MANUAL-BACKFILL`, `NULLABLE-INTRODUCTION`, or `STRUCTURAL-SEED`.

### Client to Person and ClientProfile

| CURRENT AS-IS | TARGET | Mapping |
|---|---|---|
| `Client.Id` | `ClientProfile.ClientId` | Candidate identity preservation, not assumed. Preserving it minimizes rewrites of `Reservation.ClientId` and `WaitingListEntry.ClientId`; otherwise use explicit key map. |
| none | `Person.PersonId` | GENERATED; capture mapping from old client ID. |
| `Client.IdCard` | `Person.IdentificationNumber` | Direct value transform after uniqueness/length/blank preflight. AS-IS required; TARGET nullable. |
| `Client.FirstName` | `Person.FirstName` | Direct after length validation. |
| `Client.LastName` | `Person.LastName` | Direct after length validation. |
| `Client.PhoneNumber` | `Person.PhoneNumber` | Direct after length validation. |
| none | `Person.Email` | NULLABLE-INTRODUCTION. |
| none | `Person.IsActive`, `ClientProfile.IsActive` | DEFAULT, subject to cutover policy. |
| none | `Person.CreatedAtUtc`, `Person.UpdatedAtUtc`, `ClientProfile.CustomerSinceUtc`, `ClientProfile.UpdatedAtUtc` | MANUAL-BACKFILL because repository has no original client timestamps. |
| none | `Person.RowVersion` | GENERATED by SQL Server. |
| generated person key | `ClientProfile.PersonId` | BACKFILL from migration key map; enforce unique after validation. |
| none | `ClientProfile.Notes` | NULLABLE-INTRODUCTION. |

### User to Person, UserAccount, Role, and UserRole

| CURRENT AS-IS | TARGET | Mapping |
|---|---|---|
| `User.Id` | `UserAccount.UserId` | Candidate identity preservation for JWT/reference continuity; no current schema FK points to users, but external/API identifiers may. |
| none | `Person.PersonId` | GENERATED; do not reuse user ID as person ID implicitly. |
| `User.FullName` | `Person.FirstName`, `Person.LastName` | MANUAL-BACKFILL/TRANSFORM. One string has no deterministic general split; seeded `Administrator` lacks a separate last name. |
| `User.Email` | `Person.Email` | Direct after length validation. TARGET allows duplicates and null, while AS-IS has unique required email. |
| none | `Person.IdentificationNumber`, `Person.PhoneNumber` | NULLABLE-INTRODUCTION. |
| `User.Username` | `UserAccount.Username` | Direct after max-length preflight. |
| `User.PasswordHash` | `UserAccount.PasswordHash` | Direct security-sensitive transfer; never copy into audit details. |
| none | account/person activation and timestamps | DEFAULT or MANUAL-BACKFILL; no historical source. |
| none | lockout/password-change columns | DEFAULT (`0`) or NULLABLE-INTRODUCTION as specified by TARGET. |
| none | rowversions | GENERATED by SQL Server. |
| generated person key | `UserAccount.PersonId` | BACKFILL from migration key map; unique constraint added after validation. |
| distinct `User.Role` text | `Role.Code`, `Role.Name`; `UserRole(UserId, RoleId)` | Reviewed TRANSFORM plus BACKFILL. Do not infer final code vocabulary automatically. |
| none | `UserRole.AssignedAtUtc`, `AssignedByUserId` | MANUAL-BACKFILL timestamp; actor NULLABLE-INTRODUCTION. Value denotes migrated current state, not original assignment history. |
| none | `Permission`, `RolePermission` | STRUCTURAL-SEED only after authorization design is reconciled; no AS-IS permission source. |

### Reservation

| CURRENT AS-IS | TARGET | Mapping |
|---|---|---|
| `Reservation.Id` | `Reservation.ReservationId` | Preserve where feasible; otherwise key map. |
| `ClientId` | `ClientId` | Map to resulting `ClientProfile.ClientId`; direct only if profile IDs are preserved. |
| `TableId` | `TableId` | Map to `RestaurantTable.TableId`; direct only if IDs are preserved. |
| `StatusId` | `ReservationStatusId` | Map through reviewed status-name/code crosswalk, never assume same numeric ID. |
| `TurnId` | `AssignedTurnId` | Conditional semantic transform described in Turn Migration; not direct copy. |
| `Date` | `ReservationDate` | Direct rename. |
| `StartTime`, `EndTime`, `GuestCount` | same semantic target columns | Direct after target check preflight. |
| `CreatedAt` | `CreatedAtUtc` | Direct only after confirming stored values are UTC; name/default suggest UTC but SQL column does not enforce it. |
| none | `SourceWaitingListEntryId` | NULLABLE-INTRODUCTION; backfill only proven links. |
| none | `Notes`, `CreatedByUserId` | NULLABLE-INTRODUCTION. |
| none | `UpdatedAtUtc` | MANUAL-BACKFILL, likely cutover timestamp unless stronger evidence exists. |
| none | `RowVersion` | GENERATED. |

### WaitingListEntry

| CURRENT AS-IS | TARGET | Mapping |
|---|---|---|
| `Id` | `WaitingListEntryId` | Preserve for extant rows where feasible. Deleted historical rows cannot be recreated. |
| `ClientId` | `ClientId` | Map through client-profile key map. |
| `Date` | `RequestedDate` | Direct rename. |
| `StartTime`, `EndTime`, `PartySize` | same semantic target columns | Direct after checks. |
| `Status` | `WaitingListStatusId` | Normalize known strings through code crosswalk; unknown values require cleanup. |
| `PreferredZone` | `PreferredZoneId` | Unique normalized match to existing `Zone.Name`; null stays null. |
| none | `Notes`, `CreatedByUserId` | NULLABLE-INTRODUCTION. |
| none | `CreatedAtUtc`, `UpdatedAtUtc` | MANUAL-BACKFILL; no AS-IS timestamps. |
| none | `RowVersion` | GENERATED. |

### Table, Status, TableLock, Turn, and Zone

| CURRENT AS-IS | TARGET | Mapping |
|---|---|---|
| `Table.Id` | `RestaurantTable.TableId` | Preserve where feasible. |
| `Table.ZoneId`, `TableNumber`, `Capacity` | same target semantics | Direct after FK, length, scoped-duplicate, and positive-capacity preflight. |
| no table lifecycle fields | `IsActive`, timestamps, `RowVersion` | DEFAULT, MANUAL-BACKFILL, GENERATED respectively. |
| `Status.Id` | `ReservationStatusId` | Rekey through code crosswalk; preservation is optional and must not encode semantics. |
| `Status.Name` | `ReservationStatus.Name` and reviewed `Code` | Name direct after length check; code is STRUCTURAL-SEED/reviewed transform. |
| no status behavior fields | `BlocksAvailability`, `IsTerminal`, `SortOrder`, `IsActive` | STRUCTURAL-SEED; business review required. |
| `TableLock.Id`, `TableId`, `StartTime`, `EndTime`, `Reason` | corresponding target fields | Preserve/map IDs and direct values after checks. |
| `TableLock.Date` | `LockDate` | Direct rename. |
| no lock actor/state/metadata | `CreatedByUserId`, `IsActive`, timestamps, `RowVersion` | Creator is MANUAL-BACKFILL and required; state/timestamps DEFAULT or MANUAL-BACKFILL; rowversion GENERATED. |
| `Turn.Id` | `TurnId` | Preserve where feasible. |
| `Turn.Name`, `StartTime`, `EndTime`, `IsActive` | same target semantics | Direct after lengths/checks and ambiguity analysis. |
| no turn code/metadata | `Code`, timestamps | Code STRUCTURAL-SEED/MANUAL-BACKFILL; timestamps MANUAL-BACKFILL. |
| `Zone.Id` | `ZoneId` | Preserve where feasible because tables reference it. |
| `Zone.Name` | `Name` | Direct after duplicate/length preflight. |
| `Zone.IsAvailable` | `IsActive` | Candidate semantic transform; verify business equivalence before use. |
| no zone code/metadata | `Code`, `Description`, timestamps, `RowVersion` | Code MANUAL-BACKFILL/STRUCTURAL-SEED; description nullable; timestamps manual; rowversion GENERATED. |

## Identity Merge Problem

`Client` and `User` were created independently and have no cross-reference.

| Evidence | Classification | Reason |
|---|---|---|
| Client `IdCard` versus user | `NO MATCHING EVIDENCE` | `User` has no identification field. |
| Email | `NO MATCHING EVIDENCE` for clients | `Client` has no email field. |
| Name | `PROBABILISTIC / UNSAFE` | Client has first/last fields; user has unstructured full name; names are non-unique and formatting differs. |
| Phone | `NO MATCHING EVIDENCE` for users | `User` has no phone field. |

No deterministic shared key exists in CURRENT AS-IS schema. Migration must create separate `Person` rows for every existing client and user by default. It must not merge solely on names, email-like assumptions, or coincident numeric IDs. Future manual reconciliation may merge profiles only through a controlled, audited process with verified identity evidence.

## Role Migration

Repository-established role evidence is:

- seed creates one `User.Role = "Admin"`;
- `AuthService.RegisterAsync(..., string role)` stores caller-supplied role text without catalog validation;
- login copies the single text value into JWT `ClaimTypes.Role`.

Therefore `Admin` is the only literal role value established by source/seed, but deployed data may contain arbitrary additional strings. Preflight must query distinct values, including case, whitespace, empty strings, and counts. Each reviewed distinct semantic value can become one `Role` and one effective `UserRole` membership per user. This analysis does not freeze canonical role codes or merge differently spelled values.

`Permission` and `RolePermission` require a reviewed initial authorization mapping. Illustrative permissions in TARGET documentation are not mandatory seed values. JWT claims, registration, authorization checks, and role-management behavior must be reconciled in OpenSpec/application design before cutover.

## Status Migration

CURRENT AS-IS model seed is verified as:

| AS-IS ID | Name | Proposed semantic crosswalk, subject to review |
|---:|---|---|
| 1 | `Active` | Reservation status code representing active/confirmed behavior; final code/flags not invented here |
| 2 | `Pending` | Reservation status code representing pending behavior |
| 3 | `Completed` | Reservation status code representing completed/terminal behavior |
| 4 | `Cancelled` | Reservation status code representing cancelled/terminal behavior |

Numeric IDs must not be carried forward as semantic constants. `ReservationService` writes status ID `2`, falls back to cancellation ID `4`, and translates names. `WaitingListService` locates reservation status by name `Active`. `TableService` availability queries count reservations without consistently filtering status, so `BlocksAvailability` migration also changes behavior. Status CRUD currently allows additional or renamed rows; live distinct names and all FK counts require preflight.

CURRENT waiting-list vocabulary evidenced by model/service is `Waiting`, `Assigned`, and `Cancelled`; default/create paths use `Waiting`. `WaitingListService.UpdateAsync` can directly store DTO status without vocabulary validation, so deployed data may contain other strings. Known values can map to reviewed `WaitingListStatus.Code` rows. Unknown, blank, differently cased, or whitespace-padded values are `DATA-CLEANUP` blockers for affected rows, not candidates for silent coercion.

## Zone Preference Transformation

For each non-null `WaitingLists.PreferredZone`:

1. Compare a trimmed, case-normalized value to similarly normalized existing `Zones.Name` values.
2. Accept only exactly one matching zone.
3. Preserve null as null.
4. Flag zero matches as unknown and more than one match as ambiguous.
5. Do not create a `Zone` from unmatched free text automatically.

Conceptual preflight groups normalized zone names to detect duplicate candidates, then left-joins normalized preference values and reports preference value, usage count, and match count. Raw values must remain available for review; normalization is matching logic, not silent source mutation.

## Turn Semantic Migration

CURRENT `Reservation.TurnId` is required. TARGET `AssignedTurnId` is nullable and means explicit assignment/override only. CURRENT services validate that at least one active turn contains an interval but then write hardcoded turn ID `1`; they do not prove that stored `TurnId` is the unique containing turn. The distinction between derived data and intentional override was never recorded.

For every reservation, calculate active matches using:

```text
Turn.StartTime <= Reservation.StartTime
AND Reservation.EndTime <= Turn.EndTime
```

| Result | Safe migration treatment |
|---|---|
| Exactly one derived turn and it equals current `TurnId` | Set `AssignedTurnId = NULL`; current value is redundant derived information. |
| Exactly one derived turn and it differs from current `TurnId` | Candidate explicit override, but require validation before copying current value. |
| No derived turn | Current `TurnId` may become `AssignedTurnId` only after validating referenced turn and business intent. |
| More than one derived turn | Configuration/data blocker; resolve ambiguous active turn containment first. |
| Missing/invalid current FK | Should be impossible under current FK, but deployment preflight must still detect integrity anomalies. |

Do not copy all current values automatically. If intent cannot be recovered, retain evidence in a migration review report and require policy rather than guessing.

## Waiting-List Promotion Migration

CURRENT promotion creates a reservation with copied client/date/time/party data, hardcoded `TurnId = 1`, then deletes the `WaitingListEntry` in the same save operation. TARGET retains the entry in an `ASSIGNED` state and stores unique `Reservation.SourceWaitingListEntryId`.

| Legacy reservation classification | Criterion | Action |
|---|---|---|
| `LINKABLE` | Extant waiting entry and deterministic, reviewed provenance evidence establish one source | Link once, set waiting status to reviewed `ASSIGNED` code |
| `UNLINKABLE` | Known promotion source row was deleted or no candidate survives | Keep source link null |
| `UNKNOWN` | Similar client/date/time/party rows exist but provenance is not unique/proven | Keep source link null; manual review only |

Matching copied values is not proof because independent reservations can share them. Historical deleted entries are not present in schema, snapshot, or repository data and must not be fabricated. `SourceWaitingListEntryId` may remain null for legacy reservations.

## Constraint and Index Impact

| TARGET constraint | Classification | AS-IS risk / preflight |
|---|---|---|
| non-null unique `Person.IdentificationNumber` | `REQUIRES_PREFLIGHT` | Client `IdCard` is currently required and unique, but blanks, lengths, normalization, and future user-person values need review |
| unique `UserAccount.PersonId` | `SAFE_TO_ADD` after generated mapping | One account per newly generated user-person by construction; validate before constraint |
| unique `UserAccount.Username` | `REQUIRES_PREFLIGHT` | AS-IS unique index exists; validate target length/collation behavior |
| unique `ClientProfile.PersonId` | `SAFE_TO_ADD` after generated mapping | One profile per generated client-person by construction |
| unique `Role.Code` | `REQUIRES_BACKFILL` | Codes do not exist; reviewed role crosswalk required |
| unique `Permission.Code` | `REQUIRES_BACKFILL` | No AS-IS permissions |
| unique `Zone.Code` and `Turn.Code` | `REQUIRES_BACKFILL` | No AS-IS code columns |
| `UNIQUE(RestaurantTable.ZoneId, TableNumber)` | `UNKNOWN_UNTIL_DATA_INSPECTION` | No AS-IS uniqueness; find duplicates under target collation/length |
| `CHECK RestaurantTable.Capacity > 0` | `UNKNOWN_UNTIL_DATA_INSPECTION` | Application creation does not validate capacity |
| interval `CHECK StartTime < EndTime` on turn/lock/reservation/waiting | `UNKNOWN_UNTIL_DATA_INSPECTION` | Some create paths validate, update paths do not consistently validate; no DB checks |
| `CHECK Reservation.GuestCount > 0` | `UNKNOWN_UNTIL_DATA_INSPECTION` | Create validates; update does not |
| `CHECK WaitingListEntry.PartySize > 0` | `UNKNOWN_UNTIL_DATA_INSPECTION` | No consistent DB/application enforcement |
| unique non-null `Reservation.SourceWaitingListEntryId` | `SAFE_TO_ADD` while all values null; `REQUIRES_PREFLIGHT` before link backfill | Every proven source may link to at most one reservation |
| status `SortOrder >= 0` | `REQUIRES_BACKFILL` | New structural seed values required |
| `UserAccount.FailedLoginAttempts >= 0` | `SAFE_TO_ADD` with default zero | New column, no legacy source |

Target operational indexes are additive after columns/FKs exist:

- `Reservation(TableId, ReservationDate, ReservationStatusId, StartTime, EndTime)`;
- `TableLock(TableId, LockDate, StartTime, EndTime)`;
- `WaitingListEntry(WaitingListStatusId, RequestedDate, StartTime, EndTime)`;
- `AuditLog(UserId, OccurredAtUtc)`;
- `AuditLog(EntityName, EntityId, OccurredAtUtc)`.

Existing indexes cover individual FKs only. Index creation needs query/load and duplicate-impact review. Interval indexes improve access paths; they do not enforce reservation, lock, or turn interval uniqueness/non-overlap. Exact overlap and ambiguous-turn enforcement needs transactional application/physical design.

## FK and Delete-Behavior Impact

CURRENT AS-IS explicitly uses `DeleteBehavior.Restrict` for `Zone -> Table`, `Client -> Reservation`, `Client -> WaitingListEntry`, `Table -> Reservation`, `Table -> TableLock`, `Status -> Reservation`, and `Turn -> Reservation`. No current FK references `User` or links promotion provenance.

Historical retention makes these TARGET relationships strong `RESTRICT`/`NO ACTION` candidates:

| Relationship | Direction |
|---|---|
| `Person -> UserAccount` | Avoid cascade; deleting identity must not silently delete security account/evidence |
| `Person -> ClientProfile` | Avoid cascade; customer participation and downstream history need explicit handling |
| `ClientProfile -> Reservation` | RESTRICT/NO ACTION candidate; reservations retained |
| `ClientProfile -> WaitingListEntry` | RESTRICT/NO ACTION candidate; entries retained under TARGET |
| `Zone -> RestaurantTable` | RESTRICT/NO ACTION candidate; prefer deactivation |
| `RestaurantTable -> Reservation` | RESTRICT/NO ACTION candidate |
| `RestaurantTable -> TableLock` | RESTRICT/NO ACTION candidate |
| `UserAccount -> AuditLog` | Never cascade audit history; nullable actor FK may need NO ACTION plus account retention/anonymization policy |
| `WaitingListEntry -> Reservation.SourceWaitingListEntryId` | RESTRICT/NO ACTION candidate; deleting source would destroy provenance |

`UserRole.AssignedByUserId` and required created-by references need cycle/multiple-cascade-path analysis. Final physical delete actions remain implementation design, but cascades must not be added casually.

## Initialization and Seed Impact

`Program.cs` calls `Database.Migrate()` and then `SeedData.Initialize(context)`. `SeedData.Initialize` immediately calls `Database.EnsureCreated()`. Mixing migration-based lifecycle with `EnsureCreated()` is unsafe and obscures ownership of schema creation. Migration implementation must first choose one lifecycle; for this repository, migration-managed schema is the evident direction, but no code change is made here.

| Current data source | Classification | Impact |
|---|---|---|
| EF model seed statuses 1-4 | `STRUCTURAL_REFERENCE` | Must migrate by semantic code crosswalk, not stable numeric ID |
| Seed clients Juan/Maria/Carlos | `DEVELOPMENT_DEMO` when seed-created; indistinguishable from user data after persistence | Must not be reset or duplicated in an existing deployment |
| Zones Terrace/VIP/Indoor/Outdoor | `DEVELOPMENT_DEMO` in runtime seed; may become referenced persisted data | Need code backfill and preserve IDs/references |
| Four sample tables | `DEVELOPMENT_DEMO` in runtime seed; may be operational data after persistence | Preserve references; preflight target scoped uniqueness |
| General Turn | `DEVELOPMENT_DEMO` in runtime seed; current code assumes turn ID 1 | Must not rely on generated ID; derive reviewed code |
| Admin user | `DEVELOPMENT_DEMO`/bootstrap account, not frozen structural reference | Handle as security-sensitive USER_DATA once deployed; role maps through reviewed crosswalk |
| All rows created through API or modified after seed | `USER_DATA` | Back up, preflight, map, and verify; never overwrite from demo seed assumptions |

No TARGET demo or structural seed is authorized by this analysis.

## Safe Migration Strategy

### Phase 0 - Preflight and Recovery

- Schema additions: none.
- Data work: full backup; row counts; distinct roles/statuses/preferences; duplicate, length, null, FK, interval, capacity, party-size, and turn-containment reports.
- Compatibility: current application remains unchanged.
- Verification: restore backup in rehearsal environment and reconcile all report counts.
- Rollback: backup restore is primary recovery.
- Coexistence: not applicable yet.

### Phase 1 - Additive Target Foundation

- Schema additions: new identity/RBAC/status/audit tables and nullable/additive transition columns where physical design permits; avoid dropping old columns.
- Data work: insert reviewed structural reference rows only.
- Compatibility: old code must continue using old tables/columns; new constraints remain deferred.
- Verification: empty/new structures, keys, and reference seeds match approved design.
- Rollback: drop only new unreferenced structures or restore rehearsal database; production rollback plan must be scripted before deployment.
- Coexistence: yes.

### Phase 2 - Person/Profile Split and Backfill

- Schema additions: target person/profile/account relationships and mapping support.
- Data work: create separate persons for clients and users; preserve or map profile/account IDs; no automatic client-user merge.
- Compatibility: dual read/write, adapter, or bounded maintenance window needed because old services expect personal fields in `Clients`/`Users`.
- Verification: one mapped person per source row, all client FKs accounted for, credentials byte-for-byte preserved.
- Rollback: retain legacy tables/columns and mapping table until reconciliation completes.
- Coexistence: yes.

### Phase 3 - RBAC Normalization

- Schema additions: role membership structures already additive; audit capture path must exist before revocation semantics change.
- Data work: reviewed role crosswalk and current `UserRole` backfill; optional permission mapping only after approval.
- Compatibility: JWT/authorization code must temporarily understand old text role and new membership or deploy in coordinated maintenance.
- Verification: each user has expected effective role claim(s); no unknown role string silently mapped.
- Rollback: keep `User.Role` until authorization parity and audit behavior are verified.
- Coexistence: yes.

### Phase 4 - Status Normalization

- Schema additions: nullable new reservation/waiting status FKs while old status fields remain.
- Data work: map reservation IDs through semantic codes; map known waiting strings; quarantine unknowns.
- Compatibility: services need code-based behavior and transition reads/writes.
- Verification: all rows mapped, `BlocksAvailability` behavior tested, no magic-ID dependency remains before contract.
- Rollback: retain old `StatusId`/text until parity.
- Coexistence: yes.

### Phase 5 - Reservation and Waiting-List Semantic Transition

- Schema additions: nullable `AssignedTurnId`, nullable unique source link, target metadata columns, preferred-zone FK, target lock shape.
- Data work: per-row turn classification; safe zone matching; provenance links only where proven; creator/timestamp policy backfills.
- Compatibility: new code must retain promoted entries, mark `ASSIGNED`, and use exact turn derivation while legacy columns coexist.
- Verification: zero ambiguous turns, interval checks pass, promotion one-to-zero/one uniqueness holds, availability results are reconciled.
- Rollback: preserve old `TurnId`, preference, status, and tables until semantic reports pass; restore if writes cannot be reversed safely.
- Coexistence: yes.

### Phase 6 - Constraints and Operational Indexes

- Schema additions: reviewed unique/check/FK constraints and target composite indexes.
- Data work: cleanup/backfill must already be complete.
- Compatibility: code must use zone-scoped table identity and stable codes.
- Verification: constraint validation, representative query plans, concurrency/overlap tests, and row-count/FK reconciliation.
- Rollback: index/constraint removal may be possible, but backup/restore remains required for concurrent data changes.
- Coexistence: yes until final verification.

### Phase 7 - Contract and Legacy Cleanup

- Schema changes: remove obsolete tables/columns only after an approved retention window and verified application cutover.
- Data work: archive migration reports and unresolved manual-review records.
- Compatibility: remove dual paths only after all deployed versions stop using legacy schema.
- Verification: production telemetry, audit events, authorization, status behavior, reservation availability, and promotion provenance.
- Rollback: destructive contraction requires tested restore or forward-fix plan; do not contract in same release as initial backfill.
- Coexistence: ends only here.

## Expand / Migrate / Contract Evaluation

`EXPAND -> MIGRATE -> CONTRACT` fits repository evidence because identity, RBAC, statuses, and turn meaning all change while current services bind directly to legacy shapes. EXPAND allows old code to run while target structures are added. MIGRATE supports explicit key maps, reviewed backfills, compatibility reads/writes, and reconciliation. CONTRACT removes legacy structures only after verification. A one-step destructive migration would combine irreversible data interpretation with breaking application changes and is not safe. Exact dual-write mechanics remain implementation/OpenSpec work, not a frozen decision here.

## Decision Gates

### MIGRATION_BLOCKERS (8)

1. Deployment data has not been queried; all constraint, distinct-value, and row-level semantic preflights remain outstanding.
2. `Migrate()` and `EnsureCreated()` are both in startup lifecycle; one migration-owned lifecycle must be established before migration implementation.
3. Client-user identity has no deterministic shared matching key; automatic merge is prohibited.
4. Canonical role crosswalk and initial permission/role-permission policy are not approved.
5. Reservation status behavior flags/codes and handling of non-seeded status rows are not approved.
6. Unknown waiting-list status values and unmatched/ambiguous zone preferences, if present, block affected-row conversion.
7. Turn derivation ambiguity and current `TurnId` intent must be classified per reservation; hardcoded ID 1 cannot be assumed valid.
8. Required target fields without source, especially `TableLock.CreatedByUserId` and historical timestamps, need approved backfill policy.

### DATA_CLEANUP_REQUIRED (10)

1. Normalize/review blank, oversized, or invalid client identification and personal/contact values.
2. Detect target-collation username conflicts and target-length violations.
3. Inventory and normalize role text variants without silently merging meanings.
4. Inventory reservation statuses beyond the four seeded names and reconcile all status FKs.
5. Inventory waiting-list statuses beyond `Waiting`, `Assigned`, and `Cancelled`, including casing/whitespace variants.
6. Resolve unmatched or multiply matched preferred-zone strings and duplicate normalized zone names.
7. Resolve duplicate `(ZoneId, TableNumber)` values and non-positive capacities.
8. Resolve invalid intervals, non-positive guest counts, and non-positive waiting party sizes.
9. Resolve ambiguous active-turn containment and review stored-turn mismatches/no-match reservations.
10. Detect any duplicate proposed promotion source links; leave unproven legacy links null.

### OPEN_BUSINESS_DECISIONS (9)

1. Canonical role codes/names and treatment of each deployed role string.
2. Initial permission catalog and role-permission mapping, if any, for first RBAC release.
3. Reservation status codes plus `BlocksAvailability`, `IsTerminal`, and `SortOrder` values.
4. Waiting-list status codes plus terminal/sort behavior and exact `ASSIGNED` transition policy.
5. Policy for splitting one-field `User.FullName` into required first and last names.
6. Backfill values for required timestamps and `TableLock.CreatedByUserId` when historical actor is unknown.
7. Whether `Zone.IsAvailable` is semantically identical to TARGET `Zone.IsActive`.
8. Review policy for mismatched/no-derived current reservation turns and candidate explicit overrides.
9. Compatibility deployment mechanism: dual read/write, adapter, or maintenance-window cutover.

### OPENSPEC_RECONCILIATION_REQUIRED

- Replace direct role-string registration/JWT behavior with approved normalized authorization semantics.
- Define audit capture policy for role/permission changes without adding authorization-history tables.
- Replace status magic IDs/name branching with stable code behavior.
- Define exact turn derivation and explicit override application behavior.
- Retain promoted waiting entries and write one-way reservation source provenance.
- Define compatibility sequencing and final delete behaviors before implementation.

### SAFE_AUTOMATIC_TRANSFORMS

- Rename/copy date and interval fields only after check preflight.
- Copy client names, phone, and identification to newly generated client-person rows after length/value validation.
- Copy username and password hash to mapped accounts without changing password material.
- Preserve existing surrogate IDs where explicitly selected and validated, using key maps rather than assumption.
- Keep null preferred zone as null and null legacy promotion source as null.
- Generate SQL Server rowversions and use approved defaults for genuinely new state.
- Map exact reviewed status/role/zone crosswalk entries; reject non-crosswalk values.

### MANUAL_REVIEW_REQUIRED

- Any proposed client-user person reconciliation.
- User full-name decomposition.
- Every distinct deployed role or unexpected status value.
- Unknown/ambiguous preferred-zone values.
- Turn mismatch, no-match, or multiple-match reservations.
- Legacy promotion candidates based only on similar copied fields.
- Required actor/timestamp backfills without source evidence.
- Rows violating proposed checks, unique keys, or target lengths/collation.

## Open Questions

Only repository-unresolved questions remain:

- What distinct role, reservation-status, waiting-status, and preferred-zone values exist in each deployment database?
- Do deployed rows violate target lengths, checks, scoped table uniqueness, or exact-one turn containment?
- Which required fallback actor/timestamp values will business owners approve where AS-IS stores no evidence?
- Which canonical role/permission and status behavior mappings will application/OpenSpec owners approve?
- Which compatibility deployment mechanism can supported application versions use?

## Migration Readiness Resolution

This review rebaselines open items at atomic resolution scope. Prior counts of 8 blockers, 10 cleanup items, and 9 business decisions counted several combined bullets. Splitting unrelated concerns changes the register totals without adding target entities or migration scope.

`BLOCKS_IMPLEMENTATION` means the item prevents beginning any migration implementation, including additive schema work. An item may instead block only backfill or contract.

Every `ResolutionSource` value is exactly one of: `CODE_INSPECTION`, `SEED_INSPECTION`, `DATABASE_DATA_INSPECTION`, `BUSINESS_DECISION`, `SECURITY_DECISION`, `MIGRATION_DESIGN`, or `OPENSPEC_RECONCILIATION`.

### Migration Blocker Register

| ID | Open item | ResolutionSource | Schema expand | Data backfill | Contract | Implementation | Status |
|---|---|---|---|---|---|---|---|
| `MB-001` | Deployment-database preflight has not run. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-001 through SQL-020 |
| `MB-002` | Startup mixes `Database.Migrate()` and `EnsureCreated()`; one schema lifecycle is not selected. | `MIGRATION_DESIGN` | YES | YES | YES | YES | OPEN |
| `MB-003` | Client and user rows lack a deterministic shared person key. | `CODE_INSPECTION` | NO | NO | NO | NO | RESOLVED: create separate persons by default; manual reconciliation only |
| `MB-004` | Canonical mapping for deployed `User.Role` values is not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-005` | Initial permission catalog is not approved. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN; does not prevent empty additive permission table |
| `MB-006` | Initial role-permission mapping is not approved. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN; does not prevent reviewed Role/UserRole migration |
| `MB-007` | `ReservationStatus.Code` mapping is not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-008` | `ReservationStatus.BlocksAvailability` values are not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-009` | `ReservationStatus.IsTerminal` values are not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-010` | `ReservationStatus.SortOrder` values are not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-011` | `ReservationStatus.IsActive` values are not approved. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-012` | Unknown deployed waiting-list status values may prevent status backfill. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-004 |
| `MB-013` | Preferred-zone strings with no zone match may prevent zone-FK backfill. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005A |
| `MB-014` | Duplicate normalized zone names may make zone-FK backfill ambiguous. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005B |
| `MB-015` | Active-turn containment ambiguity is not classified for deployed reservations. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-015 and SQL-016 |
| `MB-016` | Intent of current `Reservation.TurnId` mismatches/no-match rows is not recoverable automatically. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN; SQL-016 identifies affected rows but cannot resolve intent |
| `MB-017` | Required `TableLock.CreatedByUserId` has no AS-IS source or approved fallback actor policy. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-018` | Required target timestamps without AS-IS source have no approved backfill timestamp policy. | `MIGRATION_DESIGN` | NO | YES | YES | NO | OPEN |
| `MB-019` | Required `Zone.Code` values have no approved derivation/crosswalk. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MB-020` | Required `Turn.Code` values have no approved derivation/crosswalk. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |

### Data Cleanup Register

| ID | Open item | ResolutionSource | Schema expand | Data backfill | Contract | Implementation | Status / query |
|---|---|---|---|---|---|---|---|
| `DC-001` | Null or blank client identification values. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-002` | Blank or target-length-invalid client first names. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-003` | Blank or target-length-invalid client last names. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-004` | Target-length-invalid client phone values. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-005` | Null or blank usernames. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-007 |
| `DC-006` | Distinct role text variants may represent duplicate or different meanings. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-001 |
| `DC-007` | Reservation status rows outside reviewed crosswalk. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-002 and SQL-003 |
| `DC-008` | Waiting-list status values outside `Waiting`, `Assigned`, and `Cancelled`. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-004 |
| `DC-009` | Preferred-zone strings with no normalized zone-name match. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005A |
| `DC-010` | Duplicate normalized zone names make preference matching ambiguous. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005B |
| `DC-011` | Duplicate target business keys `(ZoneId, TableNumber)`. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-012 |
| `DC-012` | Non-positive table capacities. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-009 |
| `DC-013` | Invalid reservation intervals. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-010 |
| `DC-014` | Non-positive reservation guest counts. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-010 |
| `DC-015` | Invalid waiting-list intervals. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-011 |
| `DC-016` | Non-positive waiting-list party sizes. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-011 |
| `DC-017` | Invalid table-lock intervals. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-019 |
| `DC-018` | Invalid turn intervals. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-014 |
| `DC-019` | Ambiguous active-turn containment. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-015 and SQL-016 |
| `DC-020` | Stored reservation turn differs from the sole derived turn. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `DC-021` | More than one reservation is similar to one extant waiting-list source candidate. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-018B exposes conflicts but does not prove provenance |
| `DC-022` | `Users.PasswordHash` exceeds TARGET length 255. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-023` | Deployed role text exceeds TARGET role-code/name lengths. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-024` | `Statuses.Name` exceeds TARGET reservation-status name length 100. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-025` | `Zones.Name` exceeds TARGET length 100. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-026` | `Tables.TableNumber` exceeds TARGET length 20. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-027` | `TableLocks.Reason` exceeds TARGET length 300. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `DC-028` | Duplicate client identification values. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-029` | Client identification exceeds TARGET length 50. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `DC-030` | Username exceeds TARGET length 100. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-007 |
| `DC-031` | Trimmed/case-normalized username conflict under current database collation. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-007 |
| `DC-032` | Reservation has no active derived turn match. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `DC-033` | User email exceeds TARGET `Person.Email` length 254. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-008 |
| `DC-034` | `Turns.Name` exceeds TARGET length 100. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-014 |

### Business Decision Register

| ID | Open item | ResolutionSource | Schema expand | Data backfill | Contract | Implementation | Status |
|---|---|---|---|---|---|---|---|
| `BD-001` | Canonical role codes/names and treatment of each deployed role string. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-002` | Initial permission catalog. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-003` | Initial role-permission mapping. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-004` | Reservation-status codes. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-005` | Reservation-status `BlocksAvailability` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-006` | Reservation-status `IsTerminal` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-007` | Reservation-status `SortOrder` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-008` | Reservation-status `IsActive` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-009` | Waiting-list status codes. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-010` | Waiting-list status `IsTerminal` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-011` | Waiting-list status `SortOrder` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-012` | Waiting-list status `IsActive` values. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-013` | Exact waiting-list `ASSIGNED` transition policy. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-014` | Required first/last-name backfill for one-field `User.FullName`. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-015` | Backfill timestamps where AS-IS stores no creation/update evidence. | `MIGRATION_DESIGN` | NO | YES | YES | NO | OPEN |
| `BD-016` | Fallback actor for required `TableLock.CreatedByUserId`. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-017` | Whether AS-IS `Zone.IsAvailable` is semantically equivalent to TARGET `Zone.IsActive`. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-018` | Treatment when current turn equals sole derived turn. | `MIGRATION_DESIGN` | NO | NO | NO | NO | RESOLVED: `AssignedTurnId = NULL` |
| `BD-019` | Compatibility deployment mechanism: dual path, adapter, or maintenance window. | `MIGRATION_DESIGN` | NO | YES | YES | NO | OPEN |
| `BD-020` | Stable `Zone.Code` derivation/crosswalk. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-021` | Stable `Turn.Code` derivation/crosswalk. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `BD-022` | Treatment when current turn differs from sole derived turn. | `MIGRATION_DESIGN` | NO | NO | NO | NO | RESOLVED: explicit-override candidate requiring review |
| `BD-023` | Treatment when no active turn derives. | `MIGRATION_DESIGN` | NO | NO | NO | NO | RESOLVED: validate before considering current turn explicit |
| `BD-024` | Treatment when multiple active turns derive. | `MIGRATION_DESIGN` | NO | NO | NO | NO | RESOLVED: stop as ambiguous configuration |
| `BD-025` | Treatment when turn classification is unknown. | `MIGRATION_DESIGN` | NO | NO | NO | NO | RESOLVED: stop affected-row backfill and investigate |
| `BD-026` | Per-row intent adjudication for turn mismatch or no-derived-match reservations. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN; policy is resolved, but AS-IS data cannot prove intent |

### Manual Review Register

| ID | Open item | ResolutionSource | Schema expand | Data backfill | Contract | Implementation | Status / query |
|---|---|---|---|---|---|---|---|
| `MR-001` | Any proposed client-user person reconciliation. | `BUSINESS_DECISION` | NO | NO | NO | NO | Manual, controlled, and outside automatic backfill |
| `MR-002` | Decomposition of `User.FullName`. | `BUSINESS_DECISION` | NO | YES | YES | NO | OPEN |
| `MR-003` | Every deployed role value not already reviewed. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-001 |
| `MR-004` | Unexpected reservation status row or name. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-002 and SQL-003 |
| `MR-005` | Unexpected waiting-list status string. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-004 |
| `MR-006` | Unknown preferred-zone string. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005A |
| `MR-007` | Ambiguous preferred-zone string. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005A identifies used values and SQL-005B identifies duplicate normalized zone names |
| `MR-008` | Reservation classified as turn mismatch. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `MR-009` | Reservation classified with no derived turn match. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `MR-010` | Reservation classified with ambiguous derived turn. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `MR-011` | Reservation turn classification is unknown. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-016 |
| `MR-012` | Legacy promotion candidate based only on similar copied fields. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-018A; similarity is not proof |
| `MR-013` | Required actor backfill without source evidence. | `SECURITY_DECISION` | NO | YES | YES | NO | OPEN |
| `MR-014` | Required timestamp backfill without source evidence. | `MIGRATION_DESIGN` | NO | YES | YES | NO | OPEN |
| `MR-015` | Row violating a target check constraint. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-009 through SQL-011, SQL-014, and SQL-019 |
| `MR-016` | Row violating a target uniqueness expectation. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-005B, SQL-006, SQL-007, and SQL-012 |
| `MR-017` | Client identification exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `MR-018` | Current FK orphan candidate. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-013 |
| `MR-019` | Client first name exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `MR-020` | Client last name exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `MR-021` | Client phone exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-006 |
| `MR-022` | Username exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-007 |
| `MR-023` | User email exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-008 |
| `MR-024` | Password hash exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-025` | Role text exceeding target role-code/name length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-026` | Reservation status name exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-027` | Zone name exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-028` | Table number exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-029` | Table-lock reason exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-020 |
| `MR-030` | Turn name exceeding target length. | `DATABASE_DATA_INSPECTION` | NO | YES | YES | NO | OPEN; SQL-014 |

### OpenSpec Reconciliation Register

| ID | Open item | ResolutionSource | Schema expand | Data backfill | Contract | Implementation | Status |
|---|---|---|---|---|---|---|---|
| `OR-001` | Replace arbitrary role-string registration with normalized role assignment. | `OPENSPEC_RECONCILIATION` | NO | NO | YES | NO | READY FOR RECONCILIATION |
| `OR-002` | Replace single role-string JWT behavior with normalized authorization claims/policy. | `OPENSPEC_RECONCILIATION` | NO | NO | YES | NO | READY FOR RECONCILIATION |
| `OR-003` | Define `AuditLog` capture policy for role/permission grants and revocations. | `OPENSPEC_RECONCILIATION` | NO | NO | YES | NO | READY FOR RECONCILIATION |
| `OR-004` | Replace reservation magic IDs/name branching with stable reservation-status code behavior. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY after BD-004 through BD-008 |
| `OR-005` | Replace waiting-list status strings with stable waiting-list status code behavior. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY after BD-009 through BD-013 |
| `OR-006` | Implement exact DBD-007 derivation and explicit override behavior. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY FOR RECONCILIATION |
| `OR-007` | Retain a waiting-list entry after promotion. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY FOR RECONCILIATION |
| `OR-008` | Set approved `ASSIGNED` waiting-list status during promotion. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY after BD-013 |
| `OR-009` | Persist `Reservation.SourceWaitingListEntryId` only for proven promotion provenance. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY FOR RECONCILIATION |
| `OR-010` | Define expand/migrate/contract application compatibility sequencing. | `OPENSPEC_RECONCILIATION` | NO | YES | YES | NO | READY; final mechanism remains BD-019 |
| `OR-011` | Reconcile target delete behavior with historical retention and service delete operations. | `OPENSPEC_RECONCILIATION` | NO | NO | YES | NO | READY FOR RECONCILIATION |

### Schema Blockers vs Data Blockers

- Additive schema foundation is blocked by `MB-002` only. Empty target tables, nullable transition columns, and non-enforcing indexes can be designed without deployed-data cleanup or final catalog rows.
- `MB-001`, `MB-004` through `MB-020`, all `DC-*`, and applicable `BD-*` do not prevent schema expansion. They prevent affected backfills, enforcement, or legacy removal.
- Constraints and non-null conversions must wait for successful preflight/backfill. Operational indexes can be additive when their columns exist, but they do not validate business semantics or prevent interval overlap.
- Contract is blocked by every unresolved blocker/cleanup item affecting converted data plus unresolved compatibility/OpenSpec items. No legacy field/table should be removed merely because target structures exist.

### Read-Only SQL Server Validation Plan

All statements below are `SELECT`-only and use CURRENT AS-IS table/column names from the EF snapshot. Run against each deployment database under a read-only principal where possible. Result interpretation, not query execution, resolves data items.

#### SQL-001 - Distinct User Role Values

```sql
SELECT
    Role,
    LTRIM(RTRIM(Role)) AS NormalizedRole,
    COUNT_BIG(*) AS UserCount
FROM dbo.Users
GROUP BY Role, LTRIM(RTRIM(Role))
ORDER BY NormalizedRole, Role;
```

#### SQL-002 - Reservation Status References

```sql
SELECT
    r.StatusId,
    s.Name AS StatusName,
    COUNT_BIG(*) AS ReservationCount
FROM dbo.Reservations AS r
LEFT JOIN dbo.Statuses AS s ON s.Id = r.StatusId
GROUP BY r.StatusId, s.Name
ORDER BY r.StatusId;
```

#### SQL-003 - Status Catalog and Usage

```sql
SELECT
    s.Id,
    s.Name,
    COUNT_BIG(r.Id) AS ReservationCount
FROM dbo.Statuses AS s
LEFT JOIN dbo.Reservations AS r ON r.StatusId = s.Id
GROUP BY s.Id, s.Name
ORDER BY s.Id;
```

#### SQL-004 - Waiting-List Status Values

```sql
SELECT
    Status,
    LTRIM(RTRIM(Status)) AS NormalizedStatus,
    COUNT_BIG(*) AS EntryCount
FROM dbo.WaitingLists
GROUP BY Status, LTRIM(RTRIM(Status))
ORDER BY NormalizedStatus, Status;
```

#### SQL-005A - Preferred-Zone Match Cardinality

```sql
WITH NormalizedZones AS
(
    SELECT
        z.Id,
        z.Name,
        LOWER(LTRIM(RTRIM(z.Name))) AS NormalizedName
    FROM dbo.Zones AS z
),
PreferenceValues AS
(
    SELECT
        w.PreferredZone,
        LOWER(LTRIM(RTRIM(w.PreferredZone))) AS NormalizedPreference,
        COUNT_BIG(*) AS EntryCount
    FROM dbo.WaitingLists AS w
    WHERE w.PreferredZone IS NOT NULL
    GROUP BY w.PreferredZone, LOWER(LTRIM(RTRIM(w.PreferredZone)))
)
SELECT
    p.PreferredZone,
    p.NormalizedPreference,
    p.EntryCount,
    COUNT(nz.Id) AS MatchingZoneCount,
    MIN(nz.Id) AS SoleCandidateZoneId
FROM PreferenceValues AS p
LEFT JOIN NormalizedZones AS nz ON nz.NormalizedName = p.NormalizedPreference
GROUP BY p.PreferredZone, p.NormalizedPreference, p.EntryCount
ORDER BY p.PreferredZone;
```

#### SQL-005B - Duplicate Normalized Zone Names

```sql
SELECT
    LOWER(LTRIM(RTRIM(z.Name))) AS NormalizedName,
    COUNT_BIG(*) AS ZoneCount,
    MIN(z.Id) AS FirstZoneId,
    MAX(z.Id) AS LastZoneId
FROM dbo.Zones AS z
GROUP BY LOWER(LTRIM(RTRIM(z.Name)))
HAVING COUNT_BIG(*) > 1
ORDER BY NormalizedName;
```

#### SQL-006 - Client Identifier and Required Personal-Value Anomalies

```sql
SELECT c.Id, c.IdCard, c.FirstName, c.LastName, c.PhoneNumber
FROM dbo.Clients AS c
WHERE c.IdCard IS NULL
   OR LTRIM(RTRIM(c.IdCard)) = ''
   OR DATALENGTH(c.IdCard) / 2 > 50
   OR c.FirstName IS NULL OR LTRIM(RTRIM(c.FirstName)) = '' OR DATALENGTH(c.FirstName) / 2 > 100
   OR c.LastName IS NULL OR LTRIM(RTRIM(c.LastName)) = '' OR DATALENGTH(c.LastName) / 2 > 100
   OR DATALENGTH(c.PhoneNumber) / 2 > 30
   OR EXISTS
      (
          SELECT 1
          FROM dbo.Clients AS c2
          WHERE c2.Id <> c.Id
            AND c2.IdCard = c.IdCard
      )
ORDER BY c.Id;
```

#### SQL-007 - Username Anomalies

```sql
SELECT u.Id, u.Username
FROM dbo.Users AS u
WHERE u.Username IS NULL
   OR LTRIM(RTRIM(u.Username)) = ''
   OR DATALENGTH(u.Username) / 2 > 100
   OR EXISTS
      (
          SELECT 1
          FROM dbo.Users AS u2
          WHERE u2.Id <> u.Id
            AND LOWER(LTRIM(RTRIM(u2.Username))) = LOWER(LTRIM(RTRIM(u.Username)))
      )
ORDER BY u.Id;
```

Normalized comparison uses current database collation. Physical migration design must rerun uniqueness preflight with an explicit collation if TARGET uses a different collation; this query does not claim an unspecified TARGET collation.

#### SQL-008 - User Email Anomalies Relevant to Person Backfill

```sql
SELECT u.Id, u.Email
FROM dbo.Users AS u
WHERE u.Email IS NULL
   OR LTRIM(RTRIM(u.Email)) = ''
   OR DATALENGTH(u.Email) / 2 > 254
   OR EXISTS
      (
          SELECT 1
          FROM dbo.Users AS u2
          WHERE u2.Id <> u.Id
            AND LOWER(LTRIM(RTRIM(u2.Email))) = LOWER(LTRIM(RTRIM(u.Email)))
      )
ORDER BY u.Id;
```

TARGET permits duplicate/null contact email, so duplicates reported here are review evidence, not automatic merge evidence or necessarily cleanup failures.

#### SQL-009 - Invalid Table Capacities

```sql
SELECT Id, TableNumber, ZoneId, Capacity
FROM dbo.Tables
WHERE Capacity <= 0
ORDER BY Id;
```

#### SQL-010 - Invalid Reservation Intervals or Guest Counts

```sql
SELECT Id, Date, StartTime, EndTime, GuestCount, ClientId, TableId, StatusId, TurnId
FROM dbo.Reservations
WHERE StartTime >= EndTime
   OR GuestCount <= 0
ORDER BY Id;
```

#### SQL-011 - Invalid Waiting-List Intervals or Party Sizes

```sql
SELECT Id, Date, StartTime, EndTime, PartySize, ClientId, Status, PreferredZone
FROM dbo.WaitingLists
WHERE StartTime >= EndTime
   OR PartySize <= 0
ORDER BY Id;
```

#### SQL-012 - Duplicate Target Table Business Keys

```sql
SELECT
    ZoneId,
    TableNumber,
    COUNT_BIG(*) AS DuplicateCount,
    MIN(Id) AS FirstTableId,
    MAX(Id) AS LastTableId
FROM dbo.Tables
GROUP BY ZoneId, TableNumber
HAVING COUNT_BIG(*) > 1
ORDER BY ZoneId, TableNumber;
```

#### SQL-013 - Current FK Orphan Candidates

```sql
SELECT 'Reservations.ClientId' AS Relationship, r.Id AS DependentId, r.ClientId AS MissingPrincipalId
FROM dbo.Reservations AS r LEFT JOIN dbo.Clients AS c ON c.Id = r.ClientId WHERE c.Id IS NULL
UNION ALL
SELECT 'Reservations.TableId', r.Id, r.TableId
FROM dbo.Reservations AS r LEFT JOIN dbo.Tables AS t ON t.Id = r.TableId WHERE t.Id IS NULL
UNION ALL
SELECT 'Reservations.StatusId', r.Id, r.StatusId
FROM dbo.Reservations AS r LEFT JOIN dbo.Statuses AS s ON s.Id = r.StatusId WHERE s.Id IS NULL
UNION ALL
SELECT 'Reservations.TurnId', r.Id, r.TurnId
FROM dbo.Reservations AS r LEFT JOIN dbo.Turns AS t ON t.Id = r.TurnId WHERE t.Id IS NULL
UNION ALL
SELECT 'WaitingLists.ClientId', w.Id, w.ClientId
FROM dbo.WaitingLists AS w LEFT JOIN dbo.Clients AS c ON c.Id = w.ClientId WHERE c.Id IS NULL
UNION ALL
SELECT 'Tables.ZoneId', t.Id, t.ZoneId
FROM dbo.Tables AS t LEFT JOIN dbo.Zones AS z ON z.Id = t.ZoneId WHERE z.Id IS NULL
UNION ALL
SELECT 'TableLocks.TableId', l.Id, l.TableId
FROM dbo.TableLocks AS l LEFT JOIN dbo.Tables AS t ON t.Id = l.TableId WHERE t.Id IS NULL;
```

#### SQL-014 - Current Turn Definitions and Invalid Ranges

```sql
SELECT
    Id,
    Name,
    StartTime,
    EndTime,
    IsActive,
    CASE
        WHEN StartTime >= EndTime THEN 'INVALID_INTERVAL'
        WHEN Name IS NULL OR LTRIM(RTRIM(Name)) = '' THEN 'INVALID_NAME'
        WHEN DATALENGTH(Name) / 2 > 100 THEN 'TARGET_NAME_TOO_LONG'
        ELSE 'VALID_SHAPE'
    END AS ValidationResult
FROM dbo.Turns
ORDER BY IsActive DESC, StartTime, Id;
```

#### SQL-015 - Overlapping Active Turn Definitions

```sql
SELECT
    t1.Id AS Turn1Id,
    t1.Name AS Turn1Name,
    t1.StartTime AS Turn1Start,
    t1.EndTime AS Turn1End,
    t2.Id AS Turn2Id,
    t2.Name AS Turn2Name,
    t2.StartTime AS Turn2Start,
    t2.EndTime AS Turn2End
FROM dbo.Turns AS t1
JOIN dbo.Turns AS t2
  ON t1.Id < t2.Id
 AND t1.IsActive = 1
 AND t2.IsActive = 1
 AND t1.StartTime < t2.EndTime
 AND t2.StartTime < t1.EndTime
ORDER BY t1.Id, t2.Id;
```

Overlap is a warning superset. DBD-007 invalidity is specifically more than one active turn containing the same reservation interval; SQL-016 performs that row-level test.

#### SQL-016 - Reservation Turn Classification Under DBD-007

```sql
SELECT
    r.Id AS ReservationId,
    r.Date,
    r.StartTime,
    r.EndTime,
    r.TurnId AS CurrentTurnId,
    d.MatchCount,
    d.SoleDerivedTurnId,
    CASE
        WHEN d.MatchCount > 1 THEN 'AMBIGUOUS'
        WHEN d.MatchCount = 0 THEN 'NO_DERIVED_MATCH'
        WHEN d.MatchCount = 1 AND d.SoleDerivedTurnId = r.TurnId THEN 'DERIVED_REDUNDANT'
        WHEN d.MatchCount = 1 AND d.SoleDerivedTurnId <> r.TurnId THEN 'EXPLICIT_OVERRIDE_CANDIDATE'
        ELSE 'UNKNOWN'
    END AS TurnMigrationClassification
FROM dbo.Reservations AS r
OUTER APPLY
(
    SELECT
        COUNT_BIG(*) AS MatchCount,
        MIN(t.Id) AS SoleDerivedTurnId
    FROM dbo.Turns AS t
    WHERE t.IsActive = 1
      AND t.StartTime <= r.StartTime
      AND r.EndTime <= t.EndTime
) AS d
ORDER BY r.Id;
```

#### SQL-017 - Name-Only Client/User Match Candidates (Unsafe Evidence)

```sql
SELECT
    c.Id AS ClientId,
    c.FirstName,
    c.LastName,
    u.Id AS UserId,
    u.FullName,
    'PROBABILISTIC / UNSAFE' AS MatchClassification
FROM dbo.Clients AS c
JOIN dbo.Users AS u
  ON LOWER(LTRIM(RTRIM(CONCAT(c.FirstName, ' ', c.LastName))))
   = LOWER(LTRIM(RTRIM(u.FullName)))
ORDER BY c.Id, u.Id;
```

Rows returned by SQL-017 must not be auto-merged.

#### SQL-018A - Extant Waiting/Reservation Similarity Candidates (Not Proven Provenance)

```sql
SELECT
    r.Id AS ReservationId,
    COUNT_BIG(w.Id) AS SimilarWaitingEntryCount,
    MIN(w.Id) AS SoleCandidateWaitingEntryId
FROM dbo.Reservations AS r
LEFT JOIN dbo.WaitingLists AS w
  ON w.ClientId = r.ClientId
 AND w.Date = r.Date
 AND w.StartTime = r.StartTime
 AND w.EndTime = r.EndTime
 AND w.PartySize = r.GuestCount
GROUP BY r.Id
HAVING COUNT_BIG(w.Id) > 0
ORDER BY r.Id;
```

#### SQL-018B - One Waiting Candidate Similar to Multiple Reservations

```sql
WITH SimilarPairs AS
(
    SELECT w.Id AS WaitingListEntryId, r.Id AS ReservationId
    FROM dbo.WaitingLists AS w
    JOIN dbo.Reservations AS r
      ON r.ClientId = w.ClientId
     AND r.Date = w.Date
     AND r.StartTime = w.StartTime
     AND r.EndTime = w.EndTime
     AND r.GuestCount = w.PartySize
)
SELECT
    WaitingListEntryId,
    COUNT_BIG(*) AS SimilarReservationCount,
    MIN(ReservationId) AS FirstReservationId,
    MAX(ReservationId) AS LastReservationId
FROM SimilarPairs
GROUP BY WaitingListEntryId
HAVING COUNT_BIG(*) > 1
ORDER BY WaitingListEntryId;
```

SQL-018A and SQL-018B expose similarity only. Similarity does not establish promotion provenance, and no link may be backfilled without stronger deterministic evidence.

#### SQL-019 - Invalid Table-Lock Intervals

```sql
SELECT Id, Date, StartTime, EndTime, TableId, Reason
FROM dbo.TableLocks
WHERE StartTime >= EndTime
ORDER BY Id;
```

#### SQL-020 - Remaining Target-Length Anomalies

```sql
SELECT 'Users' AS ObjectName, u.Id AS RowId, 'PasswordHash' AS ColumnName,
       DATALENGTH(u.PasswordHash) / 2 AS ActualLength, 255 AS TargetMaximum,
       CONVERT(nvarchar(max), u.PasswordHash) AS CurrentValue
FROM dbo.Users AS u WHERE DATALENGTH(u.PasswordHash) / 2 > 255
UNION ALL
SELECT 'Users', u.Id, 'Role', DATALENGTH(u.Role) / 2, 80, CONVERT(nvarchar(max), u.Role)
FROM dbo.Users AS u WHERE DATALENGTH(u.Role) / 2 > 80
UNION ALL
SELECT 'Statuses', s.Id, 'Name', DATALENGTH(s.Name) / 2, 100, CONVERT(nvarchar(max), s.Name)
FROM dbo.Statuses AS s WHERE DATALENGTH(s.Name) / 2 > 100
UNION ALL
SELECT 'Zones', z.Id, 'Name', DATALENGTH(z.Name) / 2, 100, CONVERT(nvarchar(max), z.Name)
FROM dbo.Zones AS z WHERE DATALENGTH(z.Name) / 2 > 100
UNION ALL
SELECT 'Tables', t.Id, 'TableNumber', DATALENGTH(t.TableNumber) / 2, 20, CONVERT(nvarchar(max), t.TableNumber)
FROM dbo.Tables AS t WHERE DATALENGTH(t.TableNumber) / 2 > 20
UNION ALL
SELECT 'TableLocks', l.Id, 'Reason', DATALENGTH(l.Reason) / 2, 300, CONVERT(nvarchar(max), l.Reason)
FROM dbo.TableLocks AS l WHERE DATALENGTH(l.Reason) / 2 > 300
ORDER BY ObjectName, RowId, ColumnName;
```

### Person Merge Policy

CURRENT AS-IS provides no shared deterministic client-user identifier. `Client.IdCard` and phone exist only on `Clients`; email exists only on `Users`; names use incompatible structured/unstructured shapes; numeric IDs are unrelated surrogates.

| Case | Evidence classification | Migration policy |
|---|---|---|
| Client only | `NO MATCHING EVIDENCE` to a user | Generate one `Person`, then one `ClientProfile`; retain explicit source-key mapping. |
| User only | `NO MATCHING EVIDENCE` to a client | Generate one separate `Person`, then one `UserAccount`; retain explicit source-key mapping. |
| Client + User deterministic same person | Not establishable from current schema alone | Merge only when external/manual verified identity evidence is supplied through a controlled process; no repository field pair currently qualifies. |
| Client + User possible but unsafe match | `PROBABILISTIC / UNSAFE`, such as normalized name equality | Generate separate persons; flag candidate for future manual reconciliation. |
| Ambiguous duplicates | Multiple possible matches or conflicting evidence | Generate separate persons; quarantine reconciliation decision; never choose by first match. |

`IdentificationNumber` can deterministically identify client-person rows among current clients because `Clients.IdCard` is uniquely indexed, subject to data preflight, but cannot link a client to a user because `Users` has no identification value. `Users.Email` cannot link to clients because clients have no email. Name similarity is never sufficient.

### Role and Authorization Readiness

Repository-established role values are limited to:

| Value | Evidence | Readiness |
|---|---|---|
| `Admin` | `SeedData` assigns it to seeded admin user | Observed source value; target code/name still requires BD-001 |
| Any other string | `AuthService.RegisterAsync` accepts and persists caller-provided `role` without catalog validation | Unknown until SQL-001; each value requires review |

Migration A, current `User.Role` to `Role` plus effective `UserRole`, can proceed before Migration B, final `Permission` plus `RolePermission` mapping, provided SQL-001 and BD-001 produce an approved role crosswalk. Empty additive permission tables may exist before their authorization catalog is approved. Application authorization contract and removal of `Users.Role` must wait for BD-002, BD-003, and OR-001 through OR-003; no permissions are invented here.

### Status Readiness

#### ReservationStatus

- Seed evidence proves AS-IS rows `1 Active`, `2 Pending`, `3 Completed`, and `4 Cancelled` are model-managed defaults.
- Source code contains magic `StatusId = 2` on reservation creation and fallback cancellation ID `4`; waiting-list promotion looks up reservation status name `Active`.
- `ReservationService` compares/translates names; `StatusService` permits additional mutable status rows; `TableService` does not consistently apply status semantics to availability.
- No exact TARGET codes, `BlocksAvailability`, `IsTerminal`, `SortOrder`, or `IsActive` values are frozen by repository evidence. Names provide semantic candidates only; BD-004 through BD-008 must approve each mapping.
- TARGET numeric IDs are generated implementation details and must never be mapped by assuming IDs 1-4 remain stable.

#### WaitingListStatus

- Model/default/create evidence proves `Waiting`; service dictionaries also establish `Assigned` and `Cancelled` as recognized strings.
- `WaitingListService.UpdateAsync` can persist an arbitrary DTO status directly, so deployed values remain unknown until SQL-004.
- Exact TARGET codes, `IsTerminal`, `SortOrder`, and `IsActive` values require BD-009 through BD-012. Exact `ASSIGNED` transition policy requires BD-013. Known source strings may be crosswalk inputs after approval; unknown values are not coerced.

### Turn Readiness

CURRENT `Turns` already contains `StartTime`, `EndTime`, and `IsActive`, so DBD-007 containment can be evaluated against current reservation intervals without TARGET `Turn.Code`, timestamps, or other metadata. Missing TARGET metadata blocks catalog backfill, not derivation validation; `Turn.Code` remains BD-021. Current data cannot prove whether a differing stored `TurnId` was intentionally assigned because AS-IS stores no assignment mode or audit evidence.

| Classification | Current-data test | Migration treatment |
|---|---|---|
| `DERIVED_REDUNDANT` | Exactly one active turn contains interval and equals current `TurnId` | Target `AssignedTurnId = NULL` |
| `EXPLICIT_OVERRIDE_CANDIDATE` | Exactly one active turn contains interval but differs from current `TurnId` | Manual validation; candidate only, not proof |
| `NO_DERIVED_MATCH` | Zero active turns contain interval | Validate business intent before considering current `TurnId` as explicit assignment |
| `AMBIGUOUS` | More than one active turn contains interval | Data/configuration blocker; resolve active-turn definitions |
| `UNKNOWN` | Classification cannot be established because of unexpected integrity/result condition | Stop affected-row backfill and investigate |

SQL-016 computes these labels without mutation. Broad turn overlap from SQL-015 is not automatically fatal unless it creates ambiguous containment for a reservation or violates approved future configuration policy.

### Waiting-List Readiness

| AS-IS case | Recoverability | Migration rule |
|---|---|---|
| Existing waiting row | Row can be migrated | Preserve its identity when feasible; map status and preferred zone through approved crosswalks; retain row. |
| Already promoted and deleted row | Not recoverable from current relational data | Do not recreate row and do not manufacture `SourceWaitingListEntryId`. |
| Legacy reservation with no provable source | Provenance unknown/unlinkable | Leave `Reservation.SourceWaitingListEntryId = NULL`. |
| Extant row with similar reservation fields | Candidate only | Similarity is insufficient; link only with stronger deterministic evidence and enforce one source to at most one reservation. |

Future promotion behavior requires OR-007 through OR-009: retain entry, set approved `ASSIGNED` status, and store only proven unique source provenance.

### Phase Readiness

| Phase | Status | Blocking unresolved IDs | Readiness basis |
|---|---|---|---|
| Phase 0 - preflight/backup | `CONDITIONALLY_READY` | `MB-001` | SQL plan is complete; database access, execution, backup, and restore rehearsal remain. |
| Phase 1 - additive target foundation | `BLOCKED` | `MB-002` | Additive shape is known; one schema lifecycle must be selected before migration generation/implementation. |
| Phase 2 - Person/profile split | `CONDITIONALLY_READY` | `MB-001`, `MB-018`, `BD-014`, `BD-015`, `DC-001` through `DC-005`, `DC-022`, `DC-028` through `DC-031`, `DC-033`, `MR-002`, `MR-014`, `MR-017`, `MR-019` through `MR-024` | Separate-person policy resolves identity merge; values and required name/timestamps remain. |
| Phase 3 - RBAC normalization | `CONDITIONALLY_READY` | `MB-001`, `MB-004` through `MB-006`, `BD-001` through `BD-003`, `DC-006`, `DC-023`, `MR-003`, `MR-025`, `OR-001` through `OR-003` | Role/UserRole can precede permission mapping, but crosswalk and authorization cutover remain. |
| Phase 4 - status normalization | `BLOCKED` | `MB-001`, `MB-007` through `MB-012`, `BD-004` through `BD-012`, `DC-007`, `DC-008`, `DC-024`, `MR-004`, `MR-005`, `MR-026`, `OR-004`, `OR-005` | Both status domains need approved independent mappings and deployed-value inventory. |
| Phase 5 - reservation/waiting semantic transition | `BLOCKED` | `MB-001`, `MB-013` through `MB-020`, `BD-013`, `BD-015` through `BD-017`, `BD-019` through `BD-021`, `BD-026`, `DC-009` through `DC-021`, `DC-025` through `DC-027`, `DC-032`, `DC-034`, `MR-006` through `MR-014`, `MR-027` through `MR-030`, `OR-006` through `OR-010` | Turn, provenance, zone, codes, actor, timestamp, and compatibility work remains. |
| Phase 6 - constraints/indexes | `BLOCKED` | `MB-001`, all unresolved `DC-*`, `MR-015` through `MR-030` | Constraints require clean/backfilled data; indexes alone do not satisfy semantic gates. |
| Phase 7 - contract/legacy removal | `BLOCKED` | all unresolved `MB-*`, `DC-*`, `BD-*`, `MR-*`, and `OR-*` that block contract | Contract waits for verified backfill, compatibility retirement, retention, and OpenSpec/application reconciliation. |

### Readiness Gate

Atomic rebaselining yields these review totals:

```text
MIGRATION_ANALYSIS_STATUS = COMPLETE
MIGRATION_READINESS_STATUS = BLOCKED

MIGRATION_BLOCKERS_TOTAL = 20
MIGRATION_BLOCKERS_RESOLVED = 1
MIGRATION_BLOCKERS_REMAINING = 19

DATA_CLEANUP_TOTAL = 34
DATA_CLEANUP_RESOLVED_BY_DESIGN = 0
DATA_CLEANUP_REQUIRING_DATABASE_INSPECTION = 34

BUSINESS_DECISIONS_TOTAL = 26
BUSINESS_DECISIONS_RESOLVED = 5
BUSINESS_DECISIONS_REMAINING = 21

MANUAL_REVIEW_TOTAL = 30
OPENSPEC_RECONCILIATION_TOTAL = 11

SCHEMA_EXPAND_READY = NO
BACKFILL_READY = NO
CONTRACT_READY = NO

READ_ONLY_SQL_QUERIES_PREPARED = 22
OPENSPEC_RECONCILIATION_READY = YES

PRODUCTION_CODE_CHANGED = NO
MIGRATIONS_CHANGED = NO
OPENSPEC_CHANGED = NO

MIGRATION_IMPLEMENTATION = NOT_STARTED
EF_CORE_MIGRATIONS_GENERATED = NO
PRODUCTION_SCHEMA_CHANGED = NO
```

`SCHEMA_EXPAND_READY = NO` is caused only by `MB-002`; deployed-data cleanup does not itself prevent additive schema design. `OPENSPEC_RECONCILIATION_READY = YES` means reconciliation inputs are identified and may be planned later, not that OpenSpec was changed or application implementation may begin.

## Conclusion

Frozen TARGET is reachable only through reviewed additive structures, explicit key/crosswalk mappings, data preflight, semantic backfill, application compatibility, and delayed contraction. This document authorizes no schema or code operation and verifies no implementation requirement.

```text
MIGRATION_IMPLEMENTATION = NOT_STARTED
EF_CORE_MIGRATIONS_GENERATED = NO
PRODUCTION_SCHEMA_CHANGED = NO
```
