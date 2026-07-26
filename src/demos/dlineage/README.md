# dlineage — Full data-lineage analysis

Produces an end-to-end **data lineage** graph from one or many SQL scripts:
every base table, every view, every stored procedure, and every
source-column → target-column edge linking them. This is the flagship
metadata-extraction demo.

Output is XML describing:

- Tables, views, and their column lists (inferred from DDL when present,
  otherwise from usage).
- Stored-procedure definitions.
- Column-to-column lineage edges across `SELECT`, `INSERT ... SELECT`,
  `CREATE TABLE AS`, `CREATE VIEW AS`, `MERGE`, `UPDATE`, procedure calls,
  and set operations.
- Optionally the DDL schema only (`/ddl`) or the column-impact report
  without the DDL prelude.

## What it shows

- Using `DlineageCommon` (see `../dlineageCommon/`) — a pipeline of
  `DDLParser` → `ViewParser` → `ProcedureRelationScanner` → column-impact
  analyser.
- Running the analysis over a single file or a whole directory tree.
- **Forward analysis** (`/fo <table.column>`) — "starting from this source
  column, what downstream columns/tables ultimately depend on it?".
- **Backward analysis** (`/b <view.column>`) — "what source columns feed
  this target column?".
- Strict vs fuzzy matching of catalog/schema names (`/s`).

## Build and run

```bash
dotnet build demos.dlineage.csproj -c Release

# Full lineage XML for one file
dotnet run --project demos.dlineage.csproj -c Release -- /t oracle /f pipeline.sql

# Full lineage over a directory
dotnet run --project demos.dlineage.csproj -c Release -- /t mssql /d ./scripts

# DDL schema only
dotnet run --project demos.dlineage.csproj -c Release -- /f pipeline.sql /ddl

# Forward-trace a source column: which downstream columns depend on emp.sal?
dotnet run --project demos.dlineage.csproj -c Release -- /f pipeline.sql /fo emp.sal

# Backward-trace a target column: where does view.report.total come from?
dotnet run --project demos.dlineage.csproj -c Release -- /f pipeline.sql /b report.total
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Analyse one SQL file. |
| `/d <dir>`  | Analyse every `.sql` in a directory. |
| `/t <vendor>` | Dialect. Default `oracle`. Supports the full matrix (oracle/mssql/mysql/db2/postgresql/hive/teradata/sybase/informix/netezza/greenplum/redshift). |
| `/fo <tbl.col>` | Forward trace this source table column. |
| `/b  <tbl.col>` | Backward trace this target view/table column. |
| `/ddl` | Emit the DDL schema inventory instead of lineage. |
| `/s`   | Strict mode — match catalog + schema names exactly. |
| `/log` | Also write a `dlineage.log` file with diagnostics. |

## Core code pattern

```csharp
using gudusoft.gsqlparser.demos.dlineage;

var dlineage = new DlineageCommon(new FileInfo("pipeline.sql"),
                                  EDbVendor.dbvoracle,
                                  strict: false,
                                  showUIInfo: false);

// Full column-impact XML to stdout
dlineage.columnImpact();

// Or: collect relationships programmatically for custom processing
var errBuf = new StringBuilder();
var impact = dlineage.generateColumnImpact(errBuf);
IList<ColumnMetaData[]> edges = dlineage.collectDlineageRelations(impact);
// Each ColumnMetaData[] is a [source, target] pair.
```

## Build your own

This demo is the heart of data-governance tooling: ingest your team's SQL
scripts, build a graph, and expose it through a web UI. For just the edge
list (no embedded DDL schema), see `dlineageRelation/`. For column-level
analysis that is scoped to a single SELECT rather than a repository,
`columnImpact/` is a lighter-weight alternative.
