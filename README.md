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

Check whether some SQL parses:

```bash
echo 'SELECT a.id, b.name FROM ta a JOIN tb b ON a.id = b.id WHERE a.x > 1;' > q.sql
dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- /f q.sql /t oracle
```

```text
versionId:4.1.0.7, releaseDate:2026-7-26, datebase:dbvoracle
Success!
```

Reformat it:

```bash
dotnet run --project src/demos/formatsql/demos.formatsql.csproj -c Release -- /f q.sql /t oracle
```

```text
SELECT a.ID,
       b.NAME
FROM   ta a
       JOIN tb b
       ON a.ID = b.ID
WHERE  a.x > 1;
```

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

`dlineageCommon` is a shared library used by `dlineage` and `dlineageRelation`,
not a demo in its own right.

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
gsp_demo_dotnet.slnx      Solution covering all 21 projects
Directory.Build.props     Shared settings; GspVersion lives here
src/demos/                One directory per demo
  lib/  util/             Shared helper code used by several demos
  dlineageCommon/         Shared library for the lineage demos
tests/                    MSTest suites over the demo code
```

Run the tests with:

```bash
dotnet test gsp_demo_dotnet.slnx -c Release
```

## Links

- Product site: <https://www.sqlparser.com>
- .NET documentation: <https://docs-dotnet.sqlparser.com>
- Library source: <https://github.com/sqlparser/gsp_dotnet>
- NuGet package: <https://www.nuget.org/packages/gudusoft.gsqlparser>
