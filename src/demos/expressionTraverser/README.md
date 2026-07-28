# expressionTraverser — Walk a SQL expression AST

Parses a SQL query, grabs the WHERE-clause condition, and walks its
expression tree in pre-order, in-order, and post-order — printing every
node and whether it is a leaf.

This is the minimum viable example of custom AST traversal in GSP.

## What it shows

- Reaching into a parsed statement: `sqlparser.sqlstatements.get(0)`,
  `select.WhereClause.Condition`.
- Implementing `IExpressionVisitor` — a single `exprVisit(node, isLeaf)`
  callback invoked at every expression node.
- The three traversal orders supported by `TExpression`:
  - `preOrderTraverse(visitor)` — visit parent before children
  - `inOrderTraverse(visitor)`  — left, parent, right (binary expressions)
  - `postOrderTraverse(visitor)` — visit children before parent
- Inspecting `TParseTreeNode.GetType()` / `.ToString()` to identify and
  render individual operators, operands, function calls, etc.

## Build and run

All commands are run from the repository root.

```bash
dotnet build src/demos/expressionTraverser/demos.expressionTraverser.csproj -c Release

# Built-in sample (Oracle, multi-operator WHERE clause)
dotnet run --project src/demos/expressionTraverser/demos.expressionTraverser.csproj -c Release

# Your own file
dotnet run --project src/demos/expressionTraverser/demos.expressionTraverser.csproj -c Release -- \
  /f samples/postgresql-nested.sql /t postgresql
```

### Arguments

| Flag | Description |
|------|-------------|
| `/f <path>` | Optional. SQL file. If omitted, uses a built-in Oracle sample. |
| `/t <vendor>` | Dialect. Default Oracle when no `/f` is given; default Oracle otherwise. |

## Core code pattern

```csharp
class MyVisitor : IExpressionVisitor
{
    public bool exprVisit(TParseTreeNode node, bool isLeaf)
    {
        Console.WriteLine((isLeaf ? "*" : " ") + node.GetType() + " " + node);
        return true; // return false to stop traversal
    }
}

// ...
var stmt = sqlparser.sqlstatements.get(0);
TExpression where = stmt.WhereClause.Condition;
where.preOrderTraverse(new MyVisitor());
```

## Build your own

`IExpressionVisitor` is the fastest way to plug custom logic into SQL
expressions — e.g. find every `OR` that is functionally a constant-true
(`OR 1=1`), collect every function call that is deterministic, or rewrite
operand names. For whole-statement traversal (not just expressions), see
`visitors/` and its `TParseTreeVisitor` pattern.
