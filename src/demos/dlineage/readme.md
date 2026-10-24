# dlineage User Guide

[English](readme.md) | [简体中文](readme_zh.md)

`dlineage` extracts column and table lineage from SQL files or directories. It produces XML, JSON, CSV and graph data without connecting to a database.

The former `dataFlowAnalyzer` demo has been merged into this directory. New functionality calls the library's formal `gudusoft.gsqlparser.dlineage.DataFlowAnalyzer` API, following the Java demo's options and processing order. The previous `dlineage` commands for DDL output, forward tracing and backward tracing remain available.

## Contents

- [Prerequisites and build](#prerequisites-and-build): SDK, local DLLs and unreleased library builds.
- [Quick start](#quick-start): run the included examples.
- [Input, output and exit codes](#input-output-and-exit-codes): directory scanning, encoding and diagnostics.
- [Output formats](#output-formats): column lineage, table lineage, CSV and visualization data.
- [Analysis options](#analysis-options): simplified output, relation filters, transformations, objects and coordinates.
- [Offline metadata](#offline-metadata): resolve ambiguous columns using JSON metadata.
- [Statistics mode](#statistics-mode): batch status and counting rules.
- [Legacy command migration](#legacy-command-migration): retained commands, entry points and the meaning of `/s`.
- [Using the API](#using-the-api): call the formal library from your application.
- [Troubleshooting and scope](#troubleshooting-and-scope): versions, dialects and Java compatibility boundaries.

- [Current SQL Server examples and validation](#sqlserver-acceptance).

## Prerequisites and build

Install the .NET 10 SDK. Run all commands from the **`gsp_demo_dotnet` repository root**.

**The pinned NuGet package, `4.1.0.8`, does not contain the formal dlineage API. Full functionality requires the current development library, such as a local `4.1.0.18` DLL. You do not need to publish a new version first.**

Specify the DLL with `GspAssemblyPath`. This requires no changes to project files, the pinned NuGet version or the global package cache:

```bash
dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release \
  -p:GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll
```

The property also applies to project dependencies so that the process loads the same parser throughout. Use an absolute path to a DLL containing the current formal API; an older DLL with the same filename is insufficient. A `net10.0` DLL works for this demo. To build the entire solution, including its shared libraries with multiple targets, use a `netstandard2.0` DLL.

To build the library from an adjacent `gsp_dotnet` checkout:

```bash
dotnet build ../gsp_dotnet/gsp_dotnet_core/src/gsqlparser/gsqlparser.csproj \
  -c Release -f net10.0 \
  -p:includeDB2=true -p:includeGreenplum=true -p:includeHive=true \
  -p:includeImpala=true -p:includeInformix=true -p:includeMdx=true \
  -p:includeMssql=true -p:includeMySQL=true -p:includeNetezza=true \
  -p:includeOracle=true -p:includePostgreSQL=true -p:includeRedshift=true \
  -p:includeSnowflake=true -p:includeSybase=true -p:includeTeradata=true

dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release \
  -p:GspAssemblyPath="$PWD/../gsp_dotnet/gsp_dotnet_core/src/gsqlparser/bin/Release/net10.0/gudusoft.gsqlparser.dll"
```

After building, run the demo DLL directly. You do not need to repeat the build property:

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /version
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /help
```

Use the same arguments in Windows PowerShell, substituting your DLL path:

```powershell
dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release `
  -p:GspAssemblyPath=C:/gsp/gudusoft.gsqlparser.dll
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /f samples/dlineage/basic.sql /json
```

Without a local DLL, the repository still builds with its original NuGet configuration. Help, version information and legacy commands remain available. New analysis commands report the missing API and return `2`; they do not silently switch to the old algorithm. `EnableDlineage` is enabled when a local DLL is specified or `GspVersion >= 4.1.0.18`. This version check is a compilation condition, not a statement that the package has been published.

## Quick start

These examples use the full demo built above:

```bash
# One file: column-level XML, using the default Oracle dialect
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql

# Simplified column-level JSON written to a file
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /t oracle /s /json /o lineage.json

# Table-level CSV
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /tableLineage /csv /o tables.csv

# Multiple SQL files using the same dialect
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /d YOUR_SQL_DIRECTORY /t mssql /s /json /o combined.json
```

Examples are in [`samples/dlineage/`](../../../samples/dlineage/). `basic.sql` works with Oracle, SQL Server, MySQL and PostgreSQL. `advanced-oracle.sql` includes a view, CTE, JOIN, CASE, SUM and COUNT. `routine-oracle.sql` demonstrates variables, cursors and synonyms; `temporary-mssql.sql` demonstrates temporary tables. `metadata-query.sql` and `metadata.json` form a metadata example. Select the dialect appropriate to each file rather than analyzing this whole example directory as one SQL input set.

## Input, output and exit codes

| Argument | Description |
|---|---|
| `/f <file>` | Analyze one SQL file. Specify exactly one of `/f` and `/d`. |
| `/d <directory>` | Analyze a directory recursively, using the formal library's filtering and sorting rules. Normal mode can read `.txt` files and files without extensions as well as `.sql`; it excludes known non-SQL extensions such as `.md`, images and DLLs. Use a dedicated SQL directory. |
| `/t <dialect>` | Default: `oracle`. The current formal .NET implementation supports `oracle`, `mssql` (alias `sqlserver`), `mysql` and `postgresql` (alias `postgres`). |
| `/o <file>` | Write UTF-8 output without a BOM. Otherwise, write to standard output. The output must be outside the input directory and must not overwrite the input SQL or metadata file. Its parent directory must already exist. |
| `/log` | When diagnostics exist, also write them to `dataflow.log` in the current working directory, replacing the previous log. |
| `/version` | Show the actual library version and release date. No input file is required. |
| `/help`, `/?`, `--help` | Show help. Running without arguments also shows help. |

Option names are case-insensitive. Quote paths and delimiters containing spaces. SQL input follows the library's file-reading rules; the analyzer does not convert all SQL text to uppercase.

Diagnostics go to standard error, separately from JSON/XML output. When `/o` is specified, the result is not echoed to standard error. An output file may contain a partial model or `<error>` information: **the existence of a file does not mean analysis succeeded**.

| Exit code | Meaning |
|---|---|
| `0` | Analysis completed without library-reported errors, or help/version information was displayed successfully. |
| `1` | SQL analysis, output or execution failed. Check standard error and any errors in the model. |
| `2` | Invalid arguments, paths, metadata format, dialect or required library API. |

## Output formats

| Argument | Output |
|---|---|
| No format argument | Column-level XML, with a `<dlineage>` root and `<relationship>` elements. |
| `/json` | A SQLFlow JSON model containing database objects, relationships and processes. |
| `/json /graph` | JSON including graph structure. Combine with `/showER` for ER graph data. |
| `/lv` | JSON for lineage visualization. It does not open a browser or host a web page. |
| `/csv` | Detailed column-level CSV, including object/column IDs, relation types and statement effect types. |
| `/csv-simple` | Simplified column-level CSV: source, target and relation type. Excludes the synthetic `RelationRows` column. The default separator is a comma followed by a space. |
| `/delimiter <text>` | CSV separator. Ordinary CSV defaults to `,`. The historical `/delimeter` spelling from the Java usage text is also accepted. |
| `/tableLineage` | Table-level lineage. Combine with XML, `/json`, `/json /graph`, `/lv` or `/csv`. This mode overrides the simplification settings from `/s` and `/i`. |
| `/s /text` | Text dependencies, such as `amount depends on: source_data.amount`. |
| `/traceView` | Trace source tables and views, automatically enabling simplified mode and omitting intermediate data. |

```bash
# Simplified CSV with a pipe separator
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /s /csv-simple /delimiter "|"

# Graph data
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /json /graph /o graph.json

# Text dependencies
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /s /text
```

Format precedence follows Java. Prefer selecting one format per invocation. For column lineage, the order is `/lv` → `/csv-simple` → `/csv` → `/json` → `/traceView` → text/XML. For table lineage, CSV takes precedence, followed by `/lv` → JSON/graph → XML. As in Java, `/tableLineage /csv-simple` produces detailed table-level CSV; omit `/tableLineage` for simplified column-level CSV. `/graph` requires `/json`. `/text` requires explicit `/s` and has no effect when a higher-priority format is selected.

CSV follows the Java library's output conventions. Do not assume that every delimiter or SQL identifier is escaped according to general RFC CSV rules; inspect representative output before importing it automatically.

## Analysis options

### Intermediate results and retained objects

| Argument | Description |
|---|---|
| `/s` | Simplify output by removing intermediate result sets. Useful when importing lineage into another system. |
| `/i` | Hide intermediate results while retaining SQL function result sets. If combined with `/if`, function results are still removed. |
| `/topselectlist` | Retain the outermost SELECT result, usually with `/s`. Using it alone is not equivalent to `/s`. |
| `/withTemporaryTable` | Retain temporary tables in simplified output. They are omitted by default. |
| `/showResultSetTypes <types>` | Retain specified result-set types in `/s` or `/i` simplified output. Use a comma-separated list. |
| `/showVariable` | Retain variables in simplified mode. |
| `/showCursor` | Retain cursors in simplified mode. |
| `/showSynonym` | Retain synonyms in simplified mode. |
| `/removeResultSetTypes <types>` | Post-process the generated model to remove specified result-set types while preserving dependency connections. |
| `/removeVariable` | Remove variables during post-processing. |
| `/removeCursor` | Remove cursors during post-processing. |
| `/removeUnusedSynonym` | Omit unused synonyms. |
| `/if` | Remove function result sets during post-processing; other intermediate results follow the selected mode. |

The current library recognizes these result-set types: `select_list,array,struct,result_of,cte,insert_select,update_select,merge_update,merge_insert,output,update_set,pivot_table,unpivot_table,alias,function,case_when,cursor,variable`. Type recognition follows the library's rules; naming an unknown type does not add support for it.

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /s \
  /showResultSetTypes cte,function /json
```

### Relationships, expressions and names

| Argument | Description |
|---|---|
| `/j` | Include JOIN relationships. |
| `/filterRelationTypes <types>` | Emit only the specified types: `fdd` (direct), `fdr` (indirect), `join`, `call`, `er`. Use a comma-separated list. |
| `/s /simpleShowRelationTypes <types>` | Set the relation types traversed during simplification, typically `fdd,fdr`. Applied only with explicit `/s`. |
| `/transform` | Include transformation expression code. |
| `/transform /coor` | Use transformation coordinates according to Java semantics. `/coor` alone does not enable transformation information. |
| `/showConstant` | Show the constant table. |
| `/showER` | Include entity relationship information. For graph output, use `/json /graph /showER`. |
| `/treatArgumentsInCountFunctionAsDirectDataflow` | Treat COUNT arguments as direct data dependencies. Disabled by default. |
| `/showCaseWhenAsIndirect` | Treat CASE WHEN conditions as indirect dependencies. By default, conditions are treated as direct dependencies. |
| `/n` | Normalize output. |
| `/ic` | Omit coordinates from output. |
| `/traceTablePosition` | Trace all table positions. |
| `/lof` | Link orphan columns of unknown ownership to the first table. This is a heuristic; supplying DDL or metadata is preferable. |
| `/defaultDatabase <name>` | Set the default database. |
| `/defaultSchema <name>` | Set the default schema. |
| `/showImplicitSchema` | Show implicit schema names. |

## Offline metadata

For an unqualified column in a query such as `SELECT ename FROM emp, dept ...`, the analyzer needs to know which table owns `ename`. Put the table DDL before the query or supply offline metadata:

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/metadata-query.sql /t mssql \
  /env samples/dlineage/metadata.json \
  /defaultDatabase demo_db /defaultSchema dbo /s /json /o metadata-lineage.json
```

`/env` calls the formal `TJSONSQLEnvParser`, using the SQLFlow, Grabit, SQLDep or sharded formats it recognizes. As in the Java demo, it selects the **first environment** returned. This demo passes an absolute filename so that relative paths in sharded metadata retain their base directory. The included `metadata.json` uses SQLFlow format; copy it and adjust the tables and columns for your schema.

A missing file, unsupported format or failure to load an environment produces an error and exit code `2`. Metadata is not silently ignored. Its dialect, database and schema should agree with the SQL and default naming options.

## Statistics mode

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /d YOUR_SQL_DIRECTORY /t oracle /stat /o statistics.txt
```

`/stat` recursively selects `.sql` files. For each file, it reports status, elapsed time, absolute path and byte length, followed by totals for files, statements, lines, successful/failed statements and average timings. Status `1` means no analysis errors; `2` means errors with a partial table model; `3` means no usable table model.

Counting follows Java: a warm-up runs first and is excluded from the statistics. Line counts use the final token's line number and Java string-splitting rules, so they are not equivalent to `wc -l`. **Files larger than 10,000 bytes are skipped even with a full-edition DLL**, and counted under `Files ignored in trial version`. This is a demo statistics rule; ordinary `/f` and `/d` analysis does not add this filter.

Statistics mode uses separate analyzers, a listener and a fixed orphan-column setting. It does not apply every normal-mode display option. It accepts `/env`; simplification settings come only from `/s` and `/traceView`. Averages are `0` when no SQL files are found, files are empty or all files are skipped. Timings vary by environment.

## Legacy command migration

| Previous entry point or command | Current usage |
|---|---|
| `src/demos/dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj` | Use `src/demos/dlineage/demos.dlineage.csproj`; the executable is `dlineage.dll`. |
| `build.sh run dataFlowAnalyzer ...` | Retained as an alias for `dlineage`; no second project is built. |
| The old `columnImpactResult` report from `dlineage /f ...` | Use `/legacy /f ...`. The new default produces the formal dlineage model. |
| `/fo <table.column>` | Forward tracing is retained and automatically selects legacy mode. |
| `/b <view.column>` | Backward tracing is retained and automatically selects legacy mode. |
| `/ddl` | DDL schema output is retained and automatically selects legacy mode. Output is valid XML. |
| The old `/s` for strict catalog/schema matching | Retains that meaning in legacy mode. Prefer the explicit `/strict` spelling. In normal mode, `/s` means simplified output, as in Java. |

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /fo employees.salary /strict

dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /b v_employee_costs.total_cost

dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /ddl
```

Legacy commands use the original `DlineageCommon`. They accept `/f`, `/d`, `/t`, `/o` and `/log`, but cannot be combined with new-mode arguments such as `/json`. They also work with the pinned NuGet `4.1.0.8` package. The old `gudusoft.gsqlparser.demos.dataFlowAnalyzer` analyzer and model sources moved together to `Compatibility/DataFlow/`, retaining their namespaces and constructors for existing source consumers. New functionality belongs in the formal library, rather than in this historical algorithm.

With the Bash build script, pass the local DLL property as an environment variable, for example:

```bash
GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll \
  ./src/demos/build.sh run dataFlowAnalyzer /f samples/dlineage/basic.sql /json
```

`dlineageCommon` remains a shared library used by `dlineageRelation` and other projects. It and this CLI are not another pair of executables to merge.

## Using the API

New applications should call the formal library directly, without copying the demo's legacy models or algorithm:

```csharp
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.dlineage;
using gudusoft.gsqlparser.dlineage.dataflow.model;
using gudusoft.gsqlparser.runtime;

var option = new Option();
option.setVendor(EDbVendor.dbvmssql);
option.setOutput(true);
option.setSimpleOutput(true);
var analyzer = new DataFlowAnalyzer(new JFile("query.sql"), option);
try
{
    string xml = analyzer.generateDataFlow();
    var model = analyzer.getDataFlow();
    foreach (var error in analyzer.getErrorMessages())
        Console.Error.WriteLine(error.getErrorMessage());
    Console.WriteLine(xml);
}
finally { analyzer.dispose(); }
```

CLI hosts can call `Dlineage.Run(args, outputWriter, errorWriter)` to obtain an exit code. The call protects and restores `Console.Out` and `Console.Error` without closing the supplied writers. CLI calls within the same process are serialized. For your own concurrent service, use the formal library's lifecycle and concurrency APIs.

## Troubleshooting and scope

- **Why does an old-package build succeed while `/json` reports a missing API?** That build supports the legacy entry points. Rebuild with a current local DLL as described above, and check `/version` to confirm the loaded library.
- **Why are there no relationships, or why does the command return `1`?** Check standard error, errors in the XML and the selected dialect. Trial restrictions depend on the actual DLL. The older NuGet trial package has an approximately 10,000-character limit; an output file alone does not establish success.
- **Why does Java list more databases while this guide lists four?** The current formal C# lineage implementation is supported and validated for four dialects. The CLI implements the actual Java demo's analysis and output options; it does not extend the underlying parser or lineage engine's dialect coverage.
- **Does this include database connections, a DuckDB repository or browser visualization?** The actual Java entry point has no `/fromdb`, `/exportonly` or `/metadataoutput`, and does not implement the DuckDB commands proposed in its `docs/merge_csv.md`. This demo follows the implemented code: it reads offline metadata and emits graph data.
- **Which CLI differences from Java are intentional?** Argument and analysis failures return nonzero exit codes; output and diagnostics stay separate; resources and console streams are managed reliably; invalid metadata is reported; empty `/stat` input does not cause division by zero; `/s /text` actually emits text, correcting the Java demo's XML serialization after setting text mode. Normal analysis retains Java's option ordering and format precedence.
- **Is every output identical to Java?** Comparison retains a PostgreSQL `DECIMAL` type-name difference: Java emits `decimal`, while the current C# library emits `dec`. The demo does not rewrite it. JSON may also differ in property order or escaped bytes. Parsed models and raw bytes are checked separately; see the validation records.

The implementation reference is `DataFlowAnalyzer.java` at Java demo commit `65a2cef16409e99f834989478e683976032e003a`. The current C# library's Java source baseline is `4940a81cf671dee163239df60488fa716d02177e`. Validation methods and actual differences are documented in [`tests/dlineage/readme.md`](../../../tests/dlineage/readme.md), currently in Chinese.

<a id="sqlserver-acceptance"></a>

## 2026-10-24: Current SQL Server development build

The current local library is **4.1.0.18**; no new NuGet release is required. Missing CTE reference checks and CROSS APPLY argument collection have been corrected in the library. The demo continues to use the formal API. Each latest DLL passed the existing 62 CLI tests; the netstandard2.0 parser also ran under Linux/.NET 10.

The additional suite preserves 14 original SQL inputs and 62 option/format groups. Per DLL, **58 complete models match, 29 stdout byte streams match, and 34 groups return zero on both sides**. Four groups retain the existing five-segment physical identifier limitation. Another 24 produce matching models and the same orphan-column diagnostic messages, with C# returning 1 according to its documented error contract. Java also prints an extra blank line after the error header; both stderr byte streams are retained. The complete comparison still returns 1; failures are retained. Public parser fixtures are not 14 customer workloads.

The original `SELECT ... INTO [dbo].[customer_revenue]` query is included and can produce both files:

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/sqlserver-acceptance-20261024/customer-revenue.sql /t mssql /o customer_revenue.xml
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/sqlserver-acceptance-20261024/customer-revenue.sql /t mssql /json /o customer_revenue.json
```

The results contain all five target columns and their sources. Actual DLL identities, results and replay instructions are in the [validation guide](../../../tests/dlineage/readme.md#2026-10-24-sql-server-当前开发版验证) and [machine record](../../../tests/dlineage/evidence/2026-10-24/validation.json). The October 21 four-dialect comparison retains its original DLL scope. This batch did not run Windows CI or change the default package pin.
