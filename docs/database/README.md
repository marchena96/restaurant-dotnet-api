# Restaurant Database Design

## Purpose

This directory records the frozen logical database design for Restaurant Relational Model v2.0. It defines the target relational structure, normalization result, and design rationale before any physical SQL Server or EF Core implementation begins.

These documents describe a **LOGICAL TARGET**. They do not describe an already-implemented schema and do not authorize database changes.

## Current Status

```text
LOGICAL_MODEL = FROZEN
NORMALIZATION_GATE = PASS
MIGRATION_IMPACT_ANALYSIS = COMPLETE
PHYSICAL_IMPLEMENTATION = NOT_STARTED
EF_CORE_MIGRATIONS = NOT_STARTED
```

The current EF Core models, migrations, and deployed schema remain **AS-IS** and may differ from Restaurant Relational Model v2.0.

No migration may be generated until Migration Impact Analysis is complete and the relevant OpenSpec change has been reconciled with this frozen model.

## Source-of-Truth Precedence

If database-design artifacts conflict, use this precedence:

1. [`decisions/relational-design-decisions-v2.md`](decisions/relational-design-decisions-v2.md)
2. [`relational-data-dictionary-v2.md`](relational-data-dictionary-v2.md)
3. [`relational-model-v2.dbml`](relational-model-v2.dbml)
4. [`normalization-audit-v2.md`](normalization-audit-v2.md)
5. Generated or rendered diagrams

Generated images and diagrams are explanatory only. They never override the textual model.

## Files

| File | Purpose |
|---|---|
| `decisions/relational-design-decisions-v2.md` | Frozen design decisions DBD-001 through DBD-007 |
| `relational-data-dictionary-v2.md` | Canonical entities, columns, constraints, relationships, and operational indexes |
| `relational-model-v2.dbml` | Machine-renderable representation of the 16-table logical model |
| `normalization-audit-v2.md` | Entity-by-entity 1NF, 2NF, and 3NF audit |
| `migration-impact-analysis-v2.md` | Analysis-only comparison of CURRENT AS-IS EF Core schema and frozen TARGET v2, including preflight gates and phased migration strategy |

## Scope Boundary

This package contains database design documentation only. It does not:

- modify production code or EF Core models;
- create, edit, or apply migrations;
- assert that target constraints or indexes currently exist;
- modify an OpenSpec change;
- define distributed architecture;
- add business requirements beyond the frozen logical model.

Physical naming, SQL scripts, migration sequencing, data conversion, deployment, rollback, and enforcement mechanisms remain future implementation work.
