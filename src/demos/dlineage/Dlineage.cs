using System;
using System.IO;
using System.Text;

namespace gudusoft.gsqlparser.demos.dlineage
{
    /// <summary>Unified lineage command line; new analysis uses the library's Java-compatible API.</summary>
    public static class Dlineage
    {
        private static readonly object ConsoleLock = new object();
        public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

        // The library and retained demos use Console internally. Restore the host's writers
        // even after an exception; never close them when disposing an output file.
        public static int Run(string[] args, TextWriter output, TextWriter error)
        {
            lock (ConsoleLock)
            {
                TextWriter originalOut = Console.Out;
                TextWriter originalError = Console.Error;
                var diagnostics = new StringBuilder();
                using var capturedError = new StringWriter(diagnostics);
                StreamWriter fileWriter = null;
                CommandLine command = null;
                try
                {
                    Console.SetError(capturedError);
                    command = CommandLine.Parse(args);
                    if (command.Help) { CommandLine.WriteHelp(output); return 0; }
                    Console.SetOut(output);
                    if (command.Has("/version"))
                    {
#if GSP_DLINEAGE
                        Console.WriteLine("Version: " + global::gudusoft.gsqlparser.dlineage.DataFlowAnalyzer.getVersion());
                        Console.WriteLine("Release Date: " + global::gudusoft.gsqlparser.dlineage.DataFlowAnalyzer.getReleaseDate());
#else
                        Console.WriteLine("Parser version: " + typeof(TGSqlParser).Assembly.GetName().Version);
                        Console.WriteLine("Java-compatible dlineage API: unavailable. " + CommandLine.LibraryRequired);
#endif
                        return 0;
                    }
                    command.ValidateInput();
#if !GSP_DLINEAGE
                    if (!command.Legacy) throw new ArgumentException(CommandLine.LibraryRequired);
#endif
                    if (command.Value("/o") is string outputPath)
                    {
                        command.ValidateOutput(outputPath);
                        fileWriter = new StreamWriter(outputPath, false, new UTF8Encoding(false));
                        Console.SetOut(fileWriter);
                    }
                    int code = command.Legacy ? LegacyLineage.Run(command) : RunCurrent(command);
                    return code == 0 && command.Legacy && diagnostics.Length > 0 ? 1 : code;
                }
                catch (ArgumentException ex) { capturedError.WriteLine(ex.Message); return 2; }
                catch (Exception ex) { capturedError.WriteLine(ex.Message); return 1; }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                    fileWriter?.Dispose();
                    string message = diagnostics.ToString().Trim();
                    if (message.Length > 0)
                    {
                        error.WriteLine(message);
                        if (command?.Has("/log") == true)
                        {
                            try { File.WriteAllText("dataflow.log", message + Environment.NewLine, new UTF8Encoding(false)); }
                            catch (Exception ex) { error.WriteLine("Unable to write dataflow.log: " + ex.Message); }
                        }
                    }
                }
            }
        }

        private static int RunCurrent(CommandLine command)
        {
#if GSP_DLINEAGE
            return DataFlowAnalyzer.Run(command);
#else
            throw new ArgumentException(CommandLine.LibraryRequired);
#endif
        }
    }
}
