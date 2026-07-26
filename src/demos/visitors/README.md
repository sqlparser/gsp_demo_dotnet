# visitors — Visitor pattern: serialize AST to XML, search for functions

Two complementary demonstrations of the GSP **parse-tree visitor** API:

1. **`sample.cs` (entry point)** — parses a SQL script and runs `xmlVisitor`
   to emit the entire AST as an XML document. The XML is deep enough to
   faithfully round-trip every expression, statement, and clause type.
2. **`searchFunction.cs`** — a smaller visitor that walks only the function
   calls and stored-procedure invocations in a script (useful for "which
   built-ins are used?" audits).

The XML output ships with a companion XSL (`tree-view.xsl`) and CSS
(`tree-view.css`) so you can open the generated `.xml` in a browser and
explore the tree interactively.

## What it shows

- Deriving from `TParseTreeVisitor` and overriding `preVisit(...)` /
  `postVisit(...)` for specific node types (every AST class has a strongly
  typed overload — `preVisit(TFunctionCall)`, `preVisit(TSelectSqlStatement)`,
  etc.).
- Kicking off traversal: `sqlStatement.acceptChildren(visitor)`.
- Building XML while you traverse (see `lib/xmlVisitor.cs`).

## Build and run

```bash
dotnet build demos.toXML.csproj -c Release

# Built-in Oracle sample -> XML printed to stdout
dotnet run --project demos.toXML.csproj -c Release

# Your SQL file -> writes <file>.xml beside the input
dotnet run --project demos.toXML.csproj -c Release -- /t mssql /f query.sql
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Optional. SQL file to serialize. Output is `<path>.xml`. |
| `/t <vendor>` | SQL dialect. Default Oracle. |

### Viewing the generated XML

Open the produced `.xml` in a browser (the XML embeds a processing
instruction pointing at `tree-view.xsl`, which renders it as a collapsible
tree styled by `tree-view.css`).

## Core code pattern

```csharp
// XML export
class MyXmlVisitor : TParseTreeVisitor { /* override preVisit / postVisit */ }

var parser = new TGSqlParser(EDbVendor.dbvmssql);
parser.sqltext = sql;
parser.parse();

var visitor = new xmlVisitor();
visitor.run(parser);
Console.WriteLine(visitor.FormattedXml);
```

```csharp
// Function search
class FunctionVisitor : TParseTreeVisitor
{
    public override void preVisit(TFunctionCall node)
        => Console.WriteLine("function: " + node.FunctionName);

    public override void preVisit(TMssqlExecute stmt)
    {
        if (stmt.ExecType == TBaseType.metExecSp)
            Console.WriteLine("execute: " + stmt.ModuleName);
    }
}
// ...
for (int i = 0; i < parser.sqlstatements.size(); i++)
    parser.sqlstatements.get(i).acceptChildren(new FunctionVisitor());
```

The full `xmlVisitor` implementation is shared code in `../lib/xmlVisitor.cs`
— refer to it for a worked example of covering all ~80 GSP AST node types.

## Build your own

`TParseTreeVisitor` is the recommended way to walk *statements* (as opposed
to `IExpressionVisitor` in `expressionTraverser/`, which walks expressions
only). Use it for: AST-to-JSON / AST-to-GraphViz exporters, linters, impact
analysis, or any tool that needs uniform coverage of every node type.
