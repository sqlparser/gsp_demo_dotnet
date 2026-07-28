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

All commands are run from the repository root.

```bash
dotnet build src/demos/removeColumn/demos.removeColumn.csproj -c Release

# Drop every predicate referencing employees.salary. The AND e.salary > 5000
# filter disappears; the rest of the statement is regenerated unchanged.
dotnet run --project src/demos/removeColumn/demos.removeColumn.csproj -c Release -- \
    "employees.salary" /f samples/oracle-outer-join.sql /t oracle
```

Columns are matched by **table name, not alias**: `employees.salary`, not
`e.salary`. Pass several as one comma-separated argument.

### Known limitation

Removing a column that appears in a statement's select list is not implemented.
The demo prints

```text
Not yet implements removing column from selectList
```

and leaves that column in place, while still processing the others. So

```bash
dotnet run --project src/demos/removeColumn/demos.removeColumn.csproj -c Release -- \
    "employees.salary,departments.department_name" /f samples/oracle-outer-join.sql /t oracle
```

removes the `salary` predicate but keeps `department_name`, because the latter is
selected rather than filtered on. The example above stays within what the demo
actually implements.

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
