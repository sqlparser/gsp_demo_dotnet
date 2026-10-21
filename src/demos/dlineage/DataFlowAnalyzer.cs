#if GSP_DLINEAGE
// Translated orchestration: gsp_demo_java 65a2cef16409e99f834989478e683976032e003a,
// gudusoft/gsqlparser/demos/dlineage/DataFlowAnalyzer.java. Analysis stays in the library.
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using gudusoft.gsqlparser.dlineage.dataflow.listener;
using gudusoft.gsqlparser.dlineage.dataflow.model;
using gudusoft.gsqlparser.dlineage.graph;
using gudusoft.gsqlparser.dlineage.util;
using gudusoft.gsqlparser.runtime;
using gudusoft.gsqlparser.sqlenv;
using gudusoft.gsqlparser.sqlenv.parser;
using gudusoft.gsqlparser.util.json;
using Analyzer = gudusoft.gsqlparser.dlineage.DataFlowAnalyzer;
using SQLUtil = gudusoft.gsqlparser.util.SQLUtil;

namespace gudusoft.gsqlparser.demos.dlineage
{
    public static class DataFlowAnalyzer
    {
        public static int Main(string[] args) => Dlineage.Main(args);

        internal static int Run(CommandLine args)
        {
            TSqlEnv environment = null;
            if (args.Has("/env"))
            {
                // Passing the filename also supplies the base directory for sharded metadata.
                var environments = new TJSONSQLEnvParser(null, null, null).parseSQLEnv(args.Vendor, Path.GetFullPath(args.Value("/env")));
                if (environments == null || environments.Length == 0)
                    throw new ArgumentException("Unable to load metadata from " + args.Value("/env"));
                environment = environments[0];
            }
            if (args.Has("/stat")) return Statistics(args, environment);

            bool simple = args.Has("/s") || args.Has("/traceView");
            bool ignoreResultSets = args.Has("/i");
            bool tableLineage = args.Has("/tableLineage");
            bool csvSimple = args.Has("/csv-simple");
            bool csv = args.Has("/csv") || csvSimple;
            bool json = args.Has("/json");
            bool ignoreFunction = args.Has("/if");
            if (tableLineage) { simple = false; ignoreResultSets = false; }
            var analyzer = new Analyzer(new JFile(args.Input), args.Vendor, simple);
            try
            {
                if (environment != null) analyzer.setSqlEnv(environment);
                analyzer.setTransform(args.Has("/transform"));
                analyzer.setTransformCoordinate(args.Has("/transform") && args.Has("/coor"));
                analyzer.setShowJoin(args.Has("/j"));
                analyzer.setIgnoreRecordSet(ignoreResultSets);
                if (ignoreResultSets && !ignoreFunction) analyzer.setSimpleShowFunction(true);
                analyzer.setLinkOrphanColumnToFirstTable(args.Has("/lof"));
                analyzer.setIgnoreCoordinate(args.Has("/ic"));
                analyzer.setSimpleShowTopSelectResultSet(args.Has("/topselectlist"));
                analyzer.setShowImplicitSchema(args.Has("/showImplicitSchema"));
                analyzer.setIgnoreTemporaryTable(!args.Has("/withTemporaryTable"));
                if (simple) analyzer.setShowCallRelation(true);
                analyzer.setShowConstantTable(args.Has("/showConstant"));
                analyzer.getOption().setShowERDiagram(args.Has("/showER"));
                analyzer.setShowCountTableColumn(args.Has("/treatArgumentsInCountFunctionAsDirectDataflow"));
                analyzer.getOption().setSimpleShowVariable(args.Has("/showVariable"));
                analyzer.getOption().setSimpleShowCursor(args.Has("/showCursor"));
                analyzer.getOption().setSimpleShowSynonym(args.Has("/showSynonym"));
                analyzer.getOption().setIgnoreUnusedSynonym(args.Has("/removeUnusedSynonym"));
                analyzer.getOption().setShowCaseWhenAsDirect(!args.Has("/showCaseWhenAsIndirect"));
                analyzer.getOption().setNormalizeOutput(args.Has("/n"));
                analyzer.getOption().setTraceTablePosition(args.Has("/traceTablePosition"));
                if (args.Has("/defaultDatabase")) analyzer.getOption().setDefaultDatabase(args.Value("/defaultDatabase"));
                if (args.Has("/defaultSchema")) analyzer.getOption().setDefaultSchema(args.Value("/defaultSchema"));
                if (args.Has("/showResultSetTypes")) analyzer.getOption().showResultSetTypes(args.List("/showResultSetTypes"));
                if (args.Has("/filterRelationTypes")) analyzer.getOption().filterRelationTypes(args.List("/filterRelationTypes"));
                if (args.Has("/s") && args.Has("/simpleShowRelationTypes")) analyzer.getOption().setSimpleShowRelationTypes(args.List("/simpleShowRelationTypes"));
                // The Java CLI sets textFormat but then serializes XML anyway. Render the library's
                // text projection after removal (same formatter; see TextOutput provenance).
                bool text = simple && args.Has("/s") && args.Has("/text") && !json;
                if (text) analyzer.setTextFormat(true);

                DataflowRemoveOption remove = null;
                if (args.Has("/removeVariable") || args.Has("/removeCursor") || args.Has("/removeResultSetTypes") || ignoreFunction)
                {
                    remove = new DataflowRemoveOption();
                    remove.setRemoveVariable(args.Has("/removeVariable"));
                    remove.setRemoveCursor(args.Has("/removeCursor"));
                    if (args.Has("/removeResultSetTypes")) remove.setRemoveResultSetTypes(args.List("/removeResultSetTypes"));
                    if (ignoreFunction) remove.setRemoveResultSetTypes(ResultSetType.function);
                }

                analyzer.generateDataFlow();
                var dataflow = analyzer.getDataFlow();
                if (dataflow == null) throw new InvalidOperationException("The analyzer returned no data-flow model.");
                if (remove != null) dataflow = new DataflowRemoveHelper().remove(dataflow, args.Vendor, remove);
                string delimiter = args.Value("/delimiter") ?? (csvSimple ? ", " : ",");
                string result;
                if (tableLineage && csv) result = ProcessUtility.generateTableLevelLineageCsv(analyzer, dataflow, delimiter);
                else
                {
                    if (tableLineage) dataflow = ProcessUtility.generateTableLevelLineage(analyzer, dataflow);
                    if (args.Has("/lv")) result = new DataFlowGraphGenerator().genDlineageGraph(args.Vendor, true, dataflow);
                    else if (!tableLineage && csvSimple) result = ProcessUtility.generateColumnLevelLineageCsvSimple(analyzer, dataflow, delimiter);
                    else if (!tableLineage && csv) result = ProcessUtility.generateColumnLevelLineageCsv(analyzer, dataflow, delimiter);
                    else if (json)
                    {
                        var model = Analyzer.getSqlflowJSONModel(dataflow, args.Vendor);
                        result = args.Has("/graph")
                            ? (args.Has("/showER") ? new DataFlowGraphGenerator().genERGraph(args.Vendor, dataflow) : new DataFlowGraphGenerator().genDataflowGraph(model))
                            : JSON.toJSONString(model);
                    }
                    else if (!tableLineage && args.Has("/traceView")) result = analyzer.traceView();
                    else if (text) result = TextOutput(dataflow);
                    else result = XML2Model.saveXML(dataflow);
                }
                if (result == null) throw new InvalidOperationException("The selected formatter returned no output.");
                Console.WriteLine(result);
                return ReportErrors(analyzer);
            }
            finally { analyzer.dispose(); }
        }

