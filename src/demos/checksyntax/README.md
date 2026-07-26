# checksyntax — Validate SQL syntax

The simplest possible demonstration of the General SQL Parser: feed it a SQL
script and find out whether the grammar is well-formed for a given database
dialect. If it isn't, print the exact error location.

## What it shows

- Constructing `TGSqlParser` for a specific `EDbVendor`.
- Feeding the parser either an in-memory string (`sqltext`) or a file
  (`sqlfilename`).
- Calling `parse()` and checking the return code (`0` == success).
- Reading `Errormessage` when the parse fails.
- Reading `TBaseType.versionId` / `TBaseType.releaseDate` for the library
  build info.

This is the starting point for *anything* you build on top of GSP: every other
demo begins with the same three-line parse loop.

## Build and run

```bash
# Build from this directory
dotnet build demos.checksyntax.csproj -c Release

# Run with the built-in sample SQL (Oracle by default)
dotnet run --project demos.checksyntax.csproj -c Release -- /t oracle

# Run against a SQL file of your own
dotnet run --project demos.checksyntax.csproj -c Release -- /t mssql /f query.sql
```

### Arguments

| Flag | Description |
|------|-------------|
| `/t <vendor>` | SQL dialect: `oracle`, `mssql`, `mysql`, `db2`, `postgresql`, `hive`, `teradata`, `sybase`, `informix`, `netezza`, `greenplum`, `redshift`, `mdx`. Default `oracle`. |
| `/f <path>` | Optional. Path to a SQL file. If omitted, a built-in Oracle sample is parsed. |

## Core code pattern

```csharp
var sqlparser = new TGSqlParser(EDbVendor.dbvoracle);
sqlparser.sqltext = "SELECT * FROM emp WHERE dept_id = 10";
// or: sqlparser.sqlfilename = "/path/to/query.sql";

int ret = sqlparser.parse();
if (ret == 0)
{
    Console.WriteLine("Success!");
}
else
{
    Console.WriteLine("Syntax error: " + sqlparser.Errormessage);
}
```

## Build your own

Once `parse()` returns `0`, the parsed statement list is available at
`sqlparser.sqlstatements` — every other demo in this folder is a different
way of walking that list.
