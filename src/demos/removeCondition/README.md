# removeCondition — Rewrite SQL to drop WHERE conditions matching named placeholders

Given a SQL statement full of `$Placeholder$`-style variables in the WHERE
clause (a common convention in report-generation engines), walk the
expression tree and **remove every predicate whose right-hand side
references a placeholder you want to eliminate**. Then regenerate the SQL.

Useful for dynamic SQL builders that want to drop "optional filter"
conditions at runtime when the user leaves a form field blank.

## What it shows

- Parsing SQL and walking `TCustomSqlStatement` recursively (CTEs,
  subqueries, set operations, `RETURNING` clauses, table subqueries,
  function-call table sources).
- Using an `IExpressionVisitor` implementation (`ExpressionChecker.cs`) to
  inspect every predicate and decide whether it should be removed.
- Editing the AST and calling `.ToScript()` / `String` to re-emit SQL.
- Preserving leading/trailing parentheses that wrap the input (so you can
  safely pass a sub-SQL fragment).

## Build and run

```bash
dotnet build demos.removeCondition.csproj -c Release
```

The demo's `Main` is hard-wired as an example — it opens `C:\1.txt` and
removes predicates tagged `$Institute$` and `$Fund$`. Treat it as a code
sample: copy into your own application and drive the API directly.

## Core code pattern

```csharp
var conditionMap = new LinkedHashMap<string, string>();
conditionMap["Institute"] = "ShanXi University";   // key = placeholder name
conditionMap["Fund"]      = "Eclipse.org";

var remover = new removeCondition(sql, EDbVendor.dbvoracle, conditionMap);
string rewritten = remover.RemoveResult;
```

The keys of `conditionMap` are placeholder names (without the surrounding
`$ $`). Every WHERE predicate whose constant operand is `$<name>$` is
deleted; predicates outside the map are preserved.

## Build your own

Pair with a template engine: keep a catalogue of placeholders, build a
base query with *all* optional filters, then at runtime hand this demo
the subset of placeholders that were left blank, and it hands you back a
clean SQL statement with just the predicates you actually want.
