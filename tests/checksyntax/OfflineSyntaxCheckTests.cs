using System;
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.demos.checksyntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gudusoft.gsqlparser.test.checksyntax;

[TestClass]
public class OfflineSyntaxCheckTests
{
    [TestMethod]
    public void AcceptsValidSqlAndCountsStatements()
    {
        ValidationResult result = OfflineSyntaxCheck.Validate(
            OfflineSyntaxCheck.SampleSql, EDbVendor.dbvoracle);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(1, result.StatementCount);
        Assert.AreEqual(EDbVendor.dbvoracle, result.Vendor);
    }

    [TestMethod]
    public void RejectsInvalidSqlWithParserDiagnostic()
    {
        ValidationResult result = OfflineSyntaxCheck.Validate(
            "SELECT o.order_id,\nFROM sales.orders o;", EDbVendor.dbvoracle);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.AreEqual(0, result.StatementCount);
    }

    [TestMethod]
    public void DialectSelectionChangesTheResult()
    {
        const string sqlServerSql =
            "SELECT TOP 5 [order_id] FROM [sales].[orders] ORDER BY [created_at] DESC;";

        ValidationResult sqlServer = OfflineSyntaxCheck.Validate(
            sqlServerSql, EDbVendor.dbvmssql);
        ValidationResult oracle = OfflineSyntaxCheck.Validate(
            sqlServerSql, EDbVendor.dbvoracle);

        Assert.IsTrue(sqlServer.IsValid, sqlServer.ErrorMessage);
        Assert.IsFalse(oracle.IsValid,
            "Oracle grammar must reject SQL Server TOP/bracket syntax");
    }

    [TestMethod]
    public void ValidatesTheCompleteScript()
    {
        ValidationResult result = OfflineSyntaxCheck.Validate(
            "SELECT 1 FROM dual; SELECT 2 FROM dual;", EDbVendor.dbvoracle);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(2, result.StatementCount);
    }

    [TestMethod]
    public void RejectsEmptyInputBeforeParsing()
    {
        ValidationResult result = OfflineSyntaxCheck.Validate(
            "  \n  ", EDbVendor.dbvoracle);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("SQL input is empty.", result.ErrorMessage);
    }

    [TestMethod]
    public void RejectsUnknownDialect()
    {
        ArgumentException error = Assert.ThrowsException<ArgumentException>(() =>
            OfflineSyntaxCheck.ResolveVendor("not-a-database"));

        StringAssert.Contains(error.Message, "Unsupported database dialect");
    }
}
