# lib — Shared demo helpers

Reusable classes used by more than one demo. Not a standalone executable.

| File | What it provides | Used by |
|------|------------------|---------|
| `TGetTableColumn.cs` | Full "walk every table + every column + classify by clause" analyser, driven by flags (`showSummary`, `showDetail`, `showTreeStructure`, `showBySQLClause`, `showJoin`, `showColumnLocation`, …). Accepts an optional `IMetaDatabase` to validate column existence during `*`-expansion. | `gettablecolumns/` |
| `columnInClause.cs` | Helper for turning a `TObjectName.Location` (an `ESqlClause` value) into a human-readable clause label. | `gettablecolumns/` |
| `xmlVisitor.cs` | A full `TParseTreeVisitor` that serialises every GSP AST node type to XML. ~80 node types covered. Produces output that can be viewed with the `tree-view.xsl` stylesheet shipped in `../visitors/`. | `visitors/`, `tests/visitors/` |
| `joinRelationAnalyze.cs` | Join-relation analyser (`JoinCondition`, `joinConditonsInExpr`). Called by `TGetTableColumn` when `showJoin` is set. Lives here rather than in `gettablecolumns/` — see the comment at the top of the file for why it keeps that namespace. | `TGetTableColumn.cs` |

## Build

This directory is a class library, `demos.lib.csproj`, referenced by the
demos and tests that need it. It targets `net10.0` only — unlike
`demos.util`, nothing netstandard2.0 consumes it.

```bash
dotnet build demos.lib.csproj -c Release
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
