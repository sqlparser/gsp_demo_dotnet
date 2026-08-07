using System;
using System.Collections.Generic;
using System.IO;
using gudusoft.gsqlparser;

namespace gudusoft.gsqlparser.demos.checksyntax;

/// <summary>
/// Validates SQL syntax in the application process without a database connection.
/// </summary>
public static class OfflineSyntaxCheck
{
    public const string SampleSql =
        "SELECT o.order_id,\n" +
        "       o.customer_id,\n" +
        "       o.total_amount\n" +
        "FROM sales.orders o\n" +
        "WHERE o.status = 'OPEN'\n" +
        "ORDER BY o.created_at DESC";

    private static readonly IReadOnlyDictionary<string, EDbVendor> Vendors =
        new Dictionary<string, EDbVendor>(StringComparer.OrdinalIgnoreCase)
        {
            ["oracle"] = EDbVendor.dbvoracle,
            ["mssql"] = EDbVendor.dbvmssql,
            ["sqlserver"] = EDbVendor.dbvmssql,
            ["mysql"] = EDbVendor.dbvmysql,
            ["db2"] = EDbVendor.dbvdb2,
            ["postgresql"] = EDbVendor.dbvpostgresql,
            ["postgres"] = EDbVendor.dbvpostgresql,
            ["hive"] = EDbVendor.dbvhive,
            ["teradata"] = EDbVendor.dbvteradata,
            ["sybase"] = EDbVendor.dbvsybase,
            ["informix"] = EDbVendor.dbvinformix,
            ["netezza"] = EDbVendor.dbvnetezza,
            ["greenplum"] = EDbVendor.dbvgreenplum,
            ["redshift"] = EDbVendor.dbvredshift,
            ["snowflake"] = EDbVendor.dbvsnowflake,
            ["impala"] = EDbVendor.dbvimpala,
            ["mdx"] = EDbVendor.dbvmdx
        };

    public static int Main(string[] args)
    {
        try
        {
            Options options = Options.Parse(args);
            string sql = options.FilePath == null
                ? SampleSql
                : File.ReadAllText(options.FilePath);
            string input = options.FilePath == null
                ? "built-in sample"
                : Path.GetFullPath(options.FilePath);

            ValidationResult result = Validate(sql, options.Vendor);
            PrintResult(input, result);
            return result.IsValid ? 0 : 1;
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine("Input error: " + error.Message);
            PrintUsage();
            return 2;
        }
        catch (IOException error)
        {
            Console.Error.WriteLine("Input error: " + error.Message);
            PrintUsage();
            return 2;
        }
    }

    /// <summary>Parses a complete SQL string with a fresh parser instance.</summary>
    public static ValidationResult Validate(string sql, EDbVendor vendor)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return ValidationResult.Rejected(vendor, "SQL input is empty.");
        }

        TGSqlParser parser = new(vendor) { sqltext = sql };
        int parseCode = parser.parse();

        if (parseCode == 0)
        {
            return ValidationResult.Accepted(vendor, parser.sqlstatements.size());
        }

        return ValidationResult.Rejected(vendor, parser.Errormessage);
    }

    /// <summary>Resolves a documented .NET dialect alias and rejects unknown names.</summary>
    public static EDbVendor ResolveVendor(string alias)
    {
        if (alias != null && Vendors.TryGetValue(alias, out EDbVendor vendor))
        {
            return vendor;
        }

        throw new ArgumentException("Unsupported database dialect: " + alias);
    }

    private static void PrintResult(string input, ValidationResult result)
    {
        Console.WriteLine("Offline SQL syntax validation");
        Console.WriteLine("Input: " + input);
        Console.WriteLine("Dialect: " + VendorName(result.Vendor));
        Console.WriteLine("Database connection used: no");

        if (result.IsValid)
        {
            Console.WriteLine("Result: ACCEPTED");
            Console.WriteLine("Statements parsed: " + result.StatementCount);
        }
        else
        {
            Console.WriteLine("Result: REJECTED");
            Console.WriteLine("Parser diagnostic:");
            Console.WriteLine(result.ErrorMessage);
        }
    }

    private static string VendorName(EDbVendor vendor)
    {
        string name = vendor.ToString();
        return name.StartsWith("dbv", StringComparison.Ordinal)
            ? name.Substring(3)
            : name;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: checksyntax [/f <sql-file>] [/t <dialect>]");
        Console.Error.WriteLine("Without /f, the demo validates a built-in Oracle query.");
    }

    private sealed class Options
    {
        private Options(EDbVendor vendor, string filePath)
        {
            Vendor = vendor;
            FilePath = filePath;
        }

        public EDbVendor Vendor { get; }

        public string FilePath { get; }

        public static Options Parse(string[] args)
        {
            EDbVendor vendor = EDbVendor.dbvoracle;
            string filePath = null;

            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                if (argument.Equals("/t", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = ResolveVendor(RequireValue(args, ++index, "/t"));
                }
                else if (argument.Equals("/f", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = RequireValue(args, ++index, "/f");
                    if (!File.Exists(filePath))
                    {
                        throw new ArgumentException("SQL file does not exist: " + filePath);
                    }
                }
                else
                {
                    throw new ArgumentException("Unknown argument: " + argument);
                }
            }

            return new Options(vendor, filePath);
        }

        private static string RequireValue(string[] args, int index, string flag)
        {
            if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            {
                throw new ArgumentException(flag + " requires a value.");
            }

            return args[index];
        }
    }
}

public sealed class ValidationResult
{
    private ValidationResult(bool isValid,
        EDbVendor vendor,
        int statementCount,
        string errorMessage)
    {
        IsValid = isValid;
        Vendor = vendor;
        StatementCount = statementCount;
        ErrorMessage = errorMessage;
    }

    public bool IsValid { get; }

    public EDbVendor Vendor { get; }

    public int StatementCount { get; }

    public string ErrorMessage { get; }

    internal static ValidationResult Accepted(EDbVendor vendor, int statementCount)
    {
        return new ValidationResult(true, vendor, statementCount, null);
    }

    internal static ValidationResult Rejected(EDbVendor vendor, string errorMessage)
    {
        string diagnostic = string.IsNullOrWhiteSpace(errorMessage)
            ? "The parser rejected the SQL without a diagnostic."
            : errorMessage;
        return new ValidationResult(false, vendor, 0, diagnostic);
    }
}
