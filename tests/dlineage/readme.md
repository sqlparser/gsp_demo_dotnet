# dlineage demo 验证

用户操作见 [中文使用手册](../../src/demos/dlineage/readme_zh.md) 或 [English user guide](../../src/demos/dlineage/readme.md)。这里验证 demo 的参数连接、输出和旧入口迁移，不复制底层解析器/血缘引擎的测试集。

## 运行测试

从仓库根目录执行。当前开发版无需发布即可测试：

```bash
dotnet test tests/dlineage/test.dlineage.csproj -c Release \
  -p:GspAssemblyPath=/absolute/path/gudusoft.gsqlparser.dll
```

使用两种 DLL 分别运行，避免把一个产物的结果归给另一个产物。可用 `--artifacts-path /tmp/dlineage-tests-net10` 等独立输出目录隔离构建。

默认 NuGet `4.1.0.8` 构建：

```bash
dotnet build gsp_demo_dotnet.slnx -c Release
dotnet test gsp_demo_dotnet.slnx -c Release
```

旧包的测试验证帮助、参数错误、旧 DDL/追踪、原源码 API 的迁移，以及新命令明确报告 API 不可用；不会把它计作新引擎的验证。指定当前 DLL 后，`GSP_DLINEAGE` 启用真实的四方言及 XML/JSON/CSV/图、元数据、统计、变量/游标/同义词、临时表、函数过滤、错误流、UTF-8、资源恢复等检查。没有用 `Ignore` 或运行时跳过把未验证结果算作通过。

整个解决方案包含多目标共享库，指定本地 DLL 时使用 `netstandard2.0` 产物：

```bash
dotnet build gsp_demo_dotnet.slnx -c Release \
  -p:GspAssemblyPath=/absolute/path/netstandard2.0/gudusoft.gsqlparser.dll
dotnet test gsp_demo_dotnet.slnx -c Release \
  -p:GspAssemblyPath=/absolute/path/netstandard2.0/gudusoft.gsqlparser.dll
```

## 与实际 Java demo 对照

参考文件为 `gsp_demo_java` 提交 **`65a2cef16409e99f834989478e683976032e003a`** 的
`src/main/java/gudusoft/gsqlparser/demos/dlineage/DataFlowAnalyzer.java`，不是根据 readme 猜测功能。
本次使用的 C# 库来自 Java **`4940a81cf671dee163239df60488fa716d02177e`**。
Java demo 在临时目录中针对该库基准重新编译，未修改 Java checkout，也没有把其他 Java 版本的期望写成此版本的通过结果。

[`compare_java.py`](compare_java.py) 给两个进程传递相同 SQL 路径、方言、选项和元数据。它保存双方标准输出、标准错误、退出码、SHA-256 和差异文件：

```bash
python3 tests/dlineage/compare_java.py \
  --dotnet src/demos/dlineage/bin/Release/net10.0/dlineage.dll \
  --java-classpath 'JAVA_DEMO_CLASSES:PINNED_JAVA_CORE:OTHER_CLASSES:DEPENDENCY_JARS/*' \
  --output /tmp/dlineage-java-comparison
```

Windows 的 Java classpath 用 `;` 分隔。若使用完整 Java demo jar，可改用 `--java-jar /path/to/dlineage.jar`。不同 parser 版本可能产生不同结果，必须记录实际版本；公开 demo 的普通 Maven 配置固定的是 `4.2.6`，不自动等同于这里使用的库基准。

可以通过重复 `--case 名称` 只跑受影响的组合。常规模式输出按两层比较：

1. `rawExact`：完整标准输出字节相等。
2. `structuredExact`：解析后的 XML/JSON 完整结构相等；保留 ID、坐标、名称大小写、所有字段及数组/子节点顺序，不删除差异字段。XML 仅忽略展示缩进，JSON 对象字段顺序按 JSON 语义比较。

统计案例只比较明示的九项确定性计数，耗时原文保留，但不作为跨运行时相等门槛。`/s /text` 是单独测试的 CLI 修正，不纳入“与 Java demo 输出一致”的统计，因为该 Java 入口实际仍写 XML。

只要出现不一致或非零退出码，比较脚本就返回 `1`，不会把已知差异自动豁免成全通过。

Java 源码、库及输入不变时，可用 `--java-records /path/to/previous-output` 复用已核验的 Java 结果，只重新执行当前 C# demo。复用会核对输入路径/参数、SQL 与元数据字节 SHA-256，以及 Java 标准输出/错误的 SHA-256；结果明确记录复用来源。它不把旧 C# DLL 的结果归给新 DLL。

## 差异处理

PostgreSQL 的 `basic-postgresql` 案例保留完整原始差异：Java 对 `DECIMAL(10,2)` 输出 `dataType="decimal"`，当前 C# 库输出 `dataType="dec"`。该案例的血缘关系相同，完整模型比较仍应判为不一致。

