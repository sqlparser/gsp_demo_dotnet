# samples — SQL to run the demos against

Most demos take `/f <file>`, but the repository used to ship no file to point
them at, so every example in the docs asked you to write one first. These are
that file. Each parses cleanly in the dialect named below; each was chosen to
exercise something specific rather than to be realistic.

Run from the repository root.

| File | Dialect | Demonstrates | Try it with |
|---|---|---|---|
| `oracle-outer-join.sql` | oracle | Proprietary `(+)` outer joins | `convertJoin` — rewrites them as ANSI `LEFT OUTER JOIN` |
| `oracle-lineage.sql` | oracle | Table → view → summary table chain | `dlineage`, `columnImpact` — traces each column to its source |
| `oracle-bind-variables.sql` | oracle | `$name$` placeholders in a WHERE clause | `removevars`, `removeCondition` |
| `mssql-report.sql` | mssql | `TOP`, a CTE, a windowed aggregate | `formatsql`, `gettablecolumns` |
| `postgresql-nested.sql` | postgresql | `::` casts, correlated subquery, derived table | `extractTableColumns` — resolves columns through subqueries |
| `snowflake-qualify.sql` | snowflake | `QUALIFY` filtering on a window function | `checksyntax` |

Each file repeats its own command in a header comment, so you can open one and
copy the line rather than come back here.

## Examples

```bash
# Reformat the T-SQL report
dotnet run --project src/demos/formatsql/demos.formatsql.csproj -c Release -- \
  /f samples/mssql-report.sql /t mssql

# Convert Oracle (+) joins to ANSI
dotnet run --project src/demos/convertJoin/demos.convertJoin.csproj -c Release -- \
  /f samples/oracle-outer-join.sql /t oracle
```

## Dialects are not interchangeable

`snowflake-qualify.sql` is the quickest way to prove the `/t` flag is doing
something. Parsed as Snowflake it succeeds; parsed as Oracle, whose grammar has
no `QUALIFY`, it fails:

```bash
dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- \
  /f samples/snowflake-qualify.sql /t snowflake   # Success!

dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- \
  /f samples/snowflake-qualify.sql /t oracle      # syntax error
```

## A note on the trial package

These files are all well under the trial build's 10,000-character limit. If you
point a demo at a large production script and it reports a parse failure with no
obvious cause, check the length before suspecting your SQL — see the trial-limit
section in the [root README](../README.md).
