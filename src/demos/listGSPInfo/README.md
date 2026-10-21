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

Every `EDbVendor` is listed, with `(not supported)` marking the ones this build
cannot parse. The summary line follows:

```text
dbvdb2
dbvgreenplum
dbvhive
...
dbvsnowflake
dbvbigquery(not supported)
dbvhana(not supported)
dbvdax(not supported)
Version:4.1.0.8, Release date:2026-7-30, Full version = False,
Db: 14/17, dbvdb2,dbvgreenplum,dbvhive,dbvimpala,dbvinformix,dbvmysql,dbvmssql,...
```

`Full version = False` and `14/17` are what the nuget.org **trial** package
reports; the full edition prints `True`. See the trial-limit section in the
[root README](../../../README.md).

## Build your own

Add this check to your application's startup banner — if a customer reports
"snowflake queries don't parse", running this first confirms whether Snowflake
support was compiled into the DLL they have.
