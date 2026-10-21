using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gudusoft.gsqlparser.demos.dlineage
{
    internal sealed class CommandLine
    {
        internal const string LibraryRequired = "This command requires the current gudusoft.gsqlparser dlineage API. " +
            "Build with -p:GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll (current local build), " +
            "or use /legacy for the historical DlineageCommon commands. See src/demos/dlineage/readme.md.";
        private static readonly HashSet<string> ValueOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/f", "/d", "/t", "/o", "/env", "/delimiter", "/defaultDatabase", "/defaultSchema",
            "/showResultSetTypes", "/removeResultSetTypes", "/filterRelationTypes", "/simpleShowRelationTypes", "/fo", "/b"
        };
        private static readonly HashSet<string> Switches = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/s", "/i", "/j", "/n", "/topselectlist", "/withTemporaryTable", "/showVariable", "/showCursor",
            "/showSynonym", "/removeVariable", "/removeCursor", "/removeUnusedSynonym", "/if", "/ic", "/lof",
            "/traceView", "/text", "/json", "/graph", "/stat", "/tableLineage", "/csv", "/csv-simple", "/log",
            "/transform", "/coor", "/showImplicitSchema", "/showConstant", "/showER",
            "/treatArgumentsInCountFunctionAsDirectDataflow", "/showCaseWhenAsIndirect", "/lv", "/traceTablePosition",
            "/version", "/legacy", "/ddl", "/strict"
        };
        private readonly Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal bool Help { get; private set; }
        internal bool Legacy => Has("/legacy") || Has("/fo") || Has("/b") || Has("/ddl");
        internal string Input => Value("/f") ?? Value("/d");
        internal EDbVendor Vendor { get; private set; } = EDbVendor.dbvoracle;
        internal bool Has(string name) => options.ContainsKey(name);
        internal string Value(string name) => options.TryGetValue(name, out string value) ? value : null;
        internal string[] List(string name) => Value(name)?.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        internal static CommandLine Parse(string[] args)
        {
            var command = new CommandLine();
            if (args.Length == 0 || args.Any(a => a == "/?" || a.Equals("/help", StringComparison.OrdinalIgnoreCase) || a == "--help"))
            { command.Help = true; return command; }
            for (int i = 0; i < args.Length; i++)
            {
                string name = args[i];
                if (name.Equals("/delimeter", StringComparison.OrdinalIgnoreCase)) name = "/delimiter";
                if (!ValueOptions.Contains(name) && !Switches.Contains(name))
                    throw new ArgumentException("Unknown option: " + name + ". Use /help.");
                if (command.Has(name)) throw new ArgumentException("Repeated option: " + name);
                string value = null;
                if (ValueOptions.Contains(name))
                {
                    if (++i >= args.Length || ValueOptions.Contains(args[i]) || Switches.Contains(args[i]))
                        throw new ArgumentException("Missing value for " + name);
                    value = args[i];
                    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Empty value for " + name);
                }
                command.options.Add(name, value);
            }
            string dialect = command.Value("/t") ?? "oracle";
            if (dialect.Equals("sqlserver", StringComparison.OrdinalIgnoreCase)) dialect = "mssql";
            if (dialect.Equals("postgres", StringComparison.OrdinalIgnoreCase)) dialect = "postgresql";
            if (!Enum.TryParse("dbv" + dialect, true, out EDbVendor vendor) || !Enum.IsDefined(typeof(EDbVendor), vendor))
                throw new ArgumentException("Unknown SQL dialect: " + dialect);
            command.Vendor = vendor;
            return command;
        }

        internal void ValidateInput()
        {
            if (Has("/f") == Has("/d")) throw new ArgumentException("Specify exactly one of /f <SQL file> or /d <SQL directory>.");
            if (Has("/f") && !File.Exists(Input)) throw new ArgumentException("Not a valid file: " + Input);
            if (Has("/d") && !Directory.Exists(Input)) throw new ArgumentException("Not a valid directory: " + Input);
            if (Has("/env") && !File.Exists(Value("/env"))) throw new ArgumentException("Not a valid metadata file: " + Value("/env"));
            if (!Legacy && Vendor != EDbVendor.dbvoracle && Vendor != EDbVendor.dbvmssql &&
                Vendor != EDbVendor.dbvmysql && Vendor != EDbVendor.dbvpostgresql)
                throw new ArgumentException("The current .NET dlineage implementation supports oracle, mssql/sqlserver, mysql and postgresql/postgres. Requested: " + Vendor);
            if (!Legacy && Has("/strict")) throw new ArgumentException("/strict applies to /legacy, /fo, /b or /ddl only; /s means simplified output in the current API.");
            if (Legacy)
            {
                string[] allowed = { "/f", "/d", "/t", "/o", "/log", "/s", "/strict", "/legacy", "/ddl", "/fo", "/b" };
                string unsupported = options.Keys.FirstOrDefault(k => !allowed.Contains(k, StringComparer.OrdinalIgnoreCase));
                if (unsupported != null) throw new ArgumentException(unsupported + " cannot be combined with legacy lineage commands.");
                if (Has("/ddl") && (Has("/fo") || Has("/b"))) throw new ArgumentException("/ddl cannot be combined with /fo or /b.");
            }
            foreach (string name in new[] { "/showResultSetTypes", "/removeResultSetTypes", "/filterRelationTypes", "/simpleShowRelationTypes" })
                if (Has(name) && List(name).Length == 0) throw new ArgumentException("Empty list for " + name);
        }

        internal void ValidateOutput(string path)
        {
            string output = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (Has("/f") && output.Equals(Path.GetFullPath(Input), comparison))
                throw new ArgumentException("Output must not overwrite the input SQL file.");
            if (Has("/d") && output.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Input)) + Path.DirectorySeparatorChar, comparison))
                throw new ArgumentException("Place /o outside the input directory so it cannot be analyzed as SQL.");
            if (Has("/env") && output.Equals(Path.GetFullPath(Value("/env")), comparison))
                throw new ArgumentException("Output must not overwrite the metadata file.");
        }

        internal static void WriteHelp(TextWriter writer)
        {
            writer.WriteLine("Usage: dlineage /f <SQL file> | /d <SQL directory> [/t oracle|mssql|mysql|postgresql] [options]");
            writer.WriteLine("/help, /?, --help     Show help. /version shows the library version.");
            writer.WriteLine("/f <file>, /d <dir>   Analyze a file or a directory tree; /t <vendor> defaults to oracle.");
            writer.WriteLine("/o <file>, /log       UTF-8 output file; diagnostics on stderr and optionally dataflow.log.");
            writer.WriteLine("/env <metadata.json>  Offline metadata (first environment).");
            writer.WriteLine("/json, /json /graph   JSON model or graph JSON. Default: XML.");
            writer.WriteLine("/csv, /csv-simple    Column CSV; /delimiter <text> sets its separator.");
            writer.WriteLine("/tableLineage        Table lineage (XML, JSON or /csv); /lv outputs visualization JSON.");
            writer.WriteLine("/s, /i, /topselectlist, /withTemporaryTable, /text  Simplify/intermediate-output options.");
            writer.WriteLine("/showResultSetTypes <types>, /simpleShowRelationTypes <types>  Comma-separated simple-mode filters.");
            writer.WriteLine("/showVariable, /showCursor, /showSynonym  Preserve these objects in simple mode.");
            writer.WriteLine("/removeResultSetTypes <types>, /removeVariable, /removeCursor, /removeUnusedSynonym, /if  Removal options.");
            writer.WriteLine("/filterRelationTypes <fdd,fdr,join,call,er>, /j  Relation filtering and joins.");
            writer.WriteLine("/n, /ic, /lof, /traceView, /traceTablePosition  Normalize, coordinates, orphan columns and tracing.");
            writer.WriteLine("/transform [/coor], /showConstant, /showER  Transform information, constants and ER graph.");
            writer.WriteLine("/treatArgumentsInCountFunctionAsDirectDataflow, /showCaseWhenAsIndirect  COUNT and CASE semantics.");
            writer.WriteLine("/defaultDatabase <name>, /defaultSchema <name>, /showImplicitSchema  Name resolution.");
            writer.WriteLine("/stat                Per-file/statement/line statistics; skips files larger than 10,000 bytes as Java does.");
            writer.WriteLine("/legacy, /fo <table.column>, /b <view.column>, /ddl  Retained DlineageCommon operations.");
            writer.WriteLine("/strict              Strict matching for legacy operations (/s retains that meaning there).");
#if !GSP_DLINEAGE
            writer.WriteLine(LibraryRequired);
#endif
            writer.WriteLine("English manual: src/demos/dlineage/readme.md");
            writer.WriteLine("Chinese manual: src/demos/dlineage/readme_zh.md");
        }
    }
}
