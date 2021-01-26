using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using gudusoft.gsqlparser.stmt;
using gudusoft.gsqlparser.stmt.snowflake;
using gudusoft.gsqlparser.nodes;
using System.IO;

namespace gudusoft.gsqlparser.test
{
    /// <summary>
    /// UnitTestOracle 的摘要说明
    /// </summary>
    [TestClass]
    public class UnitTestSnowflake
    {
        TGSqlParser parser;

        public UnitTestSnowflake()
        {
            //
            //TODO:  在此处添加构造函数逻辑
            //
            parser = new TGSqlParser(EDbVendor.dbvsnowflake);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 附加测试特性
        //
        // 编写测试时，可以使用以下附加特性: 
        //
        // 在运行类中的第一个测试之前使用 ClassInitialize 运行代码
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // 在类中的所有测试都已运行之后使用 ClassCleanup 运行代码
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 在运行每个测试之前，使用 TestInitialize 来运行代码
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 在每个测试运行完之后，使用 TestCleanup 来运行代码
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestQuery()
        {
            parser.sqltext = "select f from t where f>1";
            int ret = parser.parse();
            Assert.IsTrue(ret == 0, parser.Errormessage);
        }

        [TestMethod]
        public void TestSnowflakeFiles()
        {
            String[] allfiles = System.IO.Directory.GetFiles(UnitTestCommon.BASE_SQL_DIR() + @"snowflake\", "*.sql", System.IO.SearchOption.AllDirectories);
            int cnt = 0;
            foreach (var file in allfiles)
            {
                FileInfo info = new FileInfo(file);
                UnitTestCommon.checkFile(parser, info.FullName);
                cnt++;
            }
        }

        [TestMethod]
        public void TestSnowflakeFiles2()
        {
            //String[] allfiles = System.IO.Directory.GetFiles(UnitTestCommon.BASE_SQL_DIR() + @"new_dotnet\snowflake\", "*.sql", System.IO.SearchOption.AllDirectories);
            //int cnt = 0;
            //foreach (var file in allfiles)
            //{
            //    FileInfo info = new FileInfo(file);
            //    UnitTestCommon.checkFile(parser, info.FullName);
            //    cnt++;
            //}
        }


        [TestMethod]
        public void TestClusterBy()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"create table ""TestSchema"".""TestTable""(
                                    col1 int,
                                    ""col2"" int
                                    )
                                    cluster
                                    by
                                    (
                                    ""col2""
                                    ,
                                    col1
                                    )";
            Assert.IsTrue(sqlParser.parse()==0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstcreatetable);
            TCreateTableSqlStatement sql = (TCreateTableSqlStatement)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.TableOptions.Count == 1);
            TCreateTableOption tableOption = sql.TableOptions[0];
            Assert.IsTrue(tableOption.CreateTableOptionType == ECreateTableOption.etoClusterBy);
            Assert.IsTrue(tableOption.ExpressionList.Count == 2);
           // Console.WriteLine(tableOption.ExpressionList.Count);
            Assert.IsTrue(tableOption.ExpressionList[0].ToString().Equals("\"col2\"", StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void TestDateRetentionTime()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE TABLE ""TestTable""
                                    (
                                    ""Col1"" int NOT NULL
                                    )
                                    DATA_RETENTION_TIME_IN_DAYS = 12; ";
            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstcreatetable);
            TCreateTableSqlStatement sql = (TCreateTableSqlStatement)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.TableOptions.Count == 1);
            TCreateTableOption tableOption = sql.TableOptions[0];
            Assert.IsTrue(tableOption.CreateTableOptionType == ECreateTableOption.etoDateRetentionTimeInDays);
            Assert.IsTrue(tableOption.DateRetentionInDays.ToString().Equals("12", StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void TestTableComment()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE TABLE ""TestTable""
                                    (
                                    ""Col1"" int NOT NULL COMMENT 'Test comment.',
                                    ""Col2"" int NOT NULL COMMENT 'Test comment 2.'
                                    )
                                    COMMENT = 'Table comment'; ";
            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstcreatetable);
            TCreateTableSqlStatement sql = (TCreateTableSqlStatement)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.TableOptions.Count == 1);
            TCreateTableOption tableOption = sql.TableOptions[0];
            Assert.IsTrue(tableOption.CreateTableOptionType == ECreateTableOption.etoComment);
            Assert.IsTrue(tableOption.CommentToken.ToString().Equals("'Table comment'", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(sql.TableComment.ToString().Equals("'Table comment'", StringComparison.CurrentCultureIgnoreCase));
            TColumnDefinition cd = sql.ColumnList.getColumn(0);
            Assert.IsTrue(cd.Comment.ToString().Equals("'Test comment.'", StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void TestStageCopyOptions()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE TABLE ""TestTable""
                                        (
                                        ""Col1"" int NOT NULL
                                        )
                                        STAGE_COPY_OPTIONS =
                                        (
                                        ON_ERROR = CONTINUE
                                        ); ";
            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstcreatetable);
            TCreateTableSqlStatement sql = (TCreateTableSqlStatement)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.TableOptions.Count == 1);
            TCreateTableOption tableOption = sql.TableOptions[0];
            Assert.IsTrue(tableOption.CreateTableOptionType == ECreateTableOption.etoStageCopyOptions);
            Assert.IsTrue(tableOption.CopyOptions.ToString().Trim().Equals("ON_ERROR = CONTINUE", StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void TestStageFileFormat()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE TABLE ""TestTable1""
                                        (
                                        ""Col1"" int NOT NULL
                                        )
                                        STAGE_FILE_FORMAT =
                                        (
                                        RECORD_DELIMITER = ''''
                                        ESCAPE_UNENCLOSED_FIELD = ''''
                                        ESCAPE = ''
                                        NULL_IF = ('a''a', ''''bb'' )
                                        FIELD_DELIMITER = ')'
                                        FIELD_OPTIONALLY_ENCLOSED_BY = '\''
                                        )
                                        COMMENT = 'Test'; ";

            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstcreatetable);
            TCreateTableSqlStatement sql = (TCreateTableSqlStatement)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.TableOptions.Count == 2);
            TCreateTableOption tableOption = sql.TableOptions[0];
            Assert.IsTrue(tableOption.CreateTableOptionType == ECreateTableOption.etoStageFileFormat);
           // Console.WriteLine(tableOption.StageFileFormat.ToString().Trim());
            Assert.IsTrue(tableOption.StageFileFormat.ToString().Trim().Equals(@"RECORD_DELIMITER = ''''
                                        ESCAPE_UNENCLOSED_FIELD = ''''
                                        ESCAPE = ''
                                        NULL_IF = ('a''a', ''''bb'' )
                                        FIELD_DELIMITER = ')'
                                        FIELD_OPTIONALLY_ENCLOSED_BY = '\''"
                    , StringComparison.CurrentCultureIgnoreCase));

        }

        [TestMethod]
        public void TestCreateFileFormat1()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE FILE FORMAT IF NOT EXISTS TestFormat11
                                    TYPE = CSV
                                    FIELD_DELIMITER = 'c';";


            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstCreateFileFormat);
            TCreateFileFormatStmt sql = (TCreateFileFormatStmt)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.FileFormatName.ToString().Equals(@"TestFormat11"
                    , StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(sql.FormatOptions.ToString().Equals(@"TYPE = CSV
                                    FIELD_DELIMITER = 'c'"
                    , StringComparison.CurrentCultureIgnoreCase));

        }

        [TestMethod]
        public void TestCreateFileFormat2()
        {
            TGSqlParser sqlParser = new TGSqlParser(EDbVendor.dbvsnowflake);
            sqlParser.sqltext = @"CREATE FILE FORMAT IF NOT EXISTS TestFormat
                                    TYPE = CSV
                                    COMMENT = 'Test comment';";


            Assert.IsTrue(sqlParser.parse() == 0);
            Assert.IsTrue(sqlParser.sqlstatements[0].sqlstatementtype == ESqlStatementType.sstCreateFileFormat);
            TCreateFileFormatStmt sql = (TCreateFileFormatStmt)sqlParser.sqlstatements[0];
            Assert.IsTrue(sql.FileFormatName.ToString().Equals(@"TestFormat"
                    , StringComparison.CurrentCultureIgnoreCase));
            // Console.WriteLine(sql.FormatOptions.ToString());
            Assert.IsTrue(sql.FormatOptions.ToString().Equals(@"TYPE = CSV
                                    COMMENT = 'Test comment'"
                    , StringComparison.CurrentCultureIgnoreCase));

        }

    }
}
