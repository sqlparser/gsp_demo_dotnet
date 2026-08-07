using System;
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.demos.modifySqlAst;
using gudusoft.gsqlparser.nodes;
using gudusoft.gsqlparser.stmt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gudusoft.gsqlparser.test.modifySqlAst;

[TestClass]
public class ModifySqlAstTests
{
    [TestMethod]
    public void RewritesProjectionAndPreservesBooleanMeaning()
    {
        ModifySqlAst.RewriteResult result =
            ModifySqlAst.Rewrite(ModifySqlAst.SampleSql, EDbVendor.dbvoracle);

        TGSqlParser parser = new(EDbVendor.dbvoracle) { sqltext = result.RewrittenSql };
        Assert.AreEqual(0, parser.parse(), parser.Errormessage);
        Assert.AreEqual(1, parser.sqlstatements.size());

        TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
        TResultColumnList columns = select.ResultColumnList;
        Assert.AreEqual(3, columns.size());
        for (int index = 0; index < columns.size(); index++)
        {
            Assert.IsFalse("internal_note".Equals(
                columns.getResultColumn(index).ColumnNameOnly,
                StringComparison.OrdinalIgnoreCase));
        }

        TExpression condition = select.WhereClause.Condition;
        Assert.AreEqual(EExpressionType.logical_and_t, condition.ExpressionType);
        Assert.AreEqual(EExpressionType.parenthesis_t, condition.LeftOperand.ExpressionType);
        Assert.AreEqual(EExpressionType.logical_or_t,
            condition.LeftOperand.LeftOperand.ExpressionType);
        Assert.AreEqual("o.tenant_id = ?", condition.RightOperand.ToScript());
        Assert.AreEqual("o.internal_note", result.RemovedProjection);
    }

    [TestMethod]
    public void RejectsMultipleStatements()
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(() =>
            ModifySqlAst.Rewrite("SELECT 1 FROM dual; SELECT 2 FROM dual",
                EDbVendor.dbvoracle));

        StringAssert.Contains(error.Message, "exactly one statement");
    }

    [TestMethod]
    public void AddsWhereClauseWhenInputHasNone()
    {
        ModifySqlAst.RewriteResult result = ModifySqlAst.Rewrite(
            "SELECT o.order_id FROM sales.orders o", EDbVendor.dbvoracle);

        TGSqlParser parser = new(EDbVendor.dbvoracle) { sqltext = result.RewrittenSql };
        Assert.AreEqual(0, parser.parse(), parser.Errormessage);

        TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
        Assert.AreEqual("o.tenant_id = ?", select.WhereClause.Condition.ToScript());
    }

    [TestMethod]
    public void RemovesEveryRestrictedProjection()
    {
        ModifySqlAst.RewriteResult result = ModifySqlAst.Rewrite(
            "SELECT o.internal_note, o.order_id, o.internal_note AS note " +
            "FROM sales.orders o", EDbVendor.dbvoracle);

        TGSqlParser parser = new(EDbVendor.dbvoracle) { sqltext = result.RewrittenSql };
        Assert.AreEqual(0, parser.parse(), parser.Errormessage);

        TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
        Assert.AreEqual(1, select.ResultColumnList.size());
        Assert.AreEqual("order_id",
            select.ResultColumnList.getResultColumn(0).ColumnNameOnly);
    }

    [TestMethod]
    public void RejectsNonSelectStatement()
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(() =>
            ModifySqlAst.Rewrite("DELETE FROM sales.orders", EDbVendor.dbvoracle));

        StringAssert.Contains(error.Message, "must be a SELECT");
    }
}