        private static int ReportErrors(Analyzer analyzer)
        {
            var errors = analyzer.getErrorMessages();
            if (errors.Count == 0) return 0;
            Console.Error.WriteLine("Error log:");
            foreach (var error in errors) Console.Error.WriteLine(error.getErrorMessage());
            return 1;
        }

        // DataFlowAnalyzer.java#getTextOutput, library revision 4940a81cf671dee163239df60488fa716d02177e.
        // Java's package-private/C# internal formatter cannot be called by an external demo.
        // Preserve encounter order and de-duplication; this is presentation, not lineage inference.
        private static string TextOutput(global::gudusoft.gsqlparser.dlineage.dataflow.model.xml.dataflow model)
        {
            var buffer = new StringBuilder();
            if (model.getRelationships() == null) return buffer.ToString();
            foreach (var relation in model.getRelationships())
            {
                var target = relation.getTarget();
                var sources = relation.getSources();
                if (target == null || sources == null || sources.Count == 0) continue;
                buffer.Append(target.getColumn() ?? "null").Append(" depends on: ");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var columns = new List<string>();
                foreach (var source in sources)
                {
                    string column = source.getColumn();
                    if (!string.IsNullOrEmpty(source.getParent_name())) column = source.getParent_name() + "." + (column ?? "null");
                    if (seen.Add(column)) columns.Add(column ?? "null");
                }
                buffer.Append(string.Join(", ", columns)).Append('\n');
            }
            return buffer.ToString();
        }

