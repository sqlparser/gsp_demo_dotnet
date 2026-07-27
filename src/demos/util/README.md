# util — Demo utility classes

Cross-cutting helpers shared by most demos. Not a standalone executable.

| File | Purpose |
|------|---------|
| `common.cs` | `Common.GetEDbVendor(args)` — parses `/t <vendor>` from the command-line and maps it to an `EDbVendor`. Covers all 15 dialects: oracle, mssql, mysql, db2, postgresql, teradata, sybase, informix, netezza, hive, greenplum, redshift, snowflake, impala, mdx. Default is `oracle`. |
| `StringUtil.cs` | Small string helpers (ref-quality Java-port leftovers). |
| `Arrays.cs` | `Arrays.asList(...)`-style helpers. |
| `LinkedHashMap.cs` | Insertion-ordered dictionary (port of Java's `LinkedHashMap`). Used when the analysis output needs to preserve the order in which columns/tables were encountered. |
| `LinkedHashSet.cs` | Insertion-ordered set. Same rationale. |

## Build

This directory is a class library, `demos.util.csproj`. Demos consume it
with a `<ProjectReference>`; it used to be source-copied into each demo
via `<Compile Include="..\util\*.cs" />`, which meant adding a helper here
required editing every consumer.

It multi-targets `net10.0;netstandard2.0` because `demos.dlineageCommon`
is netstandard2.0-capable and references it. A net10.0-only library here
would silently break that project's netstandard2.0 target.

## Build your own

If you are building a production tool, prefer the stock .NET
`Dictionary<TKey, TValue>` + `List<T>` and parse CLI arguments with a
purpose-built library such as `System.CommandLine`. The types here exist
because large parts of the codebase were auto-translated from Java and
kept the Java idioms to minimise port churn.

`Common.GetEDbVendor` is still a useful copy-paste starter for quickly
mapping a `--vendor=oracle`-style flag to the right `EDbVendor`.
