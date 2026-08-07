using System;
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.nodes;
using gudusoft.gsqlparser.stmt;

namespace gudusoft.gsqlparser.demos.modifySqlAst;

/// <summary>
/// Demonstrates a small, fail-closed SQL policy gate built on the GSP AST.
/// </summary>
public static class ModifySqlAst
{
    public const string SampleSql =
        "SELECT o.order_id,\n" +
        "       o.customer_id,\n" +
        "       o.total_amount,\n" +
        "       o.internal_note\n" +
        "FROM sales.orders o\n" +
        "WHERE o.status = 'OPEN' OR o.status = 'PENDING'\n" +
        "ORDER BY o.created_at DESC";

    private const string RestrictedColumn = "internal_note";
    private const string TenantPredicate = "o.tenant_id = ?";

    public static void Main(string[] args)
    {
        RewriteResult result = Rewrite(SampleSql, EDbVendor.dbvoracle);

        Console.WriteLine("Original SQL:");
        Console.WriteLine(result.OriginalSql);
        Console.WriteLine();
        Console.WriteLine("Policy decisions:");
        Console.WriteLine("- Accepted exactly one SELECT statement");
        Console.WriteLine("- Removed restricted projection: " + result.RemovedProjection);
        Console.WriteLine("- Added server-controlled tenant predicate: " + TenantPredicate);
        Console.WriteLine();
        Console.WriteLine("Rewritten SQL:");
        Console.WriteLine(result.RewrittenSql);
        Console.WriteLine();
        Console.WriteLine(
            "Validation: regenerated SQL parsed successfully as one SELECT statement.");
    }

    /// <summary>
    /// Applies the demo policy and returns SQL regenerated from the modified AST.
    /// Trusted application code must bind the tenant value when executing it.
    /// </summary>
    public static RewriteResult Rewrite(string sql, EDbVendor vendor)
    {
        TSelectSqlStatement select = ParseOneSelect(sql, vendor, "Input SQL");

        string removedProjection = RemoveRestrictedProjection(select);

        AddTenantPredicate(select, vendor);

        string rewrittenSql = select.ToScript();
        ParseOneSelect(rewrittenSql, vendor, "Regenerated SQL");

        return new RewriteResult(sql, rewrittenSql, removedProjection);
    }

    private static TSelectSqlStatement ParseOneSelect(
        string sql,
        EDbVendor vendor,
        string description)
    {
        TGSqlParser parser = new(vendor) { sqltext = sql };

        if (parser.parse() != 0)
        {
            throw new ArgumentException(description + " did not parse: " + parser.Errormessage);
        }

        if (parser.sqlstatements.size() != 1)
        {
            throw new ArgumentException(description + " must contain exactly one statement.");
        }

        if (parser.sqlstatements.get(0).sqlstatementtype != ESqlStatementType.sstselect)
        {
            throw new ArgumentException(description + " must be a SELECT statement.");
        }

        return (TSelectSqlStatement)parser.sqlstatements.get(0);
    }

    private static string RemoveRestrictedProjection(TSelectSqlStatement select)
    {
        TResultColumnList columns = select.ResultColumnList;
        string removedProjection = null;

        for (int index = columns.size() - 1; index >= 0; index--)
        {
            TResultColumn column = columns.getResultColumn(index);
            if (RestrictedColumn.Equals(column.ColumnNameOnly,
                    StringComparison.OrdinalIgnoreCase))
            {
                removedProjection ??= column.ToScript();
                columns.removeResultColumn(index);
            }
        }

        return removedProjection ?? "not present";
    }

    private static void AddTenantPredicate(TSelectSqlStatement select, EDbVendor vendor)
    {
        TGSqlParser expressionParser = new(vendor);
        TExpression tenantCondition = expressionParser.parseExpression(TenantPredicate);
        if (tenantCondition == null)
        {
            throw new InvalidOperationException("The configured tenant predicate did not parse.");
        }

        if (select.WhereClause?.Condition == null)
        {
            select.WhereClause = new TWhereClause { Condition = tenantCondition };
            return;
        }

        // Parenthesize the existing condition before adding AND. Without this
        // node, "A OR B" plus a tenant filter could become "A OR (B AND tenant)".
        TExpression originalCondition = select.WhereClause.Condition;
        TExpression parenthesized =
            new(EExpressionType.parenthesis_t, originalCondition, null);
        TExpression combined =
            new(EExpressionType.logical_and_t, parenthesized, tenantCondition);
        select.WhereClause.Condition = combined;
    }

    public sealed class RewriteResult
    {
        public RewriteResult(string originalSql, string rewrittenSql, string removedProjection)
        {
            OriginalSql = originalSql;
            RewrittenSql = rewrittenSql;
            RemovedProjection = removedProjection;
        }

        public string OriginalSql { get; }

        public string RewrittenSql { get; }

        public string RemovedProjection { get; }
    }
}
