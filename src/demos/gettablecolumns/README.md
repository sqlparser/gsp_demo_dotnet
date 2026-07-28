# gettablecolumns — Extract tables and columns referenced by a SQL script

The flagship "metadata harvesting" demo. Given a SQL script (or a whole
directory of scripts), it reports every table referenced, every column each
statement touches, *which clause of the statement the column appears in*
(SELECT list, WHERE, GROUP BY, ORDER BY, JOIN ON, …), and how tables are
joined.

It is the richest example of what GSP can infer about a script without
running it.

## Files

| File | Purpose |
|------|---------|
| `sample.cs` | `Main` entry point — parses CLI flags and drives `TGetTableColumn`. |
| `columnTableStmt.cs` | Small snippet: print referenced tables per statement. |
| `columnsInResultColumn.cs` | Snippet: for every item in a SELECT list, print the source columns. |
| `whatClause.cs` | Snippet: for each column, determine which SQL clause it belongs to. |
| `joinRelationAnalyze.cs` | Snippet: extract `table1.colA = table2.colB` join pairs. |

The heavy lifting lives in the shared library `../lib/TGetTableColumn.cs`.

## What it shows

- Walking `sqlparser.sqlstatements` and, for each statement, the
  `stmt.tables` and `stmt.tables.getTable(i).LinkedColumns` collections.
- Using `ESqlClause` on a `TObjectName` to find out whether the column
  appears in the SELECT list, WHERE, GROUP BY, ORDER BY, JOIN ON, CASE, etc.
- Supplying an `IMetaDatabase` implementation so the analyser knows which
  `select *` expansions are valid (see `sampleMetaDB` in `sample.cs`).
- Table "effect type": `ETableEffectType.tetCreate`, `tetInsert`, `tetSelect`, …
- Toggling output format: summary, detailed, tree, by-clause, join-only.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release

# Default (summary) output for a single file
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle

# All .sql files in a directory
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /d YOUR_SQL_DIRECTORY /t mssql

# Extra-verbose detail
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /f samples/mssql-report.sql /t mssql /showDetail

# Show the AST as a tree
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /f samples/mssql-report.sql /t mssql /showTreeStructure

# Group columns by clause (SELECT / WHERE / JOIN …)
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /f samples/mssql-report.sql /t mssql /showBySQLClause

# Show just the join relationships
dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- \
  /f samples/oracle-outer-join.sql /t oracle /showJoin
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Single SQL file. |
| `/d <dir>` | Directory of SQL files. |
| `/o <path>` | Write output to file instead of stdout. |
| `/t <vendor>` | SQL dialect. Default `oracle`. |
| `/showSummary` | (default) Summary per statement. |
| `/showDetail` | Detailed column/location info. |
| `/showTreeStructure` | Render tables/columns as a tree. |
| `/showBySQLClause` | Group columns by SELECT / WHERE / GROUP BY / ORDER BY / JOIN. |
| `/showJoin` | Only print join relationships. |

## Core code pattern

```csharp
var getter = new TGetTableColumn(EDbVendor.dbvoracle)
{
    showSummary      = true,
    showDetail       = false,
    showBySQLClause  = false,
    showJoin         = false,
    showColumnLocation = true,
};
getter.runFile(new FileInfo("samples/mssql-report.sql"));
```

Or directly from your own walker:

```csharp
for (int i = 0; i < stmt.tables.size(); i++)
{
    TTable t = stmt.tables.getTable(i);
    Console.WriteLine("table: " + t.FullName);
    for (int j = 0; j < t.LinkedColumns.size(); j++)
    {
        TObjectName col = t.LinkedColumns.getObjectName(j);
        Console.WriteLine("  column: " + col.ColumnNameOnly
                         + "  clause: " + col.Location); // ESqlClause
    }
}
```

## Build your own

This demo is the starting point for data-catalog tooling, impact analysis,
and access-control auditing. For column-level lineage *across statements*
(e.g. traced through an `INSERT ... SELECT`), use `columnImpact/` or
`dlineage/`. For the join graph only, use `joinRelationAnalyze/`.
