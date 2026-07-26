# scriptWriter — Regenerate SQL from a (possibly modified) AST

The **scriptWriter** demo shows how to use GSP's
`gudusoft.gsqlparser.scriptWriter` namespace — `TScriptGenerator`,
`TScriptWriter`, and `TScriptGeneratorVisitor` — to walk a parsed AST and
re-emit clean SQL, optionally after you have mutated some of the nodes.

This is the foundation of every "modify SQL, keep it valid" workflow in
GSP — including the `removeColumn`, `removeCondition`, `removevars`, and
`tableColumnRename` demos, which all use `ToScript()` (a thin wrapper
around the script writer) at their output step.

## Where the code lives

The working source for this demo lives in the demo-tests tree rather than
under `src/demos/`:

```
gsp_demo_dotnet/tests/scriptWriter/
```

See that directory's project and tests for runnable examples. The tests
themselves exercise round-trip (parse → regenerate) and mutation (parse →
modify AST → regenerate) scenarios across every major statement type.

## Core API

```csharp
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.scriptWriter;

var parser = new TGSqlParser(EDbVendor.dbvoracle);
parser.sqltext = sql;
parser.parse();

// Simple: ask any statement to emit itself
string regenerated = parser.sqlstatements.get(0).ToScript();

// Or drive the writer directly for finer control
var writer = new TScriptWriter();
var gen    = new TScriptGenerator(writer);
parser.sqlstatements.get(0).accept(new TScriptGeneratorVisitor(gen));
string output = writer.ToString();
```

After modifying AST nodes (adding/removing columns, rewriting expressions,
renaming objects, …), calling `ToScript()` on the enclosing statement
regenerates the SQL reflecting the changes.

## Build your own

Use the script writer any time you need to:

- Emit SQL that is not literally the original text (e.g. after an AST
  edit, or after parsing a canonical form and emitting a vendor-specific
  one).
- Build vendor-to-vendor translators — parse with one `EDbVendor`, walk
  the tree, regenerate with a target `EDbVendor` whose AST is compatible.
- Pretty-print a SQL fragment that is not a top-level statement
  (a single expression, a single WHERE clause, etc.).

For whole-statement formatting with casing / indentation options, prefer
the `formatsql` demo's `FormatterFactory.pp(...)` API — it is built on
top of the script writer and adds layout controls.
