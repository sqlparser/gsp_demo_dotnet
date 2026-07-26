# removeColumn — Rewrite SQL to drop specific columns

Parses a SQL statement, **removes the named columns wherever they appear**
(SELECT list, GROUP BY, ORDER BY, INSERT column list, UPDATE SET list), and
emits the rewritten SQL. Handles subqueries, set operations, and CTEs.

A good example of how to *modify* the GSP AST and re-emit SQL — one of the
most common post-parse workflows.

## What it shows

- Loading a `TGSqlParser`, navigating to `TSelectSqlStatement` / DML nodes,
  and editing `ResultColumnList`, `GroupByClause.Items`, `OrderByClause`,
  and the insert/update column lists in place.
- Cleaning up empty clauses after deletion — e.g. dropping `GROUP BY`
  entirely when no items remain.
- Regenerating SQL via `statement.ToScript()` / `String` property after
  the AST has been mutated.
- Handling multi-column removal by iterating `column1, column2, ...`.

## Build and run

```bash
dotnet build demos.removeColumn.csproj -c Release

# Remove emp.sal and dept.dname from query.sql (Oracle)
dotnet run --project demos.removeColumn.csproj -c Release -- \
    "emp.sal,dept.dname" /f query.sql /t oracle
```

### Arguments

Positional arg 1: a comma-separated list of qualified column names
(`schema.table.column` or `table.column`).

| Flag | Description |
|------|-------------|
| `/f <path>` | **Required.** SQL script file to rewrite. |
| `/t <vendor>` | Dialect. Default `oracle`. `oracle` and `mssql` are the tested paths. |

The rewritten SQL is printed to stdout.

## Core code pattern

```csharp
var rw = new removeColumn(EDbVendor.dbvoracle, sqltext);
int ret = rw.deleteColumn("emp.sal");
if (ret > 0)
    Console.WriteLine(rw.ModifiedText);
else
    Console.WriteLine("failed: " + rw.msg);
```

Internally the demo walks every statement (including nested ones) and calls
`ResultColumnList.removeResultColumn(...)` / `GroupByItemList.remove(...)`
etc., then asks GSP to re-emit the SQL.

## Build your own

Template for any "trim a column" migration — e.g. removing a PII column
from every query that references it, or cleaning up dead columns after a
schema refactor. Pair with a search step (`extractTableColumns` or
`gettablecolumns`) to discover which scripts need rewriting.
