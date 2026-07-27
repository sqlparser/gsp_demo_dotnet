# General SQL Parser .NET — Demo Programs

Sample programs that demonstrate what the [General SQL Parser](https://www.sqlparser.com) .NET library can do: syntax checking, SQL formatting, column/lineage analysis, AST traversal, SQL rewriting, and more.

## Prerequisites

- **.NET 10 SDK** (or later). On Ubuntu 24.04+:
  ```bash
  sudo apt-get install -y dotnet-sdk-10.0
  ```
  On other platforms, download from <https://dotnet.microsoft.com/download>.

- Verify the install:
  ```bash
  dotnet --version    # should print 10.0.x
  ```

## Quick start

All commands below are run from this directory (`gsp_demo_dotnet/src/demos/`).

### Build all demos at once

```bash
# Using the solution file (recommended)
dotnet build demos.slnx -c Release

# Or using the convenience script
./build.sh
```

### Build a single demo

```bash
# Using dotnet directly
dotnet build checksyntax/demos.checksyntax.csproj -c Release

# Or using the convenience script
./build.sh checksyntax
```

### Run a demo

```bash
# Check SQL syntax (uses a built-in sample if no file is given)
./build.sh run checksyntax /t oracle

# Format a SQL file
./build.sh run formatsql /t oracle /f /path/to/query.sql

# Or use dotnet run directly
dotnet run --project formatsql/demos.formatsql.csproj -c Release -- /t oracle /f /path/to/query.sql
```

Most demos accept `/t <vendor>` to select the SQL dialect. Supported vendors:
`oracle`, `mssql`, `mysql`, `postgresql`, `db2`, `sybase`, `teradata`,
`hive`, `impala`, `greenplum`, `redshift`, `snowflake`, `informix`,
`netezza`, `mdx`.

### Clean build output

```bash
./build.sh clean
```

## Available demos

Run `./build.sh list` to see this table in the terminal.

| Demo | What it does |
|------|-------------|
| **checksyntax** | Parse SQL and report syntax errors |
| **formatsql** | Pretty-print / reformat SQL |
| **gettablecolumns** | Extract table and column names from queries |
| **columnImpact** | Trace column-level lineage through SELECT statements |
| **dlineage** | Full data lineage analysis |
| **dlineageRelation** | Data lineage relation extraction |
| **dataFlowAnalyzer** | Analyze data flow in SQL |
| **convertJoin** | Convert between old-style (+) and ANSI JOIN syntax |
| **joinRelationAnalyze** | Analyze join relationships |
| **extractTableColumns** | Extract table/column pairs, including through subqueries |
| **expressionTraverser** | Walk the expression AST with a visitor |
| **visitors** | Visitor pattern demo / XML export of the AST |
| **analyzesp** | Analyze stored procedures |
| **removeColumn** | Rewrite SQL to remove specific columns |
| **removeCondition** | Rewrite SQL to remove WHERE-clause conditions |
| **removevars** | Strip bind variables from SQL |
| **tableColumnRename** | Rename table/column references in SQL |
| **listGSPInfo** | Print parser version and build info |

## Which database dialects are supported?

The dialects available are baked into the `gudusoft.gsqlparser` package these
demos resolve from nuget.org; the published package is built with all 15 turned
on. Rebuilding a demo cannot add or remove one.

Run the `listGSPInfo` demo to print exactly what your resolved package contains,
along with its version and whether it is the trial or full edition.

## Project structure

```
demos/
  demos.slnx              Solution file — covers all demos
  build.sh                Convenience build/run script
  readme.md               This file
  lib/                    Shared analysers, a class library (demos.lib)
  util/                   Shared helpers, a class library (demos.util)
  checksyntax/            Each demo is a standalone console app
  formatsql/
  ...
  dlineageCommon/         Shared library for dlineage and dlineageRelation
```

Each demo is an independent .NET console application that references the
`gudusoft.gsqlparser` package from nuget.org. `dotnet build` restores it
automatically; no library sources or manual downloads are required.

The version is set in one place, `Directory.Build.props` at the repository root:

```xml
<GspVersion>4.1.0.7</GspVersion>
```

Every demo references it as `Version="$(GspVersion)"`, so bumping that one
property moves the whole repository.

To build against the full edition instead of the trial on nuget.org, replace a
demo's `PackageReference` with a `Reference` whose `HintPath` points at the
full-edition DLL from <https://www.sqlparser.com/download.php>.

## Usage on Windows

### Install .NET 10 SDK

Download the installer from <https://dotnet.microsoft.com/download> and run it,
or use `winget` from a terminal:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Verify the install by opening a new **Command Prompt** or **PowerShell** window:

```powershell
dotnet --version
```

### Build and run from Command Prompt or PowerShell

All the `dotnet` commands shown in the Quick Start section work identically on
Windows — just use backslashes for paths and run from this directory
(`gsp_demo_dotnet\src\demos\`):

```powershell
# Build all demos
dotnet build demos.slnx -c Release

# Build a single demo
dotnet build checksyntax\demos.checksyntax.csproj -c Release

# Run a demo
dotnet run --project formatsql\demos.formatsql.csproj -c Release -- /t oracle /f C:\path\to\query.sql

# Run checksyntax with the built-in sample
dotnet run --project checksyntax\demos.checksyntax.csproj -c Release -- /t mssql
```

### Open in Visual Studio

Open `demos.slnx` directly in Visual Studio 2026 (18.0+), which is the first
release that supports `net10.0` projects. Visual Studio 2022 (17.14) can open
the `.slnx` format but cannot build these projects, because .NET 10 targeting
is not available there — use the `dotnet` CLI commands above instead.
Right-click any demo project in Solution Explorer
and choose **Set as Startup Project**, then press **F5** to run it. To pass
command-line arguments (e.g. `/t oracle /f ...`), go to
**Project > Properties > Debug > Command line arguments**.

For older Visual Studio versions that do not support `.slnx`, build from the
Developer Command Prompt using the `dotnet` commands above.

### Open in Visual Studio Code

Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
extension, then open this folder. VS Code will detect `demos.slnx` and show all
demo projects in the Solution Explorer panel. Use the built-in terminal
(**Ctrl+`**) to run `dotnet` commands.

## Building from the repo root

You can also build and run demos from the repository root using the full
solution:

```bash
# From the repo root (gsp_demo_dotnet/)
dotnet build gsp_demo_dotnet.slnx -c Release
dotnet run --project src/demos/formatsql/demos.formatsql.csproj \
  -c Release -- /t oracle /f path/to/query.sql
```

The root solution additionally covers the `tests/` projects, so
`dotnet test gsp_demo_dotnet.slnx -c Release` runs the test suites too.
