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

```bash
dotnet build demos.columnImpact.csproj -c Release

# Built-in Oracle sample, detailed output
dotnet run --project demos.columnImpact.csproj -c Release -- /d

# Your own file, simple output
dotnet run --project demos.columnImpact.csproj -c Release -- /t mssql /f query.sql /s

# XML output (machine-readable)
dotnet run --project demos.columnImpact.csproj -c Release -- /t oracle /f query.sql /s /xml

# Column-level summary
dotnet run --project demos.columnImpact.csproj -c Release -- /f query.sql /s /c

# Trace through views (requires CREATE VIEW statements in the script)
dotnet run --project demos.columnImpact.csproj -c Release -- /f query.sql /v
```

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
