## Purpose

Defines observable integrity and conflict behavior for concurrent reservation creation and waiting-list promotion against SQL Server persistence aligned with the frozen Restaurant Relational Model v2.0.

## ADDED Requirements

### Requirement: Overlapping active reservations are mutually exclusive
The system SHALL prevent more than one availability-blocking reservation for the same `RestaurantTable` when their time intervals overlap on the same date, including under concurrent requests. Availability semantics SHALL use `ReservationStatus.Code` and `BlocksAvailability`, not legacy generic `Status` or magic numeric IDs.

#### Scenario: Concurrent overlapping reservation attempts
- **WHEN** two valid requests concurrently attempt to reserve the same table, date, and overlapping interval
- **THEN** exactly one reservation is committed and the other request receives a deterministic conflict response

#### Scenario: Non-overlapping reservation attempts
- **WHEN** valid requests reserve the same table on non-overlapping intervals
- **THEN** both reservations may be committed

#### Scenario: Cancelled reservation does not block
- **WHEN** an existing cancelled reservation overlaps a requested interval and no other active reservation or lock conflicts
- **THEN** the new reservation may be committed

### Requirement: Reservation creation has one explicit atomic outcome
The system SHALL execute reservation validation and resulting persistence as one atomic business operation. A successful reservation SHALL commit the reservation; a detected scheduling conflict that invokes existing waiting-list behavior SHALL commit only the intended waiting-list outcome; any unexpected failure SHALL commit neither outcome.

#### Scenario: Successful reservation
- **WHEN** all reservation invariants hold
- **THEN** the reservation is committed and no waiting-list entry is created by that request

#### Scenario: Scheduling conflict enters waiting list
- **WHEN** a valid request conflicts with an active reservation or table lock and current product behavior requires waiting-list placement
- **THEN** no reservation is committed, exactly one intended waiting-list entry is committed, and the API reports a conflict outcome

#### Scenario: Failure during compound operation
- **WHEN** an unexpected failure occurs before the operation commits
- **THEN** all writes made by that operation are rolled back

### Requirement: Reservation conflicts use a stable HTTP outcome
The API SHALL report a detected reservation concurrency or scheduling conflict as HTTP 409 without exposing database exception details.

#### Scenario: Conflict response
- **WHEN** a reservation loses a concurrent allocation decision or encounters an incompatible active booking
- **THEN** the response status is 409 and the response identifies a safe reservation conflict

### Requirement: Waiting-list promotion is atomic
The system SHALL create the target reservation and transition the retained waiting-list entry through `WaitingListStatus` in one transaction. `Reservation.SourceWaitingListEntryId` SHALL be the only persisted promotion link; `WaitingListEntry.PromotedReservationId` SHALL NOT exist.

#### Scenario: Promotion succeeds
- **WHEN** an eligible waiting-list entry is promoted to an available compatible table
- **THEN** one reservation with the unique source-entry link is committed and the retained waiting-list entry records completed promotion through its waiting-list-specific status

#### Scenario: Promotion fails
- **WHEN** any promotion validation or persistence step fails
- **THEN** neither a new reservation nor a partial waiting-list transition is committed

### Requirement: Waiting-list promotion is repeat-safe
The system SHALL ensure repeated or concurrent promotion attempts for one waiting-list entry produce at most one reservation and a deterministic result.

#### Scenario: Concurrent promotion attempts
- **WHEN** two requests concurrently promote the same waiting-list entry
- **THEN** at most one reservation is created and both outcomes can be explained from the persisted promotion state

#### Scenario: Repeated completed promotion
- **WHEN** a promotion request is repeated after that entry was successfully promoted
- **THEN** no additional reservation is created and the API returns the existing successful result or a stable already-promoted result

### Requirement: Transaction conflicts are bounded
The system SHALL use a bounded retry policy only for identified transient SQL Server transaction conflicts and SHALL return a deterministic failure when retries are exhausted.

#### Scenario: Transient deadlock
- **WHEN** SQL Server selects the operation as a deadlock victim and retry capacity remains
- **THEN** the complete transaction is retried without duplicating committed state

#### Scenario: Retry exhaustion
- **WHEN** all permitted transaction attempts fail
- **THEN** the operation returns a stable failure and leaves no partial state

### Requirement: Waiting-list preferences reference canonical zones
The system SHALL represent optional waiting-list zone preference only as `WaitingListEntry.PreferredZoneId` referencing `Zone` and SHALL NOT persist a `PreferredZone` string.

#### Scenario: Promotion checks a preferred zone
- **WHEN** an entry has `PreferredZoneId` and promotion evaluates a candidate `RestaurantTable`
- **THEN** compatibility is evaluated through the referenced `Zone`

### Requirement: Turn classification follows target semantics
The system SHALL derive a normal turn only when exactly one active `Turn` satisfies `Turn.StartTime <= Reservation.StartTime AND Reservation.EndTime <= Turn.EndTime`. Derived turn SHALL NOT be persisted. Nullable `Reservation.AssignedTurnId` SHALL represent only explicit business/admin assignment or override; legacy `Reservation.TurnId` and `DerivedTurnId` SHALL NOT be used.

#### Scenario: Exactly one active turn contains reservation
- **WHEN** exactly one active turn fully contains the reservation interval and no explicit assignment exists
- **THEN** that turn is derived without persisting a derived identifier

#### Scenario: Explicit turn override
- **WHEN** `AssignedTurnId` is present
- **THEN** it is authoritative for business classification as an explicit assignment or override

### Requirement: Restaurant table identity is zone-scoped
The system SHALL use `RestaurantTable` as the target table entity and SHALL enforce business uniqueness on `(ZoneId, TableNumber)` rather than global table-number uniqueness.

#### Scenario: Same table number in different zones
- **WHEN** two restaurant tables have the same `TableNumber` in different zones
- **THEN** both satisfy the business uniqueness rule
