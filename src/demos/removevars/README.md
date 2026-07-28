# removevars — Strip every bind-variable predicate from a SQL script

A close relative of `removeCondition/`, but simpler — instead of matching
named placeholders it removes **every WHERE predicate whose right-hand
side is a bind variable** (e.g. `:foo`, `@foo`, `?`). The rewritten SQL
preserves only predicates between real values / columns.

Handy for pre-parsing SQL before piping it into a tool that can't deal
with bind syntax — e.g. a planner/explainer, a syntax checker for a dialect
that uses different bind markers, or a rewriter that requires literals.

## What it shows

- The same AST-edit pipeline as `removeCondition`:
  - parse → walk every nested statement (CTE, subquery, set op, RETURNING)
  - run `ExpressionChecker : IExpressionVisitor` against each predicate
  - for predicates that match, remove them and call `ToScript()` /
    `String` to regenerate SQL
- `removeQuote` helper that tolerates SQL wrapped in extra parentheses.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/removevars/demos.removevars.csproj -c Release

# Rewrites the given file, prints the cleaned SQL to stdout (MSSQL vendor)
dotnet run --project src/demos/removevars/demos.removevars.csproj -c Release -- script.sql
```

### Arguments

Positional arg 1: SQL file path. The vendor is hard-coded to MSSQL in the
demo's `Main`; adjust the call site to pass `EDbVendor.dbvoracle` / etc.
if you need a different dialect.

## Core code pattern

```csharp
var remover = new removevars(new FileInfo("script.sql"), EDbVendor.dbvmssql);
string cleaned = remover.RemoveResult;
```

## Build your own

Reach for this when you have a pile of parameterised reporting SQL and
need to either (a) hand it to a tool that only understands concrete
queries, or (b) diff two variants where the only differences you care
about are structural (bind-variable churn would drown out the signal).
