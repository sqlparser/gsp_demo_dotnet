# listGSPInfo — Print parser version and enabled dialects

Enumerates every `EDbVendor` value, tries to instantiate a `TGSqlParser` for
each one, and reports which dialects are actually compiled into the DLL you
are linking against. Also prints the library `versionId`, `releaseDate`, and
whether the build is a full / trial edition.

Useful when diagnosing "is this build of `gudusoft.gsqlparser.dll` the full
one?" — the library honours per-database conditional compilation
(`/p:includeOracle=true`, etc.) so a stripped build will fail for disabled
vendors.

## What it shows

- `TBaseType.versionId`, `TBaseType.releaseDate`, `TBaseType.full_edition`.
- Iterating over `EDbVendor` with `Enum.GetValues(typeof(EDbVendor))`.
- Probing support for a vendor by running a trivial `select 2 from t` parse.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/listGSPInfo/demos.listGSPInfo.csproj -c Release
dotnet run --project src/demos/listGSPInfo/demos.listGSPInfo.csproj -c Release
```

No arguments.

## Sample output

```
dbvoracle
dbvmssql
dbvmysql
...
Version:3.5.0.2, Release date:2026-04-14, Full version = True,
Db: 14/14, dbvoracle,dbvmssql,...
```

## Build your own

Add this check to your application's startup banner — if a customer reports
"snowflake queries don't parse", running this first confirms whether Snowflake
support was compiled into the DLL they have.
