# columnImpact — Column-level lineage / impact analysis

Trace each column back to its source tables through SELECTs, subqueries,
views, set operations, and joins. Answers the question "if I change column
X, which downstream SELECT list items will change?".

## What it shows

- Walking the AST of a `TSelectSqlStatement`, including nested
  subqueries, CTEs, `UNION` branches, and views.
- Collecting the columns that contribute to each SELECT list item.
- Classifying each contribution by clause (`SELECT`, `WHERE`, `GROUP BY`,
  `ORDER BY`, `JOIN ON`, `START WITH`, `CONNECT BY`).
- Emitting results in plain text, simplified text (`/s`), column-level
  (`/c`), or XML (`/xml`) formats.
- Optional **view tracing** (`/v`): follow columns through view definitions
  back to base-table columns.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/columnImpact/demos.columnImpact.csproj -c Release

# Built-in Oracle sample, detailed output
dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- /d

# Your own file, simple output
dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle /s

# XML output (machine-readable)
dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle /s /xml

# Column-level summary
dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle /s /c

# Trace through views: every view column back to its source columns
dotnet run --project src/demos/columnImpact/demos.columnImpact.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle /v
```

`/v` reports one row per view column, naming the table column it derives from and
the expression that produced it:

```text
rt=col  view=v_employee_costs  column=base_salary  source=EMPLOYEES.SALARY  expression=
rt=col  view=v_employee_costs  column=total_cost   source=EMPLOYEES.SALARY  expression=e.salary * (1 + NVL(e.commission_pct, 0))
```

`expression=` is empty for columns selected straight through without a
calculation, as `base_salary` is above.

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | SQL script file. If omitted, a built-in Oracle sample is used. |
| `/t <vendor>` | Dialect. Default `oracle`. |
| `/d` | Detailed output (default). |
| `/s` | Simplified (summary) output. |
| `/c` | Column-level summary. Implies `/s`. |
| `/xml` | XML output. Implies `/s`. |
| `/v` | Trace lineage through views. |
| `/o <path>` | Write output to file. |

## Core concepts in the code

- `columnsInExpr : IExpressionVisitor` — per-expression walker that collects
  the `TColumn` references contributing to a result column.
- `ClauseType` enum — the SQL clause each column reference was found in.
- For CTEs, subqueries in the FROM clause, and set operations, the demo
  recurses into the child statement and merges its impact results.

## Build your own

This is the right starting point when you need column-level lineage *within
one script*. For lineage across many scripts — e.g. "which base table
column ultimately feeds the report view's `net_revenue` column?" — move up
to `dlineage/`, which adds a DDL pass, view resolution, and forward/backward
trace helpers.
