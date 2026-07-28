using System;
using System.IO;
using System.Text;
using gudusoft.gsqlparser.demos.lib;
using gudusoft.gsqlparser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gudusoft.gsqlparser.test.visitors
{
    [TestClass()]
    public class testXmlVisitor
    {
        /// <summary>
        /// Serialising a MERGE statement exercises most of xmlVisitor: a nested
        /// SELECT with a WHERE clause, an ON condition, and both the MATCHED and
        /// NOT MATCHED branches with an arithmetic expression in each.
        ///
        /// The expected output lives in result.xml, copied next to the test
        /// assembly at build time. If the visitor legitimately changes, run the
        /// test, inspect result.actual.xml written beside it, and copy it over
        /// result.xml once the diff has been reviewed.
        /// </summary>
        [TestMethod()]
        public void testToXML()
        {
            string sqltext = "MERGE INTO bonuses D\r\n" +
                        "   USING(SELECT employee_id, salary, department_id FROM employees\r\n" +
                        "   WHERE department_id = 80) S\r\n" +
                        "   ON(D.employee_id = S.employee_id)\r\n" +
                        "   WHEN MATCHED THEN UPDATE SET D.bonus = D.bonus + S.salary * .01\r\n" +
                        "   WHEN NOT MATCHED THEN INSERT(D.employee_id, D.bonus)\r\n" +
                        "   VALUES(S.employee_id, S.salary * 0.1);";

            TGSqlParser sqlparser = new TGSqlParser(EDbVendor.dbvoracle);
            sqlparser.sqltext = sqltext;
            Assert.AreEqual(0, sqlparser.parse(), "MERGE statement failed to parse: " + sqlparser.Errormessage);

            xmlVisitor xv2 = new xmlVisitor();
            xv2.run(sqlparser);

            string actual = xv2.FormattedXml;
            string baselinePath = Path.Combine(AppContext.BaseDirectory, "result.xml");
            Assert.IsTrue(File.Exists(baselinePath), "Baseline not copied to output: " + baselinePath);
            string expected = File.ReadAllText(baselinePath);

            // git may check result.xml out with either line ending, and the
            // visitor emits the platform's own, so compare on normalised text.
            if (Normalize(expected) != Normalize(actual))
            {
                string actualPath = Path.Combine(AppContext.BaseDirectory, "result.actual.xml");
                File.WriteAllText(actualPath, actual);
                Assert.Fail(
                    "xmlVisitor output no longer matches result.xml. Actual output written to "
                    + actualPath + Environment.NewLine
                    + FirstDifference(Normalize(expected), Normalize(actual)));
            }
        }

        private static string Normalize(string s) =>
            s.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');

        /// <summary>Point at the first differing line, so a failure is readable
        /// without diffing 9 KB of XML by hand.</summary>
        private static string FirstDifference(string expected, string actual)
        {
            string[] e = expected.Split('\n');
            string[] a = actual.Split('\n');
            for (int i = 0; i < Math.Max(e.Length, a.Length); i++)
            {
                string el = i < e.Length ? e[i] : "<end of file>";
                string al = i < a.Length ? a[i] : "<end of file>";
                if (el != al)
                {
                    return "First difference at line " + (i + 1) + ":" + Environment.NewLine
                         + "  expected: " + el + Environment.NewLine
                         + "  actual:   " + al;
                }
            }
            return string.Empty;
        }
    }
}
