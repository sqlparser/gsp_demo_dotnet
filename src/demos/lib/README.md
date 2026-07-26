# lib — Shared demo helpers

Reusable classes used by more than one demo. Not a standalone executable.

| File | What it provides | Used by |
|------|------------------|---------|
| `TGetTableColumn.cs` | Full "walk every table + every column + classify by clause" analyser, driven by flags (`showSummary`, `showDetail`, `showTreeStructure`, `showBySQLClause`, `showJoin`, `showColumnLocation`, …). Accepts an optional `IMetaDatabase` to validate column existence during `*`-expansion. | `gettablecolumns/` |
| `columnInClause.cs` | Helper for turning a `TObjectName.Location` (an `ESqlClause` value) into a human-readable clause label. | `gettablecolumns/` |
| `xmlVisitor.cs` | A full `TParseTreeVisitor` that serialises every GSP AST node type to XML. ~80 node types covered. Produces output that can be viewed with the `tree-view.xsl` stylesheet shipped in `../visitors/`. | `visitors/`, tests in `gsp_demo_dotnet/tests/visitors/` |

## Build

This directory has no csproj of its own — the `.cs` files are included
directly (via `<Compile Include="..\lib\*.cs" />`) by the demo csprojs
that need them. Just build the consuming demo:

```bash
dotnet build ../gettablecolumns/demos.gettablecolumns.csproj -c Release
dotnet build ../visitors/demos.toXML.csproj -c Release
```

## Build your own

The classes here are demo-grade (no NuGet packaging, no public namespace
guarantees) but they are a useful reference for three recurring patterns:

- **Metadata-aware analyser** (`TGetTableColumn`) — how to surface an
  `IMetaDatabase` hook from a walker so callers can supply their own
  catalog/schema data.
- **Clause classification** (`columnInClause`) — how to look up the
  SQL clause a given `TObjectName` belongs to.
- **Exhaustive visitor** (`xmlVisitor`) — the near-complete set of
  `TParseTreeVisitor.preVisit(...)` overloads you need if you want to
  cover every GSP AST node type.
