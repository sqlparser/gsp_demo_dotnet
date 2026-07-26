# extractTableColumns — Bulk-extract schema/table/column triples to CSV

Given one SQL file or a whole directory of them, emits a flat CSV listing
every `(schema, table, column, source_file)` touple discovered. Useful for
seeding a data catalog, feeding into downstream dependency tooling, or doing
quick-and-dirty "where is column X used?" greps.

Internally this is the DDL-aware front of `DlineageCommon` — it runs the
full lineage pipeline (DDL parser + view parser + procedure scanner) but
only emits the final table/column inventory, not the relationships.

## What it shows

- Using the shared `DlineageCommon` helper (`../dlineageCommon/`) to parse a
  script and populate `DataMetaInfos[]` — a database/table/column tree.
- Filtering out views, star-references (`*`), and the internal `CONSTANT`
  table used by the analyser.
- Writing CSV output suitable for bulk import.

## Build and run

```bash
dotnet build demos.extractTableColumns.csproj -c Release

# One file — writes <file>.out beside it
dotnet run --project demos.extractTableColumns.csproj -c Release -- /t oracle /f query.sql

# A directory — writes tableColumns.txt + error.txt into that dir (or /o <dir>)
dotnet run --project demos.extractTableColumns.csproj -c Release -- /t mssql /d ./scripts /o ./out
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Analyse a single SQL file. |
| `/d <dir>` | Analyse every `.sql` file in `<dir>` (recursive). Limited to the first 30 files per run. |
| `/o <dir>` | Output directory. Default is the input directory (for `/d`) or alongside the input file (for `/f`). |
| `/t <vendor>` | SQL dialect. Default `oracle`. |

### Output format

```
Schama,Table,Column,File
schema1,EMP,EMPNO,example.sql
schema1,EMP,ENAME,example.sql
,DEPT,DEPTNO,example.sql
```

(Schema left blank when unknown.)

## Build your own

Use this demo as the starting point for any "inventory every column in our
SQL codebase" task. To additionally capture *how* the columns flow between
tables, graduate to `dlineage/` (which emits table/column XML plus
relationship edges). For the minimum viable per-statement extraction
without the DDL/view pass, use `gettablecolumns/`.
