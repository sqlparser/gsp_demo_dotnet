# tableColumnRename — Safely rename a column (or table) across a SQL script

Given a qualified column name (`schema.table.column`) and a new name, walk
the AST and rename *every* reference to the column — in the SELECT list,
WHERE, JOIN ON, GROUP BY, ORDER BY, subqueries, CTEs, set operations,
procedures, and triggers. Emits the rewritten SQL.

Unlike a naive string replace, this is AST-aware so it won't touch
string literals or identically named columns of unrelated tables.

## What it shows

- Disambiguating columns with a supplied metadata map
  (`IDictionary<string, IList<string>>` from lowercased `schema.table` to
  a list of that table's column names) — needed when a script uses
  unqualified column references that could match multiple tables.
- Walking a `TMssqlCreateProcedure`, `TSelectSqlStatement`, etc., and
  rewriting every `TObjectName` whose FQN matches the target.
- Counting the number of renamed occurrences and reporting success/failure.

## Build and run

```bash
dotnet build demos.tableColumnRename.csproj -c Release

# The demo's Main is a code sample: it runs a hard-wired MSSQL procedure
# through the renamer and prints the result.
dotnet run --project demos.tableColumnRename.csproj -c Release
```

## Core code pattern

```csharp
// Metadata hint: tell the renamer that dbo.tb_Seasons has a column
// MinimalRentalID (so the unqualified reference gets disambiguated to it).
var meta = new Dictionary<string, IList<string>>();
meta["dbo.tb_Seasons".ToLower()] = new List<string> { "MinimalRentalID".ToLower() };

var renamer = new tableColumnRename(EDbVendor.dbvmssql, sqltext, meta);
int renamed = renamer.renameColumn("dbo.tb_Seasons.MinimalRentalID",
                                   "MinimalRentalID_xx");

Console.WriteLine(renamer.msg);             // "renamed table occurs:N"
if (renamed > 0)
    Console.WriteLine(renamer.ModifiedText); // rewritten SQL
```

## Build your own

Template for schema-refactor tooling: propose a rename, run this renamer
over every SQL script in your repo, and commit the results. Combine with a
CI job and the `checksyntax` demo to verify that every rewritten file
still parses.

To rename a table rather than a column, remove the column suffix in the
call to `renameColumn` (`dbo.tb_Seasons` → `dbo.tb_Season_v2`).
