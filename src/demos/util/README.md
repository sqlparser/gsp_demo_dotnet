# util — Demo utility classes

Cross-cutting helpers shared by most demos. Not a standalone executable.

| File | Purpose |
|------|---------|
| `common.cs` | `Common.GetEDbVendor(args)` — parses `/t <vendor>` from the command-line and maps it to an `EDbVendor` (oracle, mssql, mysql, db2, postgresql, teradata, sybase, informix, netezza, hive, greenplum, redshift, mdx). Default is `oracle`. |
| `StringUtil.cs` | Small string helpers (ref-quality Java-port leftovers). |
| `Arrays.cs` | `Arrays.asList(...)`-style helpers. |
| `LinkedHashMap.cs` | Insertion-ordered dictionary (port of Java's `LinkedHashMap`). Used when the analysis output needs to preserve the order in which columns/tables were encountered. |
| `LinkedHashSet.cs` | Insertion-ordered set. Same rationale. |

## Build

This directory has no csproj of its own — the `.cs` files are included
into each demo csproj via `<Compile Include="..\util\*.cs" />`. Build the
consuming demo to pull them in.

## Build your own

If you are building a production tool, prefer the stock .NET
`Dictionary<TKey, TValue>` + `List<T>` and parse CLI arguments with a
purpose-built library such as `System.CommandLine`. The types here exist
because large parts of the codebase were auto-translated from Java and
kept the Java idioms to minimise port churn.

`Common.GetEDbVendor` is still a useful copy-paste starter for quickly
mapping a `--vendor=oracle`-style flag to the right `EDbVendor`.
