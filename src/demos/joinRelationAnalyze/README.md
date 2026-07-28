# joinRelationAnalyze — Extract join relationships from a SQL query

Walks every statement in a script and reports **join pairs** — for each
`table_a.col_x = table_b.col_y` equality in a FROM list, WHERE clause,
CONNECT BY, or START WITH, prints a row identifying the two sides. Handles
implicit joins (comma-separated FROM with WHERE equalities), explicit
ANSI joins, and nested subqueries.

## What it shows

- Iterating `stmt.tables` together with the statement's WHERE / JOIN / ON
  conditions to pair table references.
- Distinguishing condition location via `ClauseType` (`where`, `connectby`,
  `startwith`, `orderby`, `casewhen`, `casethen`).
- Recursing into subqueries so joins inside nested SELECTs are reported.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/joinRelationAnalyze/demos.joinRelationAnalyze.csproj -c Release

# Built-in Oracle sample (implicit commas-plus-WHERE join)
dotnet run --project src/demos/joinRelationAnalyze/demos.joinRelationAnalyze.csproj -c Release -- /t oracle

# Your own file
dotnet run --project src/demos/joinRelationAnalyze/demos.joinRelationAnalyze.csproj -c Release -- \
  /f samples/oracle-outer-join.sql /t oracle

# Write output to file
dotnet run --project src/demos/joinRelationAnalyze/demos.joinRelationAnalyze.csproj -c Release -- \
  /f samples/mssql-report.sql /t mssql /o joins.tsv
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | SQL file. If omitted, a built-in multi-table Oracle sample is used. |
| `/t <vendor>` | Dialect. Default `oracle`. |
| `/o <path>` | Write output to file. |

### Output format (tab-separated)

```
JoinTable1	JoinColumn1	JoinTable2	JoinColumn2
emp	        id	        dept	    id
dept	    id	        order	    no
order	    no	        d	        no
```

## Build your own

Use this when you need the join *graph* rather than per-column lineage
— e.g. building an ER-diagram generator from your codebase, or detecting
missing joins that produce Cartesian products.
