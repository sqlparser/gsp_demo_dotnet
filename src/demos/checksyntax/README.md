# Validate SQL syntax offline

Use this demo to check SQL inside a .NET application, command-line tool, IDE,
CI job, or ingestion service before the text reaches a database. GSP selects a
vendor grammar, parses the complete input in process, and returns either a
statement count or an actionable parser diagnostic. No database connection,
credentials, or database metadata are required.

`OfflineSyntaxCheck.Validate(...)` is the reusable application API. It creates
a fresh `TGSqlParser` for every request and returns a `ValidationResult` instead
of printing or exiting. `Main(...)` adds file loading, readable output, and exit
codes suitable for automation:

- `0`: the SQL is valid for the selected dialect;
- `1`: the parser rejected the SQL;
- `2`: the command arguments or input file are invalid.

## Run the built-in example

From the repository root:

```bash
dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release
```

The built-in Oracle query is accepted and the output confirms that validation
used no database connection.

## Validate checked-in SQL files

```bash
dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- \
  /f samples/checksyntax/valid-mssql.sql /t mssql
```

Change `valid-mssql.sql` to `invalid-mssql.sql` to see the rejection path and
the diagnostic returned by GSP. The process exits with code `1`, so the same
command can gate a CI step or deployment script.

## Integrate the validator

Call `OfflineSyntaxCheck.Validate(sql, vendor)` from your request handler,
editor service, text-to-SQL pipeline, migration scanner, or batch processor.
An accepted result proves that the complete text conforms to the selected GSP
grammar. It does not prove that referenced objects exist, that the caller is
authorized, or that executing the query is safe.
