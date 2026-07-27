// This demo was written fresh for net10.0 rather than ported from Java, so it
// uses nullable reference annotations. The repo-wide default is Nullable=disable
// (the ported code predates the feature), hence the opt-in here.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.pp.para;
using gudusoft.gsqlparser.pp.stmtformatter;
using gudusoft.gsqlparser.demos.util;

namespace gudusoft.gsqlparser.demos.formatsql
{
    // CLI-only SQL formatter. Reads a .sql file (or a built-in sample) and emits
    // the reformatted SQL on stdout. HTML/RTF output and custom color schemes
    // were intentionally removed when migrating to net10.0 because they pulled
    // in System.Drawing + WPF which don't exist cross-platform. For rich output,
    // pipe this demo's stdout through an external highlighter.
    internal static class FormatSql
    {
        private const string SampleSql =
            "SELECT e.last_name AS name, " +
            "e.commission_pct comm, " +
            "e.salary * 12 \"Annual Salary\" " +
            "FROM scott.employees AS e " +
            "WHERE e.salary > 1000 or 1=1 " +
            "ORDER BY e.first_name, e.last_name;";

        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }

            EDbVendor vendor = Common.GetEDbVendor(args);

            var argList = new List<string>(args);

            string? inputFile = GetOptionValue(argList, "/f");
            string? outputFile = GetOptionValue(argList, "/o");

            TextWriter? writer = null;
            if (outputFile is not null)
            {
                try
                {
                    writer = new StreamWriter(outputFile);
                    Console.SetOut(writer);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Could not open output file: " + e.Message);
                    return 2;
                }
            }

            try
            {
                int exit = inputFile is null
                    ? FormatInline(vendor, SampleSql)
                    : FormatFile(vendor, inputFile);
                return exit;
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: formatsql [/f <script file>] [/t <database type>] [/o <output file path>]");
            Console.WriteLine("  /f  Optional. Path to a SQL script file. Omit to format a built-in sample.");
            Console.WriteLine("  /t  Optional. Database vendor: oracle, mysql, mssql, db2, postgresql, ...");
            Console.WriteLine("      Default is oracle. See Common.GetEDbVendor for the full list.");
            Console.WriteLine("  /o  Optional. Write formatted output to this file instead of stdout.");
        }

        private static string? GetOptionValue(List<string> argList, string option)
        {
            int i = argList.IndexOf(option);
            if (i == -1 || i + 1 >= argList.Count) return null;
            return argList[i + 1];
        }

        private static int FormatInline(EDbVendor vendor, string sql)
        {
            var parser = new TGSqlParser(vendor) { sqltext = sql };
            return EmitPlainFormat(parser);
        }

        private static int FormatFile(EDbVendor vendor, string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Input file not found: " + path);
                return 2;
            }
            var parser = new TGSqlParser(vendor) { sqlfilename = new FileInfo(path).FullName };
            return EmitPlainFormat(parser);
        }

        private static int EmitPlainFormat(TGSqlParser parser)
        {
            int ret = parser.parse();
            if (ret != 0)
            {
                Console.Error.WriteLine(parser.Errormessage);
                return ret;
            }

            GFmtOpt option = GFmtOptFactory.newInstance();
            string result = FormatterFactory.pp(parser, option);
            Console.WriteLine(result);
            return 0;
        }
    }
}
