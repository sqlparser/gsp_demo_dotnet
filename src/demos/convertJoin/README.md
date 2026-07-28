# convertJoin — Convert proprietary join syntax to ANSI JOIN

Rewrites SQL that uses old-style proprietary outer-join syntax into the
ANSI-standard `LEFT/RIGHT/INNER JOIN ... ON` form. Supports:

- Oracle `(+)` outer-join markers in the WHERE clause.
- SQL Server `*=` / `=*` outer-join operators in the WHERE clause.

## What it shows

- The GSP **join converter** (`gudusoft.gsqlparser.joinConvert.joinConverter`)
  — a higher-level helper built on top of the parser that rewrites the AST
  and re-emits SQL.
- Piping the converted SQL back through `FormatterFactory.pp()` to get a
  cleanly formatted result.
- A pattern you can adopt for *any* SQL-to-SQL transformation: parse ->
  transform AST (or pass to a converter) -> re-emit with the formatter.

## Build and run

```bash
dotnet build demos.convertJoin.csproj -c Release

# Built-in sample: four tables joined with (+), no file needed
dotnet run --project demos.convertJoin.csproj -c Release -- /t oracle

# Or against a file. samples/ ships one written for this demo:
dotnet run --project demos.convertJoin.csproj -c Release -- \
  /t oracle /f ../../../samples/oracle-outer-join.sql

# Convert SQL Server *=/=* syntax
dotnet run --project demos.convertJoin.csproj -c Release -- /t mssql /f your_query.sql
```

Both Oracle commands print the original and the ANSI rewrite:

```text
SQL in ANSI joins
SELECT *
FROM   summit.mstr m
       LEFT OUTER JOIN summit.alt_name altname
       ON m.ID = altname.ID
          AND altname.grad_name_ind = '*'
       ...
WHERE  m.ID = ?
```

Note that the `(+)` predicates move into the `ON` clauses while `m.ID = ?`, which
is a genuine filter rather than a join, stays in the `WHERE`.

Every table in the `FROM` clause must have a join condition. A table with none
has no ANSI `JOIN` form to convert to, and the converter reports
`This table has no join condition: <name>` rather than guessing at a cross join.

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Optional. SQL script file. If omitted, a built-in Oracle `(+)` sample is used. |
| `/t <vendor>` | `oracle` (default) or `mssql`. |

## Core code pattern

```csharp
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.joinConvert;

var converter = new joinConverter(sqltext, EDbVendor.dbvoracle);
if (converter.convert() != 0)
{
    Console.WriteLine(converter.ErrorMessage);
}
else
{
    string ansiSql = converter.Query; // rewritten SQL
}
```

Example input (Oracle):
```sql
SELECT * FROM emp e, dept d WHERE e.deptno = d.deptno(+)
```
Example output:
```sql
SELECT * FROM emp e LEFT OUTER JOIN dept d ON e.deptno = d.deptno
```

## Build your own

Use this demo as a template for migration tooling — e.g. lifting a legacy
Oracle codebase into a target that only accepts ANSI joins (Snowflake,
BigQuery, etc.). The converter preserves comments and statement structure.
