# PURL-TO-PREF-RENAME

> 制定日期：2026-07-08
> 定位：将 Purl 概念重命名为 Pref，格式改为合规 URL
> 当前状态：✅ 已实施

## 背景

业界已有 package-url/purl-spec（`pkg:npm/foo@1.0.0`），与项目自用的 Purl（`curseforge:jei@11.0.0`）撞名——格式不同、语义不同，在文档与交流中造成混淆。同时旧的 Purl 格式（`scheme:path`、用 `#` 存参与身份比较的 filter）不符合 RFC 3986，不是真正的 URL。

## 目标

- 把 Purl 概念整体改名为 Pref
- 顺带把格式修成合规 URL（`pref://host/path?query`）
- 用 System.Uri 替换手写 regex 解析
- 存量数据（profile、lock、快照）无缝读入，下次写入即迁移到新格式

## 非目标 / 不做的事

- 不一次性删除旧 Purl 格式的读取兼容——保留若干版本并标记过时，待用户文件稳定后再清理
- 不写数据迁移脚本，靠运行时"读旧写新"完成
- 不改 LockFormat 版本号
- 不清理其他计划 / 文档里的 purl 字样，视作同概念的别名

## 影响面

按能力划分（精确落点交给实施时 grep 当下代码）：

- 包标识的解析与构建（原 TridentCore.Purl 项目）
- profile / lock 的序列化字段与规则选择器枚举
- 部署引擎、仓库代理、导入导出、实例与配置管理
- CLI 命令、DTO、MCP 工具描述
- ViewModel、axaml 绑定、本地化资源（Polymerium 主程序）

## 验收标准

- 旧 profile（含 purl 键、purl 格式值、`type:"purl"` 选择器）读入后内部为 Pref + pref:// 格式，重存只含 pref 键与 pref:// 值
- 旧快照无需改动即可正常恢复
- 新建实例只产新格式
- 两个解决方案编译通过

## 备选方案备案

- **新建并行 Pref 项目而非原地改名** — 否决：原 Purl 项目无外部消费者（仅 Abstractions 一处引用），并行项目徒增同名类型歧义与一个死项目。
- **Pref 暂作 `string?`** — 否决：会向所有消费端传染可空性，且日后收紧回 `required` 时还得再改消费端，制造脆弱切换点。实测发现抛异常的只是 `required` 关键字本身；非空 `string` 去掉 `required` 照样能读旧 JSON（shim 填充），故 Pref 保持非空 `string`。
- **JSON 兼容属性用 `JsonIgnoreCondition.Always`** — 否决：`Always` 连反序列化都忽略，shim setter 永不触发；改用 `WhenWritingNull`。
- **把快照序列化统一到 Web 选项** — 否决：快照走 int 枚举（重命名后整数值不变、天然安全），且 Default 选项下 shim 属性的 key 为 PascalCase 正好匹配旧快照，无需改动。

## 方案

✅ 已实施（2026-07-08）。以下记录最终落地的做法。

### 重命名

`TridentCore.Purl` → `TridentCore.Pref`，原地改名：目录、csproj 文件名、`namespace TridentCore.Purl.* → TridentCore.Pref.*`、`Trident.slnx` / `Polymerium.slnx` / `TridentCore.Abstractions` 的 ProjectReference。

### 格式与解析

新格式 `pref://{repo}/{namespace?}/{identity}@{version}?{filters}`。Parser 按 `Uri.TryCreate` + `scheme == "pref"` 分流：命中走 Uri 拆解（Host 取 repo、AbsolutePath 上 `LastIndexOf('@')` 拆版本、`IndexOf('/')` 拆 namespace、Query 拆 filter）；否则走保留的旧 `[GeneratedRegex]`（Purl 兼容分支）。Builder 只产新格式。`@` 是合法 pchar、System.Uri 不编码，故路径段可直接承载 `identity@version`。双格式等价：legacy 串与等价的新串解析出同一个 PackageDescriptor。

### JSON 兼容（Abstractions + Core）

- `Entry.Pref`、`LockedPackage.Pref` 为非空 `string`（仅去掉 `required`，`= null!` 兜底）；`RuleSelector.Pref` 保持 `string?`（原状）。消费端零改动，日后加回 `required` 也不必动消费端。
- 三处各加 `[Obsolete] string? Purl` 兼容属性：`[JsonIgnore(Condition = WhenWritingNull)]`、getter 恒返回 null（永不写出）、setter/init 经 `PackageHelper.SafeMigrate`（能解析就转 pref://、转不动原样保留、加载绝不抛）写回 Pref。
- `SelectorType` 枚举成员 `Purl → Pref`；`FileHelper.SerializerOptions` 注册 `SelectorTypeConverter`（排在 `JsonStringEnumConverter` 之前），读 `"purl"`/`"pref"` → Pref、写 `"pref"`，其中 `"purl"` 分支标过时。仅作用于 profile.json 的 string 枚举路径。
- 快照不动：走 int 枚举（重命名后值不变）+ Default 选项下 shim 属性 key 为 PascalCase、正好命中旧快照。

### 消费端改名

- **Trident（Core / Cli）**：`.Purl → .Pref`、`ToPurl → ToPref`、`SelectorType.Purl → .Pref`、`using TridentCore.Purl → .Pref`、CLI 的 DTO 字段 / 位置参数 / 表头 / MCP 工具描述、`PackageHelper` 的解析方法名。
- **Polymerium**：ViewModel 属性与 axaml 绑定（`Value="Purl" → "Pref"`、`{Binding Purl} → Pref`）、本地化键 `SelectorType_Pref` / `ParsePrefDanger*` / `EditPrefButtonText`（resx + zh-hans + Designer + 所有引用三处同改，值文本同步 Purl→Pref）。
- CLI 为破坏性变更（位置参数与 DTO 字段重命名）。

### 验证

- 端到端：旧 profile JSON（purl 键 + purl 格式值 + `type:"purl"`）经 `FileHelper.SerializerOptions` 读入 → `Pref = pref://…`、`Type = Pref`；重存只含 pref 键与 pref:// 值。
- 双格式解析往返一致；legacy 与新格式跨格式等价。
- `dotnet build Trident.slnx` 与 `dotnet build Polymerium.slnx` 均 0 错误（仅预存的 NU1903 / CS0618 警告，与本次无关）。

### 待清理（未来版本）

`[Obsolete] Purl` shim 属性、`SelectorTypeConverter` 的 `"purl"` 分支、Parser 的 `ParseLegacy`——待用户文件迁移稳定后移除，届时把 Pref 收紧回 `required string`。