        // Retains Java's warm-up, 10,000-byte statistics cap, listener counts and status codes.
        // Empty input/counters use zero averages rather than Java's division-by-zero failure.
        private static int Statistics(CommandLine args, TSqlEnv environment)
        {
            string[] paths = args.Has("/f") ? (args.Input.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ? new[] { args.Input } : Array.Empty<string>()) :
                Directory.EnumerateFiles(args.Input, "*", SearchOption.AllDirectories)
                    .Where(p => p.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            bool simple = !args.Has("/tableLineage") && (args.Has("/s") || args.Has("/traceView"));
            if (paths.Length > 0)
            {
                var warmup = new FileInfo(paths[0]).Length < 10240
                    ? new Analyzer(new JFile(paths[0]), args.Vendor, simple)
                    : new Analyzer("select a from b", args.Vendor, simple);
                try { warmup.generateDataFlow(); }
                finally { warmup.dispose(); }
            }
            long statements = 0, passed = 0, lines = 0, totalTime = 0;
            int ignored = 0, failedFiles = 0;
            foreach (string path in paths)
            {
                var file = new FileInfo(path);
                if (file.Length > 10000) { ignored++; continue; }
                Analyzer analyzer = null;
                try
                {
                    analyzer = new Analyzer(new JFile(path), args.Vendor, simple);
                    if (environment != null) analyzer.setSqlEnv(environment);
                    analyzer.setLinkOrphanColumnToFirstTable(false);
                    var listener = new StatisticsListener();
                    analyzer.setHandleListener(listener);
                    var watch = Stopwatch.StartNew();
                    analyzer.generateDataFlow();
                    watch.Stop();
                    bool errors = analyzer.getErrorMessages().Count > 0;
                    int status = !errors ? 1 : (analyzer.getDataFlow()?.getTables()?.Count > 0 ? 2 : 3);
                    Console.WriteLine($"status={status}, time={watch.ElapsedMilliseconds}ms, file={file.FullName}, length={file.Length}");
                    statements += listener.Statements;
                    passed += listener.Passed;
                    lines += listener.Lines;
                    totalTime += watch.ElapsedMilliseconds;
                    if (ReportErrors(analyzer) != 0) failedFiles++;
                }
                catch (Exception ex) { Console.Error.WriteLine(path + ": " + ex.Message); failedFiles++; }
                finally { analyzer?.dispose(); }
            }
            int processed = paths.Length - ignored;
            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine("Total sql files: " + paths.Length);
            Console.WriteLine("Total sql statements: " + statements);
            Console.WriteLine("Total sql lines: " + lines);
            Console.WriteLine("Average statements per file: " + Average(statements, processed));
            Console.WriteLine("Average lines per file: " + Average(lines, processed));
            Console.WriteLine("Average lines per sql statement: " + Average(lines, statements));
            Console.WriteLine($"Statement passed: {passed}, {Percent(passed, statements)}");
            Console.WriteLine($"Statement failed: {statements - passed}, {Percent(statements - passed, statements)}");
            Console.WriteLine("Total time escaped: " + totalTime + "ms");
            Console.WriteLine("Average time per file: " + Average(totalTime, processed) + "ms");
            Console.WriteLine("Average time per sql statement: " + Average(totalTime, passed) + "ms");
            Console.WriteLine("Files ignored in trial version: " + ignored);
            return failedFiles == 0 ? 0 : 1;
        }

        private static long Average(long count, long denominator) => denominator == 0 ? 0 : (long)Math.Floor(count / (double)denominator + 0.5);
        private static string Percent(long count, long denominator) => (denominator == 0 ? 0 : count * 100.0 / denominator).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private sealed class StatisticsListener : DataFlowHandleAdapter
        {
            internal long Statements, Passed, Lines;
            private long statementCount;
            public override void endAnalyzeStatment(TCustomSqlStatement statement) => statementCount++;
            public override void endAnalyzeDataFlow(TGSqlParser parser)
            {
                // Java accumulates the listener's count at each parser completion.
                Passed += statementCount;
                Statements += parser.sqlstatements.size();
                if (parser.sourcetokenlist.size() == 0) return;
                var token = parser.sourcetokenlist.get(parser.sourcetokenlist.size() - 1);
                // Java String.split discards trailing empty fields (including all fields
                // for a nonempty token made entirely of newlines), unlike string.Split.
                string[] parts = token.astext.Split('\n');
                int count = parts.Length;
                if (token.astext.Length > 0)
                    while (count > 0 && parts[count - 1].Length == 0) count--;
                Lines += token.lineNo + count - 1;
            }
        }
    }
}
#endif
