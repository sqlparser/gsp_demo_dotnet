using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace gudusoft.gsqlparser.demos.dlineage
{
    // Retains the pre-merge DDL and directional operations for dlineageCommon users.
    internal static class LegacyLineage
    {
        internal static int Run(CommandLine command)
        {
            var files = command.Has("/f") ? new[] { new FileInfo(command.Input) } :
                Directory.EnumerateFiles(command.Input, "*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.Ordinal).Select(f => new FileInfo(f)).ToArray();
            var analyzer = new DlineageCommon(files, command.Vendor, command.Has("/strict") || command.Has("/s"), false);
            if (command.Has("/ddl"))
            {
                // The old helper prepends a second declaration/DTD to XmlSerializer output.
                // Serialize its existing schema objects with one document root instead.
                var serializer = new XmlSerializer(typeof(model.ddl.schema.database));
                var schemas = analyzer.DataMetaInfos;
                var databases = schemas.Select(schema =>
                {
                    using var buffer = new StringWriter();
                    serializer.Serialize(buffer, schema);
                    return XDocument.Parse(buffer.ToString()).Root;
                }).ToArray();
                XElement root = databases.Length == 1 ? databases[0] : new XElement("dbSchema", databases);
                Console.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                Console.WriteLine(root);
            }
            else if (command.Has("/fo") || command.Has("/b"))
            {
                var errors = new StringBuilder();
                var relations = analyzer.collectDlineageRelations(analyzer.generateColumnImpact(errors));
                if (command.Has("/fo")) analyzer.forwardAnalyze(command.Value("/fo"), relations);
                if (command.Has("/b")) analyzer.backwardAnalyze(command.Value("/b"), relations);
                if (errors.Length > 0) { Console.Error.WriteLine(errors); return 1; }
            }
            else analyzer.columnImpact();
            return 0;
        }
    }
}