检查了双方实际的 `dlineage.DataFlowAnalyzer` 建表列处理和 `nodes.TTypeName` 类型名称投影：CLI 只调用库，双方均从 `getDataTypeName()` 取得类型。没有在 demo 中把 `dec` 强制替换成 `decimal`，也没有删去类型字段以使比较通过。底层类型表示的进一步修复属于解析器/库的独立工作。

JSON 模型可能结构相等而字节不同（字段顺序、序列化转义）；两种结果分别记录。所有原始差异都保留。

## 平台适配与迁移边界

- 命令处理和选项顺序来自固定的 Java demo，使用正式 C# 库的 Java 兼容 API。
- .NET 通过 `TextWriter` 和 `try/finally` 管理输出及 `dispose()`，恢复进程 Console；不同于 Java demo 在 `main` 中更改进程全局流并结束进程的做法。
- `/env` 传递绝对文件名而非先读 JSON 字符串，以保留当前正式解析器对分片元数据的相对路径解析。
- `/s /text` 修正 Java 入口忽略文字结果的问题。文字格式器逐项沿用上述库基准的 `DataFlowAnalyzer.java#getTextOutput`；该方法在 Java 中为包级可见、C# 中为 `internal`，外部 demo 无法直接调用，因此只保留这段展示适配。没有重写血缘遍历算法。
- 统计监听器保留 Java 的预热、语句计数、最后 token 行数计算和 10,000 字节跳过规则；对空输入/零分母输出 `0`。
- 旧 `dataFlowAnalyzer` 的分析器及全部模型源文件迁至 `src/demos/dlineage/Compatibility/DataFlow/`，保持原命名空间和源文件字节；旧独立项目、重复手册和解决方案项被删除。共享 `dlineageCommon` 仍供原有依赖使用。
- 默认新功能仅调用正式库；不向兼容算法添加新功能。`/legacy` 等旧命令保留，DDL 通过已有 schema 对象输出单个合法 XML 文档。

本次没有发布包、调整默认包版本或运行远端 Windows CI。

## 2026-10-21 验证结果

| 验证范围 | 结果 |
|---|---|
| 当前 `net10.0` 解析器 DLL，统一 demo | 62/62 通过，无失败、无跳过。 |
| 当前 `netstandard2.0` 解析器 DLL，统一 demo | 62/62 通过，无失败、无跳过；运行宿主同为 Linux/.NET 10。 |
| 原 NuGet `4.1.0.8` 的兼容与可用性验证 | 最后一次定向测试 19/19 通过。 |
| 完整解决方案构建 | 原 NuGet 与当前 `netstandard2.0` DLL 两种配置均通过，零警告、零错误。 |
| 完整解决方案集成测试 | 当前 DLL 79 项通过；最后的统计边界修正后，受影响的 62 项又分别针对两份 DLL 通过。原 NuGet 首轮全套 35 项通过，新增旧命令错误验证后 19 项定向通过。 |
| 每份 DLL 与实际 Java demo 对照 | 40 个完整模型中 39 个结构相同，1 个保留 PostgreSQL 类型差异；1 个统计案例的九项计数相同。41 组均正常运行，31 组原始输出字节相同。 |
| 源码迁移与参数核对 | 33 份历史 C# 源文件字节未变；Java 实际识别的 46 个参数均有对应入口。 |

具体 DLL/CLI SHA-256、当前源文件 SHA-256 和测试边界见
[`validation.json`](evidence/2026-10-21/validation.json)；逐案结果见
[`net10.0 对照`](evidence/2026-10-21/java-net10-summary.json) 和
[`netstandard2.0 对照`](evidence/2026-10-21/java-netstandard-summary.json)。
保留的类型差异可直接查看 [`postgresql-decimal.diff`](evidence/2026-10-21/postgresql-decimal.diff)。

[`records.tar.gz`](evidence/2026-10-21/records.tar.gz) 保存双方完整输出、错误流、测试结果和对应源码/样例；
[`records-manifest.json`](evidence/2026-10-21/records-manifest.json) 列出每个归档成员的 SHA-256，所有成员均已解包读取核验。
最后一轮 C# 重放复用了源码/输入不变的实际 Java 输出，重新执行的是两份当前 CLI 产物，随后又核验了完整、有类型区分的 JSON 和 XML 文本结构。

这些验证记录保存的是合并完成时的快照，当时 `src/demos/dlineage/readme.md` 为中文。后续该手册改名为 `readme_zh.md`，另建英文 `readme.md`，并更新帮助中的手册路径。历史归档及其源文件 SHA-256 保留原样；语言版本调整没有更改分析逻辑。
