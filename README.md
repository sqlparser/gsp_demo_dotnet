# General SQL Parser .NET — Demos

[![build](https://github.com/sqlparser/gsp_demo_dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/sqlparser/gsp_demo_dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/gudusoft.gsqlparser.svg)](https://www.nuget.org/packages/gudusoft.gsqlparser)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Eighteen runnable sample programs for [General SQL Parser](https://www.sqlparser.com)
.NET: syntax checking, SQL formatting, column-level lineage, AST traversal and
SQL rewriting, across 15 database dialects.

Every demo is a small, self-contained console app. Clone this repository and run
one — nothing else to install, no database connection, no library sources
needed. The demos pull `gudusoft.gsqlparser` from nuget.org.

## What the library does

General SQL Parser turns SQL text into a parse tree you can inspect and modify,
in process. Given a query you get statement types, tables, columns, expressions,
joins, CTEs and subqueries; you can rewrite the tree and generate SQL back out,
or run the formatter over it. It understands 15 vendor dialects rather than one
generic SQL, so Oracle `(+)` joins, T-SQL `TOP`, Snowflake `QUALIFY` and
PostgreSQL `::` casts all parse as the real thing.

## Requirements

.NET 10 SDK. On Ubuntu 24.04+:

```bash
sudo apt-get install -y dotnet-sdk-10.0
```

Elsewhere: <https://dotnet.microsoft.com/download>. Check with `dotnet --version`.

The `netstandard2.0` build also covers .NET Framework 4.6.2+, Mono and Unity if
you consume the library directly, but these demos target `net10.0`.

## Get started

```bash
git clone https://github.com/sqlparser/gsp_demo_dotnet.git
cd gsp_demo_dotnet
dotnet build gsp_demo_dotnet.slnx -c Release
```

Check whether some SQL parses. `samples/` has ready-made files for every
dialect below, so there is nothing to write first:

```bash
dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- /f samples/mssql-report.sql /t mssql
```

```text
versionId:4.1.0.7, releaseDate:2026-7-26, datebase:dbvmssql
Success!
```

Convert Oracle's proprietary `(+)` outer joins to ANSI:

```bash
dotnet run --project src/demos/convertJoin/demos.convertJoin.csproj -c Release -- /f samples/oracle-outer-join.sql /t oracle
```

```text
SQL in ANSI joins
SELECT   e.employee_id,
         e.last_name,
         d.department_name,
         l.city
FROM     employees e
         LEFT OUTER JOIN departments d
         ON e.department_id = d.department_id
         LEFT OUTER JOIN locations l
         ON d.location_id = l.location_id
```

See [`samples/README.md`](samples/README.md) for what each file exercises.

Most demos take `/f <file>` for input and `/t <vendor>` for the dialect. Valid
vendors: `oracle`, `mssql`, `mysql`, `postgresql`, `db2`, `sybase`, `teradata`,
`hive`, `impala`, `greenplum`, `redshift`, `snowflake`, `informix`, `netezza`,
`mdx`.

To confirm what your build actually supports:

```bash
dotnet run --project src/demos/listGSPInfo/demos.listGSPInfo.csproj -c Release
```

```text
...
Version:4.1.0.7, Release date:2026-7-26, Full version = False,
Db: 14/17, dbvdb2,dbvgreenplum,dbvhive,dbvimpala,dbvinformix,dbvmysql,dbvmssql,...
```

## The demos

| Demo | What it does |
|------|--------------|
| **checksyntax** | Parse SQL and report syntax errors |
| **formatsql** | Pretty-print / reformat SQL |
| **gettablecolumns** | Extract table and column names from queries |
| **extractTableColumns** | Extract table/column pairs, including through subqueries |
| **columnImpact** | Trace column-level lineage through SELECT statements |
| **dlineage** | Full data lineage analysis |
| **dlineageRelation** | Data lineage relation extraction |
| **dataFlowAnalyzer** | Analyze data flow in SQL |
| **convertJoin** | Convert between old-style `(+)` and ANSI JOIN syntax |
| **joinRelationAnalyze** | Analyze join relationships |
| **expressionTraverser** | Walk the expression AST with a visitor |
| **visitors** | Visitor pattern demo / XML export of the AST |
| **analyzesp** | Analyze stored procedures |
| **removeColumn** | Rewrite SQL to remove specific columns |
| **removeCondition** | Rewrite SQL to remove WHERE-clause conditions |
| **removevars** | Strip bind variables from SQL |
| **tableColumnRename** | Rename table/column references in SQL |
| **listGSPInfo** | Print parser version, edition and compiled-in dialects |

Three projects in the solution are libraries, not demos: `dlineageCommon`
(shared by `dlineage` and `dlineageRelation`), `demos.lib` (the reusable
analysers under `src/demos/lib/`) and `demos.util` (command-line and
collection helpers under `src/demos/util/`).

## Trial limit

These demos resolve `gudusoft.gsqlparser` from nuget.org, which is the **trial**
build. It supports all 15 dialects and behaves exactly like the full edition,
with one limit: SQL longer than **10,000 characters** is refused. `parse()`
returns `-1` and `Errormessage` explains it. That is the trial limit, not a
parse error in your SQL — so a demo that works on a small query and fails on a
large script is hitting this, not a bug.

`listGSPInfo` prints `Full version = False` when you are on the trial.

The full edition is available from <https://www.sqlparser.com/download.php>. To
run these demos against it, replace the `PackageReference` in a demo's `.csproj`
with a `Reference` pointing at the full-edition DLL.

## Updating the library version

Every project takes its version from one property in `Directory.Build.props`:

```xml
<GspVersion>4.1.0.7</GspVersion>
```

Change it there and rebuild.

## Layout

```
gsp_demo_dotnet.slnx      Solution covering all 23 projects
Directory.Build.props     Shared settings; GspVersion lives here
src/demos/                One directory per demo
  lib/                    Shared analysers (demos.lib)
  util/                   Shared helpers (demos.util)
  dlineageCommon/         Shared library for the lineage demos
samples/                  SQL files to run the demos against
tests/                    MSTest suites over the demo code
```

Run the tests with:

```bash
dotnet test gsp_demo_dotnet.slnx -c Release
```

## If you copied this code before 28 July 2026

Some namespaces were corrected so that each demo owns its own. If you pasted
demo code into your own project and it stops compiling after pulling an update,
this table is why. Only the `using` lines change: **no type was renamed, no
behaviour changed, and no assembly name moved.**

| Was | Now |
|---|---|
| `gudusoft.gsqlparser.demos.dlineage` (for `DataFlowAnalyzer`) | `gudusoft.gsqlparser.demos.dataFlowAnalyzer` |
| `gudusoft.gsqlparser.demos.dlineage.dataflow.model` | `gudusoft.gsqlparser.demos.dataFlowAnalyzer.dataflow.model` |
| `gudusoft.gsqlparser.demos.dlineage.dataflow.model.xml` | `gudusoft.gsqlparser.demos.dataFlowAnalyzer.dataflow.model.xml` |
| `gudusoft.gsqlparser.dataFlowAnalyzer.dataflow.model.xml` | `gudusoft.gsqlparser.demos.dataFlowAnalyzer.dataflow.model.xml` |
| `gudusoft.gsqlparser.demos.dlineage` (for `DlineageRelation`) | `gudusoft.gsqlparser.demos.dlineageRelation` |
| `gudusoft.gsqlparser.demos.gettablecolumns` (for `removeColumn`) | `gudusoft.gsqlparser.demos.removeColumn` |

`gudusoft.gsqlparser.demos.dlineage` still exists and still holds `Dlineage` and
`DlineageCommon` plus the shared lineage model. It is the lineage library's
namespace; only the types listed above moved out of it. Every other demo
namespace, plus `demos.lib` and `demos.util`, is unchanged.

Two smaller changes in the same period:

- `Common.GetEDbVendor(string[], EDbVendor)` became `public` (it was `internal`,
  which only worked while the file was compiled into each demo). Existing calls
  keep working.
- `/t snowflake` and `/t impala` now select those dialects. They previously fell
  through to the oracle default without saying so, so a demo that appeared to
  work on Snowflake SQL was parsing it as Oracle.

If you are consuming the demos as source, updating the `using` lines is the
whole migration. Nothing here is published as a NuGet package, so no binary
compatibility is involved.

## Links

- Product site: <https://www.sqlparser.com>
- .NET documentation: <https://docs-dotnet.sqlparser.com>
- Library source: <https://github.com/sqlparser/gsp_dotnet>
- NuGet package: <https://www.nuget.org/packages/gudusoft.gsqlparser>
