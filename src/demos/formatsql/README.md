# formatsql — Pretty-print / reformat SQL

A minimal CLI SQL formatter. Reads a SQL script (or a built-in sample) and
emits a reformatted version on stdout.

## What it shows

- Parsing SQL with `TGSqlParser`.
- Configuring formatting options via `GFmtOpt` / `GFmtOptFactory`.
- Running the pretty-printer with `FormatterFactory.pp(parser, option)`.
- Dialect-aware formatting (the formatter respects the vendor's keyword
  casing and quote rules).

## Build and run

```bash
# Build
dotnet build demos.formatsql.csproj -c Release

# Run against a SQL file (reformatted output goes to stdout)
dotnet run --project demos.formatsql.csproj -c Release -- /t oracle /f query.sql

# Write output to a file
dotnet run --project demos.formatsql.csproj -c Release -- /t mssql /f query.sql /o formatted.sql

# No file? Prints usage (omit /f to format the built-in sample).
dotnet run --project demos.formatsql.csproj -c Release -- /f query.sql
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Path to a SQL script file. Required (otherwise usage is printed). |
| `/t <vendor>` | SQL dialect. Default `oracle`. |
| `/o <path>` | Optional. Write the formatted SQL to this file instead of stdout. |

## Core code pattern

```csharp
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.pp.para;
using gudusoft.gsqlparser.pp.stmtformatter;

var parser = new TGSqlParser(EDbVendor.dbvoracle);
parser.sqlfilename = path;
if (parser.parse() != 0) { Console.Error.WriteLine(parser.Errormessage); return; }

GFmtOpt option = GFmtOptFactory.newInstance();
// Tweak option.caseKeyword, option.caseIdentifier, option.indentSpaces, etc.
string formatted = FormatterFactory.pp(parser, option);
Console.WriteLine(formatted);
```

## Build your own

`GFmtOpt` exposes dozens of settings (keyword case, identifier case, comma
placement, line width, indent width, align-on-equals, etc.). The defaults
give reasonable output; skim `GFmtOpt` via IntelliSense / IDE autocomplete
to customise the house style for your project.

Note: HTML / RTF / coloured output was intentionally dropped when the demo
was ported to .NET 10 cross-platform. For syntax-highlighted output, pipe
this demo's stdout through an external highlighter such as `bat` or `pygments`.
