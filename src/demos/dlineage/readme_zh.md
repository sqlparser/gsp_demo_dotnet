# dlineage 中文使用手册

[English](readme.md) | [简体中文](readme_zh.md)

`dlineage` 从 SQL 文件或目录中提取列级、表级血缘，可输出 XML、JSON、CSV 和图数据，不需要连接数据库。

原 `dataFlowAnalyzer` demo 已合并到本目录。新功能直接调用正式库的 `gudusoft.gsqlparser.dlineage.DataFlowAnalyzer`，对齐 Java demo 的选项与处理顺序；旧 `dlineage` 的 DDL、正向追踪和反向追踪命令继续可用。

## 目录

- [准备与构建](#准备与构建)：SDK、本地 DLL 和未发布版本的使用方法。
- [快速开始](#快速开始)：运行随仓库提供的示例。
- [输入输出与退出码](#输入输出与退出码)：目录扫描、文件编码、错误处理。
- [输出格式](#输出格式)：列级、表级、CSV 和可视化数据。
- [分析选项](#分析选项)：简化模式、关系过滤、转换表达式、对象与坐标。
- [离线元数据](#离线元数据)：使用 JSON 解决列归属歧义。
- [统计模式](#统计模式)：批量分析的状态和统计口径。
- [旧命令迁移](#旧命令迁移)：保留的功能、入口和 `/s` 含义。
- [代码调用](#代码调用)：在自己的程序中使用正式 API。
- [常见问题与实现范围](#常见问题与实现范围)：版本、方言、Java 对齐边界。

- [当前 SQL Server 示例与验证](#sqlserver-acceptance)。

## 准备与构建

需要 .NET 10 SDK，命令均从 **`gsp_demo_dotnet` 仓库根目录**执行。

**当前仓库固定的 NuGet `4.1.0.8` 不含正式 dlineage API。完整功能需要当前开发版库，例如本地 `4.1.0.18` DLL；不需要先发布新版本。**

通过 `GspAssemblyPath` 显式指定 DLL，不必修改项目文件、NuGet 版本或全局包缓存：

```bash
dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release \
  -p:GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll
```

该属性也作用于项目依赖，确保进程只加载这一份解析器。路径应为绝对路径，且 DLL 必须包含当前正式 API；旧 DLL 即使文件名相同也不能补齐功能。`net10.0` DLL 可用于构建本 demo；若构建整个多目标解决方案，请提供 `netstandard2.0` DLL。

如果需要从相邻的 `gsp_dotnet` 源码构建库：

```bash
dotnet build ../gsp_dotnet/gsp_dotnet_core/src/gsqlparser/gsqlparser.csproj \
  -c Release -f net10.0 \
  -p:includeDB2=true -p:includeGreenplum=true -p:includeHive=true \
  -p:includeImpala=true -p:includeInformix=true -p:includeMdx=true \
  -p:includeMssql=true -p:includeMySQL=true -p:includeNetezza=true \
  -p:includeOracle=true -p:includePostgreSQL=true -p:includeRedshift=true \
  -p:includeSnowflake=true -p:includeSybase=true -p:includeTeradata=true

dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release \
  -p:GspAssemblyPath="$PWD/../gsp_dotnet/gsp_dotnet_core/src/gsqlparser/bin/Release/net10.0/gudusoft.gsqlparser.dll"
```

构建后直接运行 demo DLL，后续命令无需重复构建属性：

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /version
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /help
```

Windows PowerShell 使用同一组参数，将 DLL 路径换成实际位置即可：

```powershell
dotnet build src/demos/dlineage/demos.dlineage.csproj -c Release `
  -p:GspAssemblyPath=C:/gsp/gudusoft.gsqlparser.dll
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll /f samples/dlineage/basic.sql /json
```

未指定本地 DLL 时，仓库仍能按原来的 NuGet 配置构建：帮助、版本和旧命令可用；新分析命令明确提示缺少 API，返回 `2`，不会悄悄切换到旧算法。`EnableDlineage` 在指定本地 DLL 或 `GspVersion >= 4.1.0.18` 时启用；版本号只表示编译条件，不意味着该包已经发布。

## 快速开始

以下命令使用上一步构建的完整 demo：

```bash
# 单文件，默认 Oracle，输出列级 XML
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql

# 简化列级 JSON，写入文件
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /t oracle /s /json /o lineage.json

# 表级 CSV
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /tableLineage /csv /o tables.csv

# 同一方言的多个 SQL 文件
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /d YOUR_SQL_DIRECTORY /t mssql /s /json /o combined.json
```

示例位于 [`samples/dlineage/`](../../../samples/dlineage/)：`basic.sql` 可用于 Oracle、SQL Server、MySQL、PostgreSQL；`advanced-oracle.sql` 包含视图、CTE、JOIN、CASE、SUM、COUNT；`routine-oracle.sql` 演示变量、游标和同义词；`temporary-mssql.sql` 演示临时表。`metadata-query.sql` 与 `metadata.json` 是元数据示例。请按文件选择对应方言，不宜把整个示例目录作为一套 SQL 输入。

## 输入输出与退出码

| 参数 | 说明 |
|---|---|
| `/f <文件>` | 分析一个 SQL 文件。必须与 `/d` 二选一。 |
| `/d <目录>` | 递归分析目录，按正式库的规则筛选和排序；普通模式不仅接受 `.sql`，也可能读取 `.txt`、无扩展名等文件，排除 `.md`、图片、DLL 等已知非 SQL 扩展名。请使用专门的 SQL 目录。 |
| `/t <方言>` | 默认 `oracle`。当前正式 .NET 实现支持 `oracle`、`mssql`（别名 `sqlserver`）、`mysql`、`postgresql`（别名 `postgres`）。 |
| `/o <文件>` | 以 UTF-8、无 BOM 写入结果；省略则写到标准输出。输出位置必须在输入目录之外，也不能覆盖输入 SQL 或元数据文件；父目录须已存在。 |
| `/log` | 有诊断时，额外写入当前工作目录的 `dataflow.log`，覆盖旧日志。 |
| `/version` | 显示实际库版本和发布日期，不要求输入文件。 |
| `/help`、`/?`、`--help` | 显示帮助；无参数也显示帮助。 |

选项名称不区分大小写。包含空格的路径和分隔符请用引号包裹。SQL 读取沿用库的文件读取规则，输出不会把 SQL 文本全部转换为大写。

诊断写到标准错误，不混入 JSON/XML，也不会在指定 `/o` 后把结果再次打印到标准错误。输出文件可能保留部分模型或 `<error>` 信息，**文件存在不代表分析成功**。

| 退出码 | 含义 |
|---|---|
| `0` | 分析完成且没有库报告的错误；或帮助、版本查询成功。 |
| `1` | SQL 分析、输出或运行失败；请检查标准错误与模型中的错误信息。 |
| `2` | 参数、路径、元数据格式、方言或所需库 API 不满足要求。 |

## 输出格式

| 参数 | 结果 |
|---|---|
| 无格式参数 | 列级 XML；根节点为 `<dlineage>`，关系为 `<relationship>`。 |
| `/json` | SQLFlow JSON 模型，含数据库对象、关系和处理过程。 |
| `/json /graph` | 含图结构的 JSON；与 `/showER` 组合时输出 ER 图数据。 |
| `/lv` | 供血缘可视化使用的 JSON 数据；不会启动浏览器或托管页面。 |
| `/csv` | 详细列级 CSV，保留对象/列 ID、关系类型和语句作用类型。 |
| `/csv-simple` | 简化列级 CSV：来源、目标、关系类型，排除 `RelationRows` 系统列。默认分隔符为逗号加空格。 |
| `/delimiter <文本>` | CSV 分隔符，普通 CSV 默认 `,`。兼容 Java usage 中的旧拼写 `/delimeter`。 |
| `/tableLineage` | 生成表级血缘，可与 XML、`/json`、`/json /graph`、`/lv` 或 `/csv` 组合。此模式覆盖 `/s`、`/i` 的简化设置。 |
| `/s /text` | 文字依赖，例如 `amount depends on: source_data.amount`。 |
| `/traceView` | 追踪源表与视图，自动启用简化模式，省略中间数据。 |

```bash
# 简化 CSV；使用竖线分隔
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /s /csv-simple /delimiter "|"

# 图数据
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /json /graph /o graph.json

# 文字依赖
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/basic.sql /s /text
```

格式选择沿用 Java 的优先顺序，建议每次只选择一种格式：列级为 `/lv` → `/csv-simple` → `/csv` → `/json` → `/traceView` → 文字/XML；表级首先选择 CSV，否则为 `/lv` → JSON/图 → XML。`/tableLineage /csv-simple` 按 Java 行为输出表级详细 CSV；需要简化列级 CSV 时省略 `/tableLineage`。`/graph` 需要 `/json`；`/text` 需要显式 `/s`，遇到更高优先级的格式时不生效。

CSV 保持 Java 库的输出约定，不能假定任意分隔符或 SQL 标识符都符合通用 RFC CSV 转义规则；用于自动导入前请先检查样例。

## 分析选项

### 中间结果与对象保留

| 参数 | 说明 |
|---|---|
| `/s` | 简化输出，去除中间结果集；通常适合把血缘导入其他系统。 |
| `/i` | 隐去中间结果，保留 SQL 函数结果集；与 `/if` 同时使用时函数仍被去除。 |
| `/topselectlist` | 保留最外层 SELECT 结果，通常与 `/s` 组合；单独使用并不等同于 `/s`。 |
| `/withTemporaryTable` | 在简化输出中保留临时表，默认不保留。 |
| `/showResultSetTypes <类型列表>` | 在 `/s`、`/i` 简化结果中保留指定结果集，逗号分隔。 |
| `/showVariable` | 简化模式中保留变量。 |
| `/showCursor` | 简化模式中保留游标。 |
| `/showSynonym` | 简化模式中保留同义词。 |
| `/removeResultSetTypes <类型列表>` | 对生成模型进行后处理，去除指定类型的结果集，保留依赖连接。 |
| `/removeVariable` | 后处理去除变量。 |
| `/removeCursor` | 后处理去除游标。 |
| `/removeUnusedSynonym` | 不输出未使用的同义词。 |
| `/if` | 后处理去除函数结果集；其他中间结果按当前模式处理。 |

当前库支持的结果集类型包括 `select_list,array,struct,result_of,cte,insert_select,update_select,merge_update,merge_insert,output,update_set,pivot_table,unpivot_table,alias,function,case_when,cursor,variable`。沿用库的类型识别规则；不存在的类型不会自动成为新功能。

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/advanced-oracle.sql /s \
  /showResultSetTypes cte,function /json
```

### 关系、表达式与命名

| 参数 | 说明 |
|---|---|
| `/j` | 包含 JOIN 关系。 |
| `/filterRelationTypes <列表>` | 只输出指定关系：`fdd`（直接）、`fdr`（间接）、`join`、`call`、`er`，逗号分隔。 |
| `/s /simpleShowRelationTypes <列表>` | 设置简化时遍历的关系类型，通常为 `fdd,fdr`；只有显式 `/s` 时设置。 |
| `/transform` | 包含转换表达式代码。 |
| `/transform /coor` | 按 Java 语义使用转换表达式坐标；单独 `/coor` 不启用转换信息。 |
| `/showConstant` | 显示常量表。 |
| `/showER` | 包含实体关系信息；图输出请用 `/json /graph /showER`。 |
| `/treatArgumentsInCountFunctionAsDirectDataflow` | 把 COUNT 参数视为直接数据依赖，默认关闭。 |
| `/showCaseWhenAsIndirect` | 把 CASE WHEN 条件视为间接依赖，默认条件按直接依赖处理。 |
| `/n` | 规范化输出。 |
| `/ic` | 忽略输出中的坐标。 |
| `/traceTablePosition` | 追踪表的全部位置。 |
| `/lof` | 无法确定归属的孤立列连接到第一张表；这是启发式设置，优先提供 DDL 或元数据。 |
| `/defaultDatabase <名称>` | 设置缺省数据库。 |
| `/defaultSchema <名称>` | 设置缺省 schema。 |
| `/showImplicitSchema` | 显示隐式 schema。 |

## 离线元数据

对 `SELECT ename FROM emp, dept ...` 这类未限定列，分析器需要知道哪张表拥有 `ename`。可以把建表 DDL 放在 SQL 前面，也可以传入离线元数据：

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/metadata-query.sql /t mssql \
  /env samples/dlineage/metadata.json \
  /defaultDatabase demo_db /defaultSchema dbo /s /json /o metadata-lineage.json
```

`/env` 调用正式 `TJSONSQLEnvParser`，使用它识别的 SQLFlow、Grabit、SQLDep 或分片格式，并与 Java demo 一样选取返回的**第一个环境**。本 demo 传递绝对文件路径，保留分片元数据的相对文件基准目录。示例 `metadata.json` 使用 SQLFlow 格式，可直接复制调整表和列。

文件不存在、格式不受支持或未能加载环境时，返回 `2` 并报告错误，不会忽略元数据继续输出容易误解的血缘。环境中的方言、数据库和 schema 应与 SQL 及缺省命名设置一致。

## 统计模式

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /d YOUR_SQL_DIRECTORY /t oracle /stat /o statistics.txt
```

`/stat` 递归筛选 `.sql` 文件，对每个文件输出状态、耗时、绝对路径和字节长度，再输出文件数、语句数、行数、成功/失败语句数、平均耗时等汇总。状态 `1` 表示无分析错误，`2` 表示有错误但取得部分表模型，`3` 表示没有可用表模型。

统计口径保留 Java 行为：先预热，不把预热计入统计；行数由最后一个 token 的行号和 Java 字符串拆分规则计算，不等同于 `wc -l`；**跳过大于 10,000 字节的文件，即使使用完整版 DLL 也如此**，通过 `Files ignored in trial version` 显示跳过数。这是 demo 的统计规则，普通分析 `/f`、`/d` 没有这条额外过滤。

统计模式使用独立分析器、监听器和固定的孤立列设置，不套用普通模式的全部展示参数；可指定 `/env`，简化设置只取 `/s`、`/traceView`。无 SQL 文件、空文件或全部跳过时，平均值输出 `0`。耗时会随运行环境变化。

## 旧命令迁移

| 原入口或命令 | 现在的用法 |
|---|---|
| `src/demos/dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj` | 改为 `src/demos/dlineage/demos.dlineage.csproj`，执行文件为 `dlineage.dll`。 |
| `build.sh run dataFlowAnalyzer ...` | 保留为 `dlineage` 的命令别名，不再构建第二个项目。 |
| 旧 `dlineage /f ...` 的 `columnImpactResult` 报告 | 使用 `/legacy /f ...`。新版默认生成正式 dlineage 模型。 |
| `/fo <table.column>` | 保留正向追踪，自动进入旧命令模式。 |
| `/b <view.column>` | 保留反向追踪，自动进入旧命令模式。 |
| `/ddl` | 保留 DDL schema 输出，自动进入旧命令模式；输出为合法 XML。 |
| 旧 `/s` 严格匹配 catalog/schema | 旧命令模式中仍保留这个含义，建议改用更明确的 `/strict`；新版普通模式的 `/s` 与 Java 一致，表示简化输出。 |

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /fo employees.salary /strict

dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /b v_employee_costs.total_cost

dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/oracle-lineage.sql /ddl
```

旧命令通过原 `DlineageCommon` 实现，接受 `/f`、`/d`、`/t`、`/o`、`/log`，不支持混用 `/json` 等新模式参数。原 NuGet `4.1.0.8` 也可运行这些兼容命令。原 `gudusoft.gsqlparser.demos.dataFlowAnalyzer` 分析器及模型源码整体迁至 `Compatibility/DataFlow/`，原命名空间和构造函数保留，便于既有源代码迁移；新的功能使用正式库，不在这套历史算法中继续添加。

通过 Bash 构建脚本使用本地 DLL 时，可把构建属性作为环境变量传入，例如 `GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll ./src/demos/build.sh run dataFlowAnalyzer /f samples/dlineage/basic.sql /json`。

`dlineageCommon` 仍被 `dlineageRelation` 等项目使用，因此作为共享库保留；它与本目录的 CLI 不是需要再次合并的两个可执行项目。

## 代码调用

新程序直接使用正式库 API，不必复制 demo 的旧模型或旧算法：

```csharp
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.dlineage;
using gudusoft.gsqlparser.dlineage.dataflow.model;
using gudusoft.gsqlparser.runtime;

var option = new Option();
option.setVendor(EDbVendor.dbvmssql);
option.setOutput(true);
option.setSimpleOutput(true);
var analyzer = new DataFlowAnalyzer(new JFile("query.sql"), option);
try
{
    string xml = analyzer.generateDataFlow();
    var model = analyzer.getDataFlow();
    foreach (var error in analyzer.getErrorMessages())
        Console.Error.WriteLine(error.getErrorMessage());
    Console.WriteLine(xml);
}
finally { analyzer.dispose(); }
```

CLI 宿主可调用 `Dlineage.Run(args, outputWriter, errorWriter)` 获取退出码；调用期间保护并恢复 `Console.Out`、`Console.Error`，不关闭宿主传入的 writer。同一进程中的 CLI 调用会串行执行。需要构建自己的并发服务时，应使用正式库的生命周期与并发 API。

## 常见问题与实现范围

- **为什么旧包构建成功，但 `/json` 提示缺少 API？** 构建成功只能证明旧兼容入口可用。请使用“准备与构建”中的本地当前 DLL，并重新构建；查看 `/version` 确认加载的库。
- **为什么没有任何关系或返回 `1`？** 检查标准错误、XML 中的错误以及 SQL 方言。商业试用限制由实际 DLL 决定；旧 NuGet 试用包有约 10,000 字符的限制，不能把输出文件存在当作成功。
- **Java 列出更多数据库，为什么这里只列四种？** 当前正式 C# 血缘实现的验证与支持边界为四种方言。CLI 完整承接实际 Java demo 的分析与输出选项，并不扩展底层解析器/血缘引擎的方言范围。
- **是否包含数据库连接、DuckDB 仓库和浏览器展示？** Java 的实际入口没有 `/fromdb`、`/exportonly`、`/metadataoutput`，也未实现其 `docs/merge_csv.md` 中的 DuckDB 设计命令。本 demo 与实际代码范围一致，读取离线元数据、输出图数据，不把设计稿当作已实现功能。
- **与 Java 有哪些有意的 CLI 差别？** 参数错误和分析错误返回非零退出码；输出和诊断分流并可靠释放资源；无效元数据明确报错；`/stat` 空输入不会除零；`/s /text` 实际输出文字，修正 Java demo 设置文字选项后又序列化 XML 的问题。普通分析的选项设置和格式优先级仍沿用 Java。
- **是否所有输出都与 Java 完全相同？** 对照中保留了 PostgreSQL `DECIMAL` 的类型名称差异：Java 模型为 `decimal`，当前 C# 库为 `dec`，demo 不改写它。JSON 也可能存在字段顺序或转义字节差异；可解析模型与原始字节分别核验，详见验证记录。

实现参考固定为 Java demo 提交 `65a2cef16409e99f834989478e683976032e003a` 的 `DataFlowAnalyzer.java`；当前 C# 库的 Java 源码基准为 `4940a81cf671dee163239df60488fa716d02177e`。验证方法与实际差异见 [`tests/dlineage/readme.md`](../../../tests/dlineage/readme.md)。

<a id="sqlserver-acceptance"></a>

## 2026-10-24：SQL Server 当前开发版验证

当前本地库为 **4.1.0.18**，无需发布新 NuGet。原始 CTE 和 CROSS APPLY 的漏迁移已在库中修复，demo 继续调用正式 API。两份最新 DLL 各通过现有 62 项 CLI 测试；`netstandard2.0` 的执行宿主也是 Linux/.NET 10。

新增 14 份原始 SQL、62 组选项／格式对照：每份 DLL 有 **58 组完整模型相同、29 组 stdout 字节相同、34 组双方退出码均为 0**。四组五段物理表名仍触发既有 Identifier 能力限制；另有 24 组双方报告相同的 orphan-column 诊断、模型相同，但 C# 按公开合同返回 1；Java 错误标题后还多一个空行，双方原始 stderr 均保留。完整比较仍返回 1，没有忽略这些失败。公开解析器样例不等于 14 份客户工作负载。

用户的 `SELECT ... INTO [dbo].[customer_revenue]` 示例也保存为原字节，可直接生成文件：

```bash
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/sqlserver-acceptance-20261024/customer-revenue.sql /t mssql /o customer_revenue.xml
dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  /f samples/dlineage/sqlserver-acceptance-20261024/customer-revenue.sql /t mssql /json /o customer_revenue.json
```

结果包含五个目标列及对应来源。实际 DLL 身份、完整结果和重放方式见[本批验证](../../../tests/dlineage/readme.md#2026-10-24-sql-server-当前开发版验证)及[机器记录](../../../tests/dlineage/evidence/2026-10-24/validation.json)。10 月 21 日的四方言比较保持原 DLL 范围；本批没有运行 Windows CI，也没有修改默认包版本。
