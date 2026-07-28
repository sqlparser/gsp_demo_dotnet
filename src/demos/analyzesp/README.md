# analyzesp — Static analysis of SQL Server stored procedures

Scans MSSQL stored procedures and extracts three kinds of information:

1. **Database object relations** — every table or procedure the SP reads,
   writes, creates, drops, or calls, plus the columns touched.
2. **Built-in function usage** — which built-ins the SP invokes, where,
   and in what kind of statement (SELECT, INSERT, UPDATE, …).
3. **Try/catch coverage** — whether each procedure wraps its body in a
   `BEGIN TRY / BEGIN CATCH` block.

All three outputs are CSV-like (configurable delimiter) so they can feed a
review spreadsheet or a compliance dashboard.

## What it shows

- Detecting `TMssqlCreateProcedure` and iterating its nested
  `Statements` list — including procedural wrappers like `TMssqlBlock`,
  `TMssqlIfElse`, `TMssqlDeclare`, and `TMssqlExecute`.
- Extracting read/write columns from `TSelectSqlStatement`,
  `TInsertSqlStatement`, `TUpdateSqlStatement`, `TDeleteSqlStatement`,
  `TCreateTableSqlStatement`, and `TDropTableSqlStatement`.
- Scanning `TSourceTokenList` and inspecting `EDbObjectType.function` to
  find built-in invocations, with line/column numbers for IDE integration.
- Reading `stmt.tables.getTable(i).LinkedColumns` and handling MSSQL's
  special `inserted` / `deleted` pseudo-tables in triggers.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/analyzesp/demos.analyzesp.csproj -c Release

# Relations only (default)
dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
  samples/mssql-proc-refresh.sql samples/mssql-proc-rollup.sql

# All three checks, tab-delimited, written to file
dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
  samples/mssql-proc-refresh.sql /a /o report.tsv

# Only built-in function audit
dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
  samples/mssql-proc-refresh.sql /f

# Only try/catch coverage
dotnet run --project src/demos/analyzesp/demos.analyzesp.csproj -c Release -- \
  samples/mssql-proc-refresh.sql samples/mssql-proc-rollup.sql /t  # /t here = try/catch (not vendor)
```

### Arguments

Positional args = one or more SQL script files.

| Flag | Description |
|------|-------------|
| `/a` | Run all three checks (relations + functions + try/catch). |
| `/r` | Check DB object relations. |
| `/f` | Check built-in function usage. |
| `/t` | Check try/catch coverage. (Note: `/t` with a value — e.g. `/t mssql` — also selects the SQL vendor via `Common.GetEDbVendor`.) |
| `/o <path>` | Write output to this file. |
| `/d <char>` | CSV delimiter character. Default `|`. |

## Output columns

**Relations**: `DB | Procedure | ObjectType | ObjectUsed | ObjectType | UsageType | Columns`

**Functions**: `File | Function | Line | Column | StatementType`

**Try/Catch**: `File | DB | Procedure | WithTryCatch`

## Build your own

Great template for MSSQL-specific compliance tooling: enforce that every
procedure has error handling, flag use of deprecated built-ins, or build
a dependency matrix of which procs touch which tables. The code is
demo-grade (positional args, hand-rolled CSV) but the analysis patterns
translate directly.

### One procedure per file

`analyzesp` attributes its findings per input file, so two procedures in a single
file are both credited to the last one. Pass each procedure as its own file, which
is why `samples/` ships the pair above rather than one combined script.
