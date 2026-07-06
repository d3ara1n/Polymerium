# URL Scheme 统一：建立内部资源 URI 命名规范

> 制定日期：2026-06-29
> 定位：基础设施任务，是 Recipe 系统的前置条件之一。
> 当前状态：蓝本，✅ 已实施（2026-07-06）。下游任务直接消费 InternalUriHelper。
> Jira：[POLY-115](https://d3ara1n.atlassian.net/browse/POLY-115)
> 依赖：无。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 系统需要一种方式标识"某个 mod 属于哪个 recipe"——这个标识要存进 `Profile.Rice.Entry.Source`，被字符串比较消费（分组、锁定判断），且要能被用户在 UI 上读懂（"这个 mod 来自 recipe://qol-pack"）。

项目已有一个内部资源 URI 的雏形：`poly://skin?type=...&src=...`，用于本地皮肤渲染。但 `poly://` 是个笼统前缀（既不说明资源种类，也无法扩展到 skin 以外）。本任务把它统一为 `<kind>://<identifier>` 的命名规范，使 `skin://` 和 `recipe://` 各自成立，在用户认知和代码实现上双重统一，为 Recipe（以及未来其他内部资源类型）铺好标识层。

---

## 1. 背景与动机

### 1.1 现存两套标识体系，互不相干

Polymerium 代码里同时存在两套资源标识，**没有任何共享抽象**：

| 体系 | 形式 | 用途 | 解析方式 |
|------|------|------|----------|
| **Purl**（Trident） | `label:ns/pid@vid#filter`（如 `curseforge:jei@123456`） | 标识外部仓库资源（CurseForge / Modrinth） | `TridentCore.Purl.Parsing.Parser`（regex） |
| **poly://**（Polymerium 内） | `poly://skin?type=...&src=...` | 本地皮肤渲染请求路由 | `SkinRenderService` 里 `StartsWith` + 手解析 query |

`poly://` 散落在 4 个文件，全硬编码：

| 文件 | 行 | 内容 |
|------|----|------|
| `Services/SkinRenderService.cs` | 33 | `private const string Scheme = "poly://"` |
| `AppImageLoader.cs` | 62 | `url.StartsWith("poly://", ...)` 路由分支 |
| `Utilities/AccountHelper.cs` | 77,80,87,91-94 | 7 处 `"poly://skin?..."` 字符串拼接 |
| `PageModels/UnknownPageModel.cs` | 36-42 | 7 处测试样本硬编码 |

### 1.2 问题

- **`poly://` 前缀笼统**：`poly` 不表示资源种类，`poly://skin` 把"Polymerium 品牌"和"资源类型"混在一起，无法自然扩展到 `poly://recipe`（语义错位：recipe 不是 poly 品牌下的渲染请求）。
- **没有命名规范**：新增内部资源类型（recipe）该用什么形式？没有约定，各处会各搞各的。
- **认知割裂**：用户在 UI 看到的来源标识（未来的 `recipe://xxx`）和内部图片 URL（`poly://skin`）若形态不一，心智负担加重。

### 1.3 为什么必须先统一

Recipe 引入后，`Entry.Source` 会同时存在三种文本：

```
curseforge:all-the-mods:123456:789012   ← 整合包（Purl，外部仓库）
recipe://qol-essentials                 ← recipe（内部资源，本任务引入）
null                                     ← 手动添加
```

若不先把"内部资源用什么 URI 形式"定下来，recipe 的标识就无处安放，且容易和 Purl（`curseforge:`）混淆。

---

## 2. 目标与非目标

**目标**

1. 建立 **`<kind>://<identifier>`** 作为 Polymerium 内部资源 URI 的统一命名规范，用户可见、代码可解析。
2. **`poly://skin` → `skin://`**：机械迁移，前缀即资源种类。
3. **引入 `recipe://<id>`** 作为 recipe 的标识形式，供 `Entry.Source` 存储。
4. 明确 **Purl 与内部 URI 的边界**：保证现有 `Source` 里的 Purl 文本不被误识别为内部 URI，反之亦然。
5. 提供一个**集中的 scheme 判定工具**，避免 `StartsWith` 字面量散落。

**非目标（本次不做）**

- 不实现 recipe 的功能逻辑（导入/管理/链接）——那是 Recipe 系统本身。
- 不把 Purl 体系改造成 `<kind>://` 形式——Purl 是 Trident 子模块的既有标准（`label:...`），保持不动。
- 不强制所有图片资源都走 scheme 路由——只统一内部资源标识，外部 http(s) 图片加载不变。
- 不引入重量级 URI 解析框架——内部 URI 形态简单，正则/前缀足够。

---

## 3. 核心设计

### 3.1 命名规范

Polymerium 内部概念资源统一用：

```
<kind>://<identifier>[?<query>]
```

- `<kind>`：资源种类，小写，单一词（`skin`、`recipe`、未来可能的 `icon` 等）。
- `<identifier>`：该资源在自身命名空间内的 id（recipe id、皮肤来源 key 等）。
- `<query>`：可选，种类特定的参数（如 skin 的 `?type=face`）。

**与 Purl 的区分规则**（关键，用于 §3.4 的判定工具）：

| 形式 | 体系 | 判定 |
|------|------|------|
| 包含 `://` | 内部 URI | 本规范管辖 |
| `label:...` 不含 `://`（如 `curseforge:jei`） | Purl（外部仓库） | Trident Purl 体系 |

这两者**格式上天然不重叠**（`://` 是分水岭），因此 `recipe://qol` 绝不会被 Purl parser 当成合法 purl，`curseforge:jei` 也绝不会触发内部 URI 分支。

### 3.2 `skin://` 迁移

纯机械改名，4 个文件：

- `SkinRenderService.Scheme` 由 `"poly://"` 改为 `"skin://"`，`ParseQuery` 入口前缀相应调整。
- `AppImageLoader.cs:62` 的 `StartsWith("poly://")` 改为 `StartsWith("skin://")`。
- `AccountHelper.cs` 7 处 `"poly://skin?..."` 改为 `"skin://..."`（注意：`<kind>://` 已含种类，不再写 `skin://skin`，直接 `skin://?type=...&src=...`，或保留一个轻量 path：`skin://render?type=...`——见备选 B.2）。
- `UnknownPageModel.cs` 7 处测试样本同步。

**兼容**：已渲染的皮肤 `MemoryCache` 以完整 URI 为 key，改名后旧 key 自然失效、按需重渲染，无持久化数据需迁移。`poly://` 不在任何 `profile.json` / `data.lock.json` 中持久化（它只是运行时图片请求），所以无需读旧数据兼容。

### 3.3 `recipe://` 引入

`recipe://<id>` 形式，`<id>` 是 recipe 的唯一标识（recipe 定义文件的名字或内部 id，见 Recipe 系统任务）。

**本任务只交付**：
- `recipe://` 的字符串形式约定。
- 一个 `InternalUriHelper` 工具类（§3.4）能识别和构造它。
- 为后续 `Entry.Source` 存储 recipe:// 提供判定基础。

**不交付**：recipe 的解析（`recipe://qol` 指向哪个 recipe 文件）、导入、UI——这些在 Recipe 系统任务和 SOURCE-REFERENCE-SEMANTICS 任务里。

### 3.4 集中的 scheme 工具

新增 `src/Polymerium.Avalonia/Utilities/InternalUriHelper.cs`（或放合适命名空间），收敛散落的 `StartsWith`：

```csharp
public static class InternalUriHelper
{
    public static bool IsInternal(string? s) =>
        s is not null && s.Contains("://", StringComparison.Ordinal);

    public static bool IsKind(string? s, string kind)
    {
        if (s is null) return false;
        var prefix = kind + "://";
        return s.StartsWith(prefix, StringComparison.Ordinal);
    }

    public static string Skin(string query) => "skin://" + query;       // 带 ?type=...
    public static string Recipe(string id) => "recipe://" + id;
}
```

`AppImageLoader`、`AccountHelper`、`SkinRenderService` 统一改用 `InternalUriHelper.IsKind(url, "skin")` / `InternalUriHelper.Skin(...)`，消灭裸字符串 `"poly://"`。

> 这个工具不放 Trident——内部 URI 是 Polymerium 宿主的概念，Trident 的 Purl 体系自洽，不需引入。

### 3.5 与 Purl 的边界保证

现有 `Entry.Source` 里存的整合包标识是 Purl 文本（如 `curseforge:all-the-mods`，由 `CurseForgeImporter` 写入，见 `Importers/CurseForgeImporter.cs:38`）。这些文本 **不含 `://`**，因此：

- `InternalUriHelper.IsInternal(source)` 对所有现存 Source 返回 `false`（Purl 不被误判为内部 URI）。
- 反之 `recipe://xxx` 含 `://`，`PackageHelper.TryParse` 会解析失败——recipe 标识不会被当 Purl。

任何"这个 Source 是 recipe 还是整合包"的判断，用 `InternalUriHelper.IsKind(source, "recipe")`，**不要**用 `PackageHelper.TryParse`。这条边界规则要在 SOURCE-REFERENCE-SEMANTICS 任务的所有 Source 读取点贯彻。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 工具 | `Utilities/InternalUriHelper.cs`（新增） | scheme 判定与构造工具 |
| 渲染 | `Services/SkinRenderService.cs` | `Scheme` 改 `skin://`，解析入口调整 |
| 图片加载 | `AppImageLoader.cs` | 路由分支用 `InternalUriHelper.IsKind` |
| 账号 | `Utilities/AccountHelper.cs` | 7 处拼接改 `InternalUriHelper.Skin` |
| 测试样本 | `PageModels/UnknownPageModel.cs` | 7 处同步 |

本任务**不改动** Profile / LockData / Importer / 部署管线——它只建立命名规范和工具，recipe:// 的消费点（Source 语义、UI 分组）在后续任务接入。

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 全局搜索 `poly://` | 仅可能在历史 git 记录出现，源码中归零 |
| 2 | 皮肤头像/全身渲染 | 与迁移前行为一致（`skin://` 正确路由到 `SkinRenderService`） |
| 3 | `InternalUriHelper.IsKind("curseforge:jei", "recipe")` | 返回 `false`（Purl 不误判） |
| 4 | `InternalUriHelper.IsKind("recipe://qol", "recipe")` | 返回 `true` |
| 5 | `PackageHelper.TryParse("recipe://qol", out _)` | 返回 `false`（recipe 不被当 Purl） |
| 6 | 旧 `data.lock.json` / `profile.json` 读取 | 不受影响（从未持久化 poly://） |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 迁移后 `MemoryCache` 旧 key 失效，首次重渲染皮肤 | 皮肤渲染轻量、按需触发，用户无感；无需做 key 迁移 |
| 引入 `InternalUriHelper` 工具看似过度设计（当前只有 skin） | recipe 即将到来，提前收敛避免 `StartsWith` 再次散落；工具极轻 |
| 未来若 recipe 需要带 query（如 `recipe://qol?v=2`） | `<kind>://<id>[?query]` 规范已预留 query 位，按需扩展 |

---

## 7. 不做的事（明确边界）

- **不改 Purl 体系** —— `label:...` 是 Trident 标准，外部仓库标识不动。
- **不实现 recipe 解析逻辑** —— 只定义形式和判定工具，recipe 指向什么、怎么导入在 Recipe 系统任务。
- **不持久化迁移 `poly://`** —— 它从未持久化，无需迁移。
- **不统一图片加载为通用 scheme handler 框架** —— 当前只有 skin 一种图片 scheme，`InternalUriHelper.IsKind` + `SkinRenderService` 足够；待真出现第二种图片 scheme 再抽象（见备选 B.1）。

---

## 附录：备选方案备案

### B.1 内部 URI 的路由抽象

当前选：**`InternalUriHelper` 静态工具 + 各消费方自行处理**（§3.4）

| 备选 | 做法 | 取舍 |
|------|------|------|
| `ISchemeHandler` 注册表 + DI 注入 `IEnumerable<ISchemeHandler>` | `AppImageLoader` 遍历 handler 找匹配 | 过度设计：当前图片 scheme 仅 skin 一种；recipe 不走图片加载。抽象成本 > 收益 |
| 统一 URI 解析框架（自定义 `Uri` 类） | 一个 parser 解析所有内部 URI | 形态简单（kind + id + query），正则/前缀足够，框架是杀鸡用牛刀 |

### B.2 skin URI 的 path 形式

当前选：**`skin://?<query>`**（kind 后直接 query）

| 备选 | 做法 | 取舍 |
|------|------|------|
| `skin://render?type=...`（带 path） | path 表示"操作" | 引入 path 维度增加复杂度；skin 只有渲染一种操作，path 冗余 |
| `skin://face?src=...`（kind 后直接 view type） | view type 当 identifier | 与 recipe 的 `recipe://id` 形态不一致（recipe 的 id 是资源 id，skin 的不是）；统一规范里 identifier 应是资源标识，view type 是参数，归 query 更合理 |

### B.3 recipe id 的形式

当前选：**简单字符串 id**（`recipe://qol-essentials`）

| 备选 | 做法 | 取舍 |
|------|------|------|
| UUID / 随机 id | `recipe://550e8400-...` | 人类不可读，UI 展示需额外查名字；recipe 是用户可创建/导入的，slug 形式更友好 |
| 带命名空间的 id（`recipe://builtin/qol`、`recipe://user/my-pack`） | 区分内置/用户/导入来源 | 若需要按来源管理 recipe 可考虑；首版用扁平 id，命名空间需求出现再加 |
