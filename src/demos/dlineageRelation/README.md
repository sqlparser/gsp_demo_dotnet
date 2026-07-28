# dlineageRelation — Data lineage as a flat relation list

A lighter-weight sibling of `dlineage/`. Runs the same lineage analysis
pipeline but emits **only the relationship edges** — tables, procedures,
column-to-column relations, and procedure-to-procedure relations — as a
compact XML document. No embedded DDL, no forward/backward tracing, just
the edges.

Good choice when you want to feed the lineage graph into another system
(graph database, BI tool, documentation generator) and don't need
interactive tracing.

## What it shows

- Re-using `DlineageCommon` to parse and collect column impact
  (`generateColumnImpact`) and procedure relations.
- Emitting results directly as XML via `System.Xml.Linq` (`XDocument`,
  `XElement`).
- A tidy, schema-friendly XML shape:
  ```xml
  <dlineageRelation>
    <tables>...</tables>
    <procedures>...</procedures>
    <relation><source .../><target .../></relation>
    ...
  </dlineageRelation>
  ```

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/dlineageRelation/demos.dlineageRelation.csproj -c Release

# One file, output to stdout
dotnet run --project src/demos/dlineageRelation/demos.dlineageRelation.csproj -c Release -- \
  /f samples/oracle-lineage.sql /t oracle

# Whole directory, output to file
dotnet run --project src/demos/dlineageRelation/demos.dlineageRelation.csproj -c Release -- \
  /d YOUR_SQL_DIRECTORY /t mssql /o lineage.xml
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Single SQL file. |
| `/d <dir>` | Directory of SQL files. |
| `/t <vendor>` | Dialect. Default `oracle`. |
| `/o <path>` | Write XML output to this file. |
| `/log` | Also write `dlineage.log` with diagnostics. |

## Core code pattern

```csharp
var dlineage = new DlineageCommon(new FileInfo("samples/oracle-lineage.sql"),
                                  EDbVendor.dbvoracle, false, false);

var errBuf  = new StringBuilder();
var impact  = dlineage.generateColumnImpact(errBuf);

var relation = new DlineageRelation();
string xml   = relation.generateDlineageRelation(dlineage, impact);
```

## Build your own

Pipe the XML into your pipeline: parse with `XDocument.Parse`, map
`<source>` / `<target>` attributes onto your graph nodes, and import.
If you also need to render views/tables with their columns alongside the
edges, prefer `dlineage/` (whose output embeds the full DDL tree).
