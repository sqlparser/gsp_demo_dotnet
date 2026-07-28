# dlineageCommon — Shared data-lineage library

The analysis engine behind `dlineage/`, `dlineageRelation/`, and
`extractTableColumns/`. Not a standalone executable — it is a class
library that those demo csprojs reference via `<ProjectReference>`.

## What lives here

- `DlineageCommon.cs` — the top-level orchestrator. Construct with either
  a SQL string, a `FileInfo` pointing at a single file, or a `FileInfo`
  pointing at a directory of SQL files. It runs three phases over the
  input(s):
  1. **`DDLParser`** — collects table/column definitions from `CREATE TABLE`
     statements.
  2. **`ViewParser`** — resolves `CREATE VIEW` definitions against the
     tables discovered in phase 1.
  3. **`ProcedureRelationScanner`** — collects procedure definitions and
     the objects they reference.
  Then `columnImpact()` / `generateColumnImpact(errBuf)` walks every
  SELECT / DML statement and records column-to-column relationships using
  the metadata from phases 1–3.
- `columnImpact/` — per-SELECT column-impact tracing (subqueries, CTEs,
  set operations, views).
- `model/metadata/`, `model/ddl/schema/`, `model/xml/` — strongly typed
  objects for the analyser's metadata, DDL schema, and XML output.
- `metadata/` — DDL/view/procedure pre-scanners.
- `util/` — internal helpers.

## Build

```bash
dotnet build src/demos/dlineageCommon/demos.dlineageCommon.csproj -c Release
```

Consuming demos pick this up automatically via project references.

## Core API

```csharp
using gudusoft.gsqlparser.demos.dlineage;

// From a SQL string
var d1 = new DlineageCommon(sqltext, EDbVendor.dbvoracle,
                            strict: false, showUIInfo: false);

// From a file or a directory (recursive)
var d2 = new DlineageCommon(new FileInfo("script.sql"),
                            EDbVendor.dbvmssql, false, false);

// Emit the full column-impact XML to stdout
d1.columnImpact();

// Or collect the relationships for custom downstream work
var errBuf  = new StringBuilder();
var impact  = d1.generateColumnImpact(errBuf);
var edges   = d1.collectDlineageRelations(impact);       // IList<ColumnMetaData[]>
d1.forwardAnalyze("emp.sal", edges);                     // -> downstream columns
d1.backwardAnalyze("report.total", edges);               // -> source columns

// Or render the DDL schema inventory
d1.outputDDLSchema();
```

## Build your own

This is the piece to embed when you want to ship a data-lineage product —
it is the best-covered abstraction for "give me the column graph of a
repository full of SQL". If you only need per-script tracing without the
DDL / view pre-pass, the lighter-weight `columnImpact/` demo is a better
starting point.
