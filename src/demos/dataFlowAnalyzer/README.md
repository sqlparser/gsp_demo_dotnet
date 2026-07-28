# dataFlowAnalyzer — Data-flow (DML + procedure) analysis

A self-contained lineage analyser focused on **data flow through DML** —
`INSERT`, `UPDATE`, `DELETE`, `MERGE`, `CREATE TABLE AS`, and procedure
side-effects. Complementary to `dlineage/`: this one reads a script, walks
every DML statement, and reports the source columns that end up in each
target column, together with the containing statement type and token
locations.

## What it shows

- Detecting and classifying DML statements: `TInsertSqlStatement`,
  `TUpdateSqlStatement`, `TMergeSqlStatement`, `TCreateTableSqlStatement`,
  `TDeleteSqlStatement`, and vendor-specific wrappers.
- Walking the source SELECT / VALUES list in each DML and mapping its
  expressions back to columns of underlying tables.
- Handling CTEs, subqueries, set operations, and aggregate functions
  (a small table of supported aggregates is hard-coded near the top of
  `DataFlowAnalyzer.cs`).
- Emitting results with clause provenance and source token locations so
  you can render them inline in an IDE.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj -c Release

# One file
dotnet run --project src/demos/dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle

# Directory of scripts, write result to file
dotnet run --project src/demos/dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj -c Release -- \
  /d YOUR_SQL_DIRECTORY /t mssql /o dataflow.txt
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Single SQL file. |
| `/d <dir>` | Directory of SQL files. |
| `/t <vendor>` | Dialect. Default `oracle`. |
| `/o <path>` | Write output to file. |
| `/log` | Also write a `dataflow.log` diagnostics file. |

## Core code pattern

```csharp
using gudusoft.gsqlparser.demos.dataFlowAnalyzer;

var analyzer = new DataFlowAnalyzer(new FileInfo("samples/oracle-lineage.sql"),
                                    EDbVendor.dbvoracle);
var errBuf = new StringBuilder();
string report = analyzer.generateDataFlow(errBuf);
Console.WriteLine(report);
```

## Build your own

Use this as a drop-in when you already know "is this SQL a DML?" and want
the source/target column graph without the full DDL/view plumbing of
`dlineage/`. Great fit for ETL documentation, `INSERT ... SELECT`
cataloguing, and CDC pipeline visualisation.
