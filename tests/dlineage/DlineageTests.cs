using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using gudusoft.gsqlparser.demos.dlineage;

namespace gudusoft.gsqlparser.test.dlineage
{
    [TestClass]
    [DoNotParallelize] // Console is process-wide, as are the retained demo's historical bindings.
    public class DlineageTests
    {
        private string temporary;
        private static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "samples", name);
        [TestInitialize] public void Initialize() { temporary = Path.Combine(Path.GetTempPath(), "dlineage-demo-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary); }
        [TestCleanup] public void Cleanup() { Directory.Delete(temporary, true); }

        private static (int Code, string Output, string Error) Run(params string[] args)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            int code = Dlineage.Run(args, output, error);
            return (code, output.ToString(), error.ToString());
        }
        private static (int Code, string Output, string Error) Analyze(string flags = "", string sample = "basic.sql", string vendor = "oracle") =>
            Run(new[] { "/f", Sample(sample), "/t", vendor }.Concat(flags.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray());

        [DataTestMethod]
        [DataRow("")][DataRow("/help")][DataRow("--help")][DataRow("/?")]
        public void HelpListsModernAndLegacyModes(string flag)
        {
            var run = flag.Length == 0 ? Run() : Run(flag);
            Assert.AreEqual(0, run.Code, run.Error);
            StringAssert.Contains(run.Output, "/csv-simple");
            StringAssert.Contains(run.Output, "/fo");
        }

        [DataTestMethod]
        [DataRow("/f")][DataRow("/unknown")][DataRow("/t", "not-a-dialect")]
        [DataRow("/fromdb")][DataRow("/exportonly")][DataRow("/metadataoutput")]
        public void InvalidArgumentsAreErrors(params string[] args)
        {
            var run = Run(args);
            Assert.AreEqual(2, run.Code);
            Assert.IsFalse(string.IsNullOrWhiteSpace(run.Error));
            Assert.AreEqual("", run.Output);
        }

        [TestMethod] public void VersionDoesNotRequireAnInput()
        {
            var run = Run("/version");
            Assert.AreEqual(0, run.Code, run.Error);
            StringAssert.Contains(run.Output.ToLowerInvariant(), "version");
        }

        [TestMethod] public void RejectsInputConflictsAndMissingFiles()
        {
            Assert.AreEqual(2, Run("/f", Sample("basic.sql"), "/d", temporary).Code);
            Assert.AreEqual(2, Run("/f", Path.Combine(temporary, "missing.sql")).Code);
            Assert.AreEqual(2, Run("/d", Path.Combine(temporary, "missing")).Code);
            Assert.AreEqual(2, Run("/f", Sample("basic.sql"), "/env", Path.Combine(temporary, "missing.json")).Code);
        }

        [TestMethod] public void RestoresConsoleWritersAfterSuccessfulAndFailingCalls()
        {
            var beforeOut = Console.Out; var beforeError = Console.Error;
            Run("/help"); Run("/unknown");
            Assert.AreSame(beforeOut, Console.Out); Assert.AreSame(beforeError, Console.Error);
            beforeOut.Write(""); beforeError.Write("");
        }

        [TestMethod] public void LegacyDdlStillProducesARealSchema()
        {
            var run = Analyze("/ddl /strict");
            Assert.AreEqual(0, run.Code, run.Error);
            XDocument.Parse(run.Output);
            StringAssert.Contains(run.Output.ToLowerInvariant(), "source_data");
            StringAssert.Contains(run.Output.ToLowerInvariant(), "target_data");
        }

        [TestMethod] public void LegacyForwardAndBackwardTracingRemainAvailable()
        {
            var forward = Analyze("/fo source_data.amount");
            var backward = Analyze("/b target_data.amount");
            Assert.AreEqual(0, forward.Code, forward.Error);
            Assert.AreEqual(0, backward.Code, backward.Error);
            StringAssert.Contains(forward.Output.ToLowerInvariant(), "source_data");
            StringAssert.Contains(backward.Output.ToLowerInvariant(), "target_data");
        }

        [TestMethod] public void LegacyDoesNotSilentlyIgnoreModernFlags()
        {
            Assert.AreEqual(2, Analyze("/ddl /json").Code);
            Assert.AreEqual(2, Analyze("/strict").Code);
        }

        [TestMethod] public void HistoricalDataFlowSourceApiIsStillUsableFromMergedAssembly()
        {
            var analyzer = new demos.dataFlowAnalyzer.DataFlowAnalyzer(File.ReadAllText(Sample("basic.sql")), EDbVendor.dbvoracle);
            var errors = new StringBuilder();
            var xml = XDocument.Parse(analyzer.generateDataFlow(errors));
            Assert.IsTrue(xml.Descendants("relation").Any());
        }

        [TestMethod] public void LegacyParseErrorsAreReportedAsFailure()
        {
            string path = Path.Combine(temporary, "invalid.sql"); File.WriteAllText(path, "SELECT FROM ;");
            var run = Run("/f", path, "/legacy");
            Assert.AreEqual(1, run.Code, run.Output);
            Assert.IsFalse(string.IsNullOrWhiteSpace(run.Error));
        }

#if GSP_DLINEAGE
        [DataTestMethod]
        [DataRow("oracle")][DataRow("mssql")][DataRow("mysql")][DataRow("postgresql")]
        [DataRow("sqlserver")][DataRow("postgres")]
        public void DefaultApiProducesSourceTargetLineageForAllFourDialects(string vendor)
        {
            var run = Analyze("", vendor: vendor);
            Assert.AreEqual(0, run.Code, run.Error);
            var xml = XDocument.Parse(run.Output);
            Assert.IsTrue(xml.Descendants("relationship").Any());
            StringAssert.Contains(run.Output.ToLowerInvariant(), "source_data");
            StringAssert.Contains(run.Output.ToLowerInvariant(), "target_data");
        }

        [DataTestMethod]
        [DataRow("/s")][DataRow("/i")][DataRow("/s /topselectlist")][DataRow("/if")]
        [DataRow("/s /showResultSetTypes cte,function")][DataRow("/s /showVariable /showCursor /showSynonym")]
        [DataRow("/removeResultSetTypes cte,function /removeVariable /removeCursor /removeUnusedSynonym")]
        [DataRow("/n /ic /lof /j")][DataRow("/transform /coor /showConstant /showER /traceTablePosition")]
        [DataRow("/showCaseWhenAsIndirect /treatArgumentsInCountFunctionAsDirectDataflow")]
        [DataRow("/defaultDatabase demo /defaultSchema sales /showImplicitSchema")]
        [DataRow("/s /simpleShowRelationTypes fdd,fdr")]
        public void JavaOptionCombinationsKeepRealLineage(string flags)
        {
            var run = Analyze(flags, "advanced-oracle.sql");
            Assert.AreEqual(0, run.Code, run.Error);
            var xml = XDocument.Parse(run.Output);
            Assert.IsTrue(xml.Descendants("relationship").Any(), flags);
        }

        [DataTestMethod]
        [DataRow("/json")][DataRow("/json /graph")][DataRow("/json /graph /showER")][DataRow("/lv")]
        [DataRow("/tableLineage /json")][DataRow("/tableLineage /json /graph")][DataRow("/tableLineage /lv")]
        public void JsonAndGraphOutputsParseAndContainLineage(string flags)
        {
            var run = Analyze(flags, "advanced-oracle.sql");
            Assert.AreEqual(0, run.Code, run.Error);
            using var json = JsonDocument.Parse(run.Output);
            StringAssert.Contains(run.Output.ToLowerInvariant(), "employees");
            Assert.IsTrue(json.RootElement.EnumerateObject().Any());
        }

        [DataTestMethod]
        [DataRow("/csv")][DataRow("/csv-simple")][DataRow("/tableLineage /csv")]
        public void CsvContainsSourceTargetEdges(string flags)
        {
            var run = Analyze(flags);
            Assert.AreEqual(0, run.Code, run.Error);
            StringAssert.Contains(run.Output.ToLowerInvariant(), "source_data");
            StringAssert.Contains(run.Output.ToLowerInvariant(), "target_data");
            if (flags == "/csv-simple") Assert.IsFalse(run.Output.Contains("RelationRows"));
        }

        [TestMethod] public void CsvDelimiterAndHistoricalTypoAreBothSupported()
        {
            var correct = Analyze("/csv /delimiter |");
            var alias = Analyze("/csv /delimeter |");
            Assert.AreEqual(0, correct.Code, correct.Error);
            Assert.AreEqual(correct.Output, alias.Output);
            StringAssert.Contains(correct.Output, "SOURCE_DB|SOURCE_SCHEMA");
        }

        [TestMethod] public void TextModeActuallyWritesTextAndRemovalStillApplies()
        {
            var run = Analyze("/s /text /if");
            Assert.AreEqual(0, run.Code, run.Error);
            StringAssert.Contains(run.Output, "depends on:");
            Assert.IsFalse(run.Output.TrimStart().StartsWith("<"));
        }

        [TestMethod] public void TableLevelXmlAndTraceViewHaveUsefulResults()
        {
            var tables = Analyze("/tableLineage");
            Assert.AreEqual(0, tables.Code, tables.Error);
            Assert.IsTrue(XDocument.Parse(tables.Output).Descendants("relationship").Any());
            var trace = Analyze("/traceView", "advanced-oracle.sql");
            Assert.AreEqual(0, trace.Code, trace.Error);
            StringAssert.Contains(trace.Output.ToLowerInvariant(), "employees");
        }

        [TestMethod] public void RelationFilterRestrictsTheActualEmittedEdges()
        {
            var run = Analyze("/filterRelationTypes fdd", "advanced-oracle.sql");
            Assert.AreEqual(0, run.Code, run.Error);
            var relations = XDocument.Parse(run.Output).Descendants("relationship").ToArray();
            Assert.IsTrue(relations.Length > 0);
            Assert.IsTrue(relations.All(r => (string)r.Attribute("type") == "fdd"));
        }

        [TestMethod] public void SimplificationAndFunctionRemovalChangeTheActualModel()
        {
            var full = XDocument.Parse(Analyze(sample: "advanced-oracle.sql").Output);
            var simple = XDocument.Parse(Analyze("/s", "advanced-oracle.sql").Output);
            var withoutFunctions = XDocument.Parse(Analyze("/if", "advanced-oracle.sql").Output);
            Assert.IsTrue(full.Descendants("resultset").Any(r => (string)r.Attribute("type") == "function"));
            Assert.IsFalse(simple.Descendants("resultset").Any());
            Assert.IsTrue(withoutFunctions.Descendants("resultset").Any());
            Assert.IsFalse(withoutFunctions.Descendants("resultset").Any(r => (string)r.Attribute("type") == "function"));
        }

        [TestMethod] public void TemporaryTableRetentionChangesSimplifiedLineage()
        {
            var retained = Analyze("/s /withTemporaryTable", "temporary-mssql.sql", "mssql");
            var removed = Analyze("/s", "temporary-mssql.sql", "mssql");
            Assert.AreEqual(0, retained.Code, retained.Error); Assert.AreEqual(0, removed.Code, removed.Error);
            Assert.IsTrue(XDocument.Parse(retained.Output).Descendants("table").Any(t => (string)t.Attribute("name") == "#stage"));
            Assert.IsFalse(XDocument.Parse(removed.Output).Descendants("table").Any(t => (string)t.Attribute("name") == "#stage"));
            Assert.IsTrue(XDocument.Parse(removed.Output).Descendants("relationship").Any());
        }

        [TestMethod] public void VariablesCursorsAndSynonymsAreProcessedAndCanBeRemoved()
        {
            var retained = Analyze("/s /showVariable /showCursor /showSynonym", "routine-oracle.sql");
            var removed = Analyze("/removeVariable /removeCursor", "routine-oracle.sql");
            Assert.AreEqual(0, retained.Code, retained.Error); Assert.AreEqual(0, removed.Code, removed.Error);
            var xml = XDocument.Parse(retained.Output);
            Assert.IsTrue(xml.Descendants("variable").Any(v => (string)v.Attribute("name") == "v_amount"));
            Assert.IsTrue(xml.Descendants("variable").Any(v => (string)v.Attribute("name") == "amounts"));
            Assert.IsTrue(xml.Descendants("table").Any(t => (string)t.Attribute("name") == "source_alias"));
            Assert.IsFalse(XDocument.Parse(removed.Output).Descendants("variable").Any());
        }

        [TestMethod] public void Utf8IdentifiersArePreservedAndConsoleSurvivesFileErrors()
        {
            string source = Path.Combine(temporary, "unicode.sql");
            File.WriteAllText(source, "CREATE TABLE \"来源\" (\"金额\" INT); SELECT \"金额\" FROM \"来源\";", new UTF8Encoding(false));
            var run = Run("/f", source, "/json");
            Assert.AreEqual(0, run.Code, run.Error);
            using var json = JsonDocument.Parse(run.Output);
            StringAssert.Contains(json.RootElement.ToString(), "来源");
            var beforeOut = Console.Out; var beforeError = Console.Error;
            Assert.AreEqual(1, Run("/f", source, "/o", Path.Combine(temporary, "missing", "out.json")).Code);
            Assert.AreSame(beforeOut, Console.Out); Assert.AreSame(beforeError, Console.Error);
        }

        [TestMethod] public void LogContainsDiagnosticsAndAnOutputFileIsNotMistakenForSuccess()
        {
            string input = Path.Combine(temporary, "bad.sql"), output = Path.Combine(temporary, "bad.xml");
            File.WriteAllText(input, "SELECT FROM ;");
            string original = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(temporary);
                var run = Run("/f", input, "/o", output, "/log");
                Assert.AreEqual(1, run.Code); Assert.AreEqual("", run.Output);
                Assert.IsTrue(File.Exists(output)); XDocument.Load(output);
                Assert.AreEqual(run.Error.Trim(), File.ReadAllText(Path.Combine(temporary, "dataflow.log")).Trim());
            }
            finally { Directory.SetCurrentDirectory(original); }
        }

        [TestMethod] public void MetadataResolvesAnAmbiguousColumnToItsOwningTable()
        {
            var run = Run("/f", Sample("metadata-query.sql"), "/t", "mssql", "/env", Sample("metadata.json"),
                "/defaultDatabase", "demo_db", "/defaultSchema", "dbo", "/s");
            Assert.AreEqual(0, run.Code, run.Error);
            var sources = XDocument.Parse(run.Output).Descendants("source").Where(s => ((string)s.Attribute("column"))?.Equals("ename", StringComparison.OrdinalIgnoreCase) == true).ToArray();
            Assert.IsTrue(sources.Length > 0, run.Output);
            Assert.IsTrue(sources.All(s => ((string)s.Attribute("parent_name"))?.EndsWith("emp", StringComparison.OrdinalIgnoreCase) == true));
        }

        [TestMethod] public void InvalidMetadataIsNotSilentlyIgnored()
        {
            string path = Path.Combine(temporary, "bad.json"); File.WriteAllText(path, "{}");
            var run = Run("/f", Sample("basic.sql"), "/env", path);
            Assert.AreEqual(2, run.Code, run.Error);
            StringAssert.Contains(run.Error, "metadata");
        }

        [TestMethod] public void DirectoryAnalysisRecursesAndIgnoresDocumentation()
        {
            Directory.CreateDirectory(Path.Combine(temporary, "nested"));
            File.Copy(Sample("basic.sql"), Path.Combine(temporary, "nested", "input.sql"));
            File.WriteAllText(Path.Combine(temporary, "README.md"), "this is not SQL");
            var run = Run("/d", temporary, "/json");
            Assert.AreEqual(0, run.Code, run.Error);
            using var json = JsonDocument.Parse(run.Output);
            Assert.IsTrue(json.RootElement.GetProperty("relationships").GetArrayLength() > 0);
        }

        [TestMethod] public void OutputFileIsUtf8AndDoesNotPolluteStdoutOrStderr()
        {
            string path = Path.Combine(temporary, "lineage.json");
            var before = Console.Out;
            var run = Run("/f", Sample("basic.sql"), "/json", "/o", path);
            Assert.AreEqual(0, run.Code, run.Error);
            Assert.AreEqual("", run.Output); Assert.AreEqual("", run.Error);
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Assert.IsTrue(json.RootElement.GetProperty("relationships").GetArrayLength() > 0);
            Assert.IsFalse(File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }));
            Assert.AreSame(before, Console.Out);
        }

        [TestMethod] public void OutputCannotOverwriteInputOrMetadataOrBecomeDirectoryInput()
        {
            string path = Path.Combine(temporary, "input.sql"); File.Copy(Sample("basic.sql"), path);
            byte[] before = File.ReadAllBytes(path);
            Assert.AreEqual(2, Run("/f", path, "/o", path).Code);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            Assert.AreEqual(2, Run("/d", temporary, "/o", Path.Combine(temporary, "out.xml")).Code);
            Assert.AreEqual(2, Run("/f", path, "/env", Sample("metadata.json"), "/o", Sample("metadata.json")).Code);
        }

        [TestMethod] public void SqlParseErrorsKeepDiagnosticOutputAndReturnFailure()
        {
            string path = Path.Combine(temporary, "invalid.sql"); File.WriteAllText(path, "SELECT FROM ;");
            var run = Run("/f", path);
            Assert.AreEqual(1, run.Code, run.Output);
            Assert.IsFalse(string.IsNullOrWhiteSpace(run.Error));
            XDocument.Parse(run.Output); // Partial/error model is still available to inspect.
        }

        [TestMethod] public void StatisticsCountsStatementsAndHandlesEmptyInputAndCap()
        {
            var normal = Analyze("/stat");
            Assert.AreEqual(0, normal.Code, normal.Error);
            StringAssert.Contains(normal.Output, "Total sql statements: 3");
            StringAssert.Contains(normal.Output, "Total sql lines: 3"); // Java listener/token convention, not wc -l.
            var empty = Run("/d", temporary, "/stat");
            Assert.AreEqual(0, empty.Code, empty.Error);
            StringAssert.Contains(empty.Output, "Average time per file: 0ms");
            File.WriteAllText(Path.Combine(temporary, "empty.sql"), "");
            Assert.AreEqual(0, Run("/d", temporary, "/stat").Code);
            string textFile = Path.Combine(temporary, "query.txt"); File.Copy(Sample("basic.sql"), textFile);
            var nonSql = Run("/f", textFile, "/stat");
            Assert.AreEqual(0, nonSql.Code, nonSql.Error);
            StringAssert.Contains(nonSql.Output, "Total sql files: 0");
            File.WriteAllText(Path.Combine(temporary, "large.sql"), "--" + new string('x', 10001));
            var large = Run("/d", temporary, "/stat");
            Assert.AreEqual(0, large.Code, large.Error);
            StringAssert.Contains(large.Output, "Files ignored in trial version: 1");
        }
#else
        [TestMethod] public void OldPackageReportsMissingModernApiInsteadOfSilentlyUsingAnotherEngine()
        {
            var run = Analyze("/json");
            Assert.AreEqual(2, run.Code);
            Assert.AreEqual("", run.Output);
            StringAssert.Contains(run.Error, "GspAssemblyPath");
        }
#endif
    }
}
