# Recipe 系统：无副作用的可复用 mod 预制清单

> 制定日期：2026-06-29
> 定位：Recipe 系统总纲。在完成 5 个前置任务后实施。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。
> Jira：[POLY-120](https://d3ara1n.atlassian.net/browse/POLY-120)（Recipe 主体任务，被 POLY-115/116/117/118/119 阻塞）
> 关联：源自 GitHub #73（整合包归属管理诉求）

---

## 0. 这是什么，为什么需要它

### 0.1 问题：纯 group by 不够，整合包又有副作用

GitHub #73 的原始诉求是"把整合包作为独立条目管理"。前期讨论确认了两件事：

- **纯 group by 没有意义**——现有筛选面板加个"按来源过滤"就做到了，不创造新能力，不动数据层。
- **多整合包合并是错误实践**——整合包携带文件、启动参数、`options.txt`，叠加只会带来不确定性和麻烦。一个实例绑定一个整合包是正确语义。

但 #73 背后真正的诉求——"快速给实例加一组预设的 mod、且能整体管理"——整合包做不到（副作用太重、不能跨实例复用），group by 做不到（只是视图）。**Recipe 就是补这个缺口的实体。**

### 0.2 Recipe 的定位

**Recipe = 命名的、可复用的、无副作用的 mod 浮动引用清单 + 元数据。**

| 特性 | Recipe | 整合包 | 手动添加 |
|------|--------|--------|----------|
| 携带文件/配置/启动参数 | ❌ | ✅ | ❌ |
| 可跨实例复用 | ✅ | ❌（绑死一个实例） | ❌ |
| 可整体管理（一组启停/移除） | ✅ | ✅ | ❌ |
| 导入即锁定 | ✅ | ✅ | — |
| 一个实例可挂多个 | ✅ | ❌ | — |
| 实例间同步 | ❌（导入即快照） | ❌（手动更新） | — |

Recipe 像"轻量级、可堆叠、可分享的整合包"——用户可以自己组装"QoL 大礼包""科技启动包"这类预制清单，导入到任意实例，多个 recipe 共存，每个 recipe 作为一组被整体管理。

### 0.3 与纯 group by 的本质区别

group by 是**视图层把已有信息换种摆法**；Recipe 引入**新的可复用实体**，创造"预先组装、跨实例分享、整体管理"的能力，这是 group by 无法提供的。

---

## 1. 前置任务依赖

Recipe 系统建立在 5 个前置任务之上，本任务**消费**它们的成果，不重复其内容：

| 前置任务 | Recipe 如何消费 |
|----------|----------------|
| [LOCKDATA-MODERNIZATION](https://d3ara1n.atlassian.net/browse/POLY-116) | recipe 导入即锁定，其 mod 版本不漂移 |
| [URL-SCHEME-UNIFICATION](URL-SCHEME-UNIFICATION.md) | recipe 用 `recipe://<id>` 作 `Entry.Source` 标识 |
| [SOURCE-REFERENCE-SEMANTICS](SOURCE-REFERENCE-SEMANTICS.md) | recipe 包 `Source != null` 即锁定；与整合包/手动三类共存 |
| [PACKAGE-GROUPING-UI](PACKAGE-GROUPING-UI.md) | recipe 包按 `Source` 自动成组，组级操作天然可用 |
| [DEPLOYMENT-PRIORITY-BY-GROUP](DEPLOYMENT-PRIORITY-BY-GROUP.md) | recipe 组有确定优先级（介于整合包与手动之间） |

**这些前置任务必须先完成。** 本文档只描述 Recipe 实体本身的设计：数据模型、Management 页面、实例链接流程、更新机制。

---

## 2. 目标与非目标

**目标**

1. Recipe 作为独立实体，支持创建、编辑、删除、导入、导出。
2. Recipe Management 顶层页面管理本地 recipe 库。
3. 实例可"链接"任意数量的 recipe：校验兼容性 → 合并进 `Profile.Setup.Packages`（`Source = recipe://<id>`）。
4. 链接后 recipe 包随前置任务的分组/锁定/优先级机制自然工作，无需额外特殊处理。
5. Recipe 更新走显式"重新链接"，导入后与 recipe 源不同步。

**非目标**

- 不实现 recipe 的"在线市场/分享平台"——首版只支持本地管理与文件导入导出。
- 不实现 recipe 的自动同步（recipe 改了实例不跟着改）。
- 不让 recipe 携带文件/配置——它纯是 mod 引用清单，副作用归整合包。
- 不改 `Entry` / `Profile` 结构（前置任务已就位）。
- 不在 recipe 内实现"条件 mod"（optional 标记首版仅作元数据，UI 不做交互式勾选）——见备选 F.4。

---

## 3. 核心设计

### 3.1 Recipe 数据模型

独立于 Profile，每个 recipe 一个 JSON 文件，存于 `PathDef.Default.PrivateDataDirectory()/recipes/<id>.json`（即 `~/.polymerium/recipes/`，Polymerium 宿主层目录，非 Trident 数据目录）。

```csharp
public class RecipeDocument
{
    public required string Id { get; init; }          // slug 形式，如 "qol-essentials"，文件名同 id
    public required string Name { get; set; }          // 展示名，如 "QoL 大礼包"
    public string? Description { get; set; }
    public string? IconPath { get; set; }              // 相对 recipe 文件的图标路径，或内置 asset key
    public IList<string> Tags { get; init; } = [];     // 分类标签，便于检索
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public required RecipeCompatibility Compatibility { get; init; }
    public required IList<RecipeEntry> Entries { get; init; }
}

public class RecipeCompatibility
{
    // 整体兼容性声明，链接时校验。空集合 = 不约束
    public IList<string>? GameVersions { get; init; }  // 如 ["1.20.1", "1.20.2"]
    public IList<string>? Loaders { get; init; }       // 如 ["fabric", "quilt"]
}

public class RecipeEntry
{
    public required string Purl { get; init; }         // 浮动 purl（PackageHelper.Identify 生成）
    public bool Optional { get; init; }                // 元数据：是否可选（首版不交互）
    public string? Note { get; init; }                 // 该项的说明（如"核心依赖，勿禁用"）
}
```

**关键点**：
- `Purl` 是**浮动 purl**（`vid` 为 null，带 `#version=`/`#loader=` filter）。这是 recipe 跨实例复用的核心——同一份 recipe 链接到不同版本/loader 的实例时，按 filter 现场解析出该实例对应的具体文件。复用 `PackageHelper.Identify`（见 URL-SCHEME-UNIFICATION 调研结论），不造新轮子。
- `Compatibility` 是**整体声明**，与每个 entry 的 filter 互补：filter 处理"哪个版本匹配"，Compatibility 处理"这个 recipe 适不适合这个实例"（链接前的门控）。
- 文件格式选 JSON 而非 zip：recipe 无文件载荷，纯文本便于用户手改、版本管理、分享。图标用单独文件或内置 asset key，避免二进制污染 JSON。

### 3.2 Recipe 文件格式（.recipe.json）

导出时用 `.recipe.json` 扩展名（区别于整合包 `.zip`，强调"无副作用、纯引用"）。icon 若为本地文件，导出时打包成 `.recipe.zip`（含 `recipe.json` + icon 文件）；无 icon 或 icon 为内置 asset key 时，直接导出 `.recipe.json`。

### 3.3 Recipe Management 页面

**位置**：顶层导航页（参考 `AccountsPage` / `MarketplacePortalPage` 范式，见调研 §2）。

新增：
- `Pages/RecipeManagementPage.axaml` + `PageModels/RecipeManagementPageModel.cs`
- `MainWindowContext.cs` 加 `GoRecipes` 命令；`MainWindow.axaml` 侧边栏加导航按钮。

**布局**：左列表 + 右详情范式，直接参照 `SnapshotManagementPage`（`Grid ColumnDefinitions="4*,Auto,6*"`，左 `ListBox` + 右 `PlaceholderContainer` 详情）。

**功能**：
- 创建：空白 recipe → 进入编辑（右侧详情或独立编辑 modal）
- 编辑：改 Name/Description/Tags/Compatibility，增删 Entries（从 Marketplace 搜索添加，或手填 purl）
- 删除：删本地 recipe 文件（不影响已链接该 recipe 的实例——见 §3.5）
- 导入：`.recipe.json` / `.recipe.zip` → 解析 → 存入 recipes 目录
- 导出：选 recipe → 文件保存对话框（参考 `MainWindowContext.ExportInstance` 的 StorageProvider 用法）→ 写文件

**recipe 列表数据源**：扫描 `recipes/` 目录反序列化所有 `.json`。用 DynamicData `SourceCache<RecipeDocument, string>`（key=Id），目录变更时 refresh。

### 3.4 实例链接 Recipe

**入口**：实例 Setup 页（`InstanceSetupPage`）的"添加"菜单（现有 `SplitButton` 下拉，调研 §3）加一项"链接 Recipe"。

**链接流程**：
```
1. 用户选 recipe（从 recipe 选择器，列出本地可用 recipe）
2. 校验 Compatibility：
   - recipe.GameVersions 与实例 Version 交集非空？loader 同理
   - 不兼容 → 提示并阻断（UI 警告，不强制）
3. 对 recipe 每个 Entry：
   a. purl 是浮动 → 链接时刻立即解析（PackagePlanner + RepositoryAgent）
      → 得到固定 vid（参考 PACKAGE-GROUPING 决策：链接即解析成固定写入）
   b. 构造 Profile.Rice.Entry:
      {
        Purl = 固定 purl（解析后）,
        Enabled = true,
        Source = InternalUri.Recipe(recipe.Id),   // recipe://<id>
        Tags = []  // 不继承 recipe 标签，避免污染实例标签语义
      }
   c. ProfileGuard.Value.Setup.Packages.Add(entry)  // 复用现有增量添加模式
4. guard.DisposeAsync() 触发持久化 + 刷新
```

**复用现有代码模式**（调研 §3）：
- `ProfileManager.TryGetMutable(key, out guard)` 获取写锁
- `guard.Value.Setup.Packages.Add(entry)`
- `guard.DisposeAsync()` 持久化 + `ProfileUpdated` 事件
- `persistenceService.AppendAction({ Kind = EditPackage, ... })` 记录操作历史

**链接时刻立即解析成固定**（而非保留浮动到部署时）：现有 `Entry.Purl` 全是固定的，链接即解析让 recipe 包与其他包行为一致，改动最小。recipe 本体仍存浮动定义，供下次链接到别的实例。

**冲突处理**：链接时若实例已有同 project 的包（来自整合包/手动/其他 recipe），不阻断——由 DEPLOYMENT-PRIORITY-BY-GROUP 的部署优先级解决（手动 > recipe > 整合包）。UI 可在链接后提示冲突（复用优先级任务的冲突报告）。

### 3.5 解除链接（移除 recipe 组）

分组 UI（PACKAGE-GROUPING-UI）已提供"整组移除"——recipe 组的移除 = 删除所有 `Source == recipe://<id>` 的 entry。

**关键设计**：recipe 文件的删除（Management 页）与实例链接的解除**完全解耦**——
- 删 recipe 文件 ≠ 自动从已链接实例移除。已链接实例里的包是独立快照，继续存在。
- 这样避免"我删了个 recipe，实例突然少了一堆 mod"的意外。删除时 Management 页提示"已有 N 个实例链接了此 recipe，删除后这些实例不受影响"。

### 3.6 Recipe 更新（重新链接）

**不同步**是 Recipe 的明确语义（与整合包一致）：recipe 改了，已链接的实例**不跟着变**。想更新需用户显式触发。

**更新入口**：分组 UI 的 recipe 组头加"更新到最新版本"按钮（PACKAGE-GROUPING-UI 已预留组级操作）。

**更新流程**：
```
1. 从 recipe 文件读当前定义（可能是用户编辑过的新 entries）
2. 对该 Source (recipe://<id>) 下的现有 entry：
   - recipe 仍含此 project → 按 recipe 新 purl 重新解析 → 更新 entry.Purl（可能升/降版本）
   - recipe 已删此 project → 移除该 entry（提示用户）
3. recipe 新增的 project → 添加为新 entry
4. 持久化
```

更新是"对齐到 recipe 当前定义"，不是"拉取上游最新版本"——后者由 LOCKDATA-MODERNIZATION 的 `UpdatePackageAsync` 承担（解除单包锁重解析）。两者正交：recipe 更新改"有哪些 mod"，lock 更新改"这些 mod 的具体版本"。

---

## 4. 改动面

| 层 | 文件/新增 | 改动 |
|----|-----------|------|
| 数据模型 | `Models/RecipeDocument.cs`（新增） | Recipe / RecipeEntry / RecipeCompatibility |
| 存储 | `Services/RecipeService.cs`（新增） | 扫描 recipes 目录、CRUD、导入导出、序列化 |
| 路径 | `PathDef` 用法（不扩展 Trident） | `Path.Combine(PathDef.Default.PrivateDataDirectory(), "recipes")` |
| ViewModel | `PageModels/RecipeManagementPageModel.cs`（新增） | 列表 + 详情 + 编辑命令 |
| 视图 | `Pages/RecipeManagementPage.axaml`（新增） | 左列表右详情，参照 SnapshotManagementPage |
| 导航 | `MainWindowContext.cs`、`MainWindow.axaml` | `GoRecipes` 命令 + 侧边栏按钮 |
| 链接入口 | `InstanceSetupPageModel.cs`、`InstanceSetupPage.axaml` | "添加"下拉加"链接 Recipe"项 + 链接流程 |
| 链接流程 | `Services/RecipeLinker.cs`（新增）或 InstanceService | 兼容校验 + 浮动解析 + 合并 Entry |
| 更新入口 | `PACKAGE-GROUPING-UI` 的组操作 | recipe 组"更新"按钮调 RecipeService |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | Management 页创建 recipe | 填名/描述/标签 → 存为 recipes/<id>.json |
| 2 | 编辑 recipe 加 entries | 从 Marketplace 搜索添加，存浮动 purl |
| 3 | 导出 recipe | 存 .recipe.json（无 icon）或 .recipe.zip（带 icon） |
| 4 | 导入 recipe 文件 | 解析进 recipes 目录，列表出现 |
| 5 | 实例链接 recipe | 兼容校验通过 → entries 解析成固定 → 写入 Packages，`Source = recipe://<id>` |
| 6 | 链接不兼容 recipe（版本/loader） | UI 警告阻断 |
| 7 | 链接后分组视图 | recipe 包自动成组（PACKAGE-GROUPING-UI） |
| 8 | 链接后版本锁定 | recipe 包版本不漂移（LOCKDATA-MODERNIZATION） |
| 9 | 删除 recipe 文件 | 已链接实例的包不受影响，继续存在 |
| 10 | recipe 组"更新" | entry 对齐到 recipe 当前定义 |
| 11 | 多 recipe 链接同一实例 | 各自独立成组，优先级按组顺序（DEPLOYMENT-PRIORITY） |
| 12 | recipe 与整合包同 mod 冲突 | 按 group 优先级解决，UI 警告 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| recipe 与已删 recipe 同名重新链接产生混淆 | Id 用 slug 且不允许重名；删除是软概念，Id 一旦占用不再回收（或加 `.deleted` 后缀规避） |
| 浮动 purl 解析失败（仓库无匹配版本） | 链接时单包失败不阻断整体，该 entry 标记为"解析失败"跳过，UI 提示 |
| recipe 文件手改导致 JSON 损坏 | 加载时 try-catch，损坏文件在列表标红，不阻塞其他 recipe |
| recipe 链接到实例后仓库下架了某版本 | 由 LOCKDATA 的"锁失效"降级机制处理（下载失败提示手动更新） |
| recipe 数量多时目录扫描慢 | 首版规模可控；真出现性能问题再加索引文件（见备选 F.2） |
| "更新"操作批量改实例，用户误操作 | 更新前显示 diff 预览（新增 X / 移除 Y / 版本变更 Z），二次确认 |

---

## 7. 不做的事（明确边界）

- **不实现 recipe 在线市场/分享平台** —— 首版纯本地 + 文件导入导出。
- **不自动同步 recipe 到实例** —— 显式链接/更新，快照语义。
- **不携带文件/配置** —— recipe 纯引用清单，副作用归整合包。
- **不支持 recipe 嵌套**（recipe 引用别的 recipe）—— 增加复杂度，首版不做。
- **不做 recipe 版本号** —— recipe 本身不版本化，"改了"就是新定义，更新时对齐即可。
- **不继承 recipe 标签到实例 entry** —— 实例 Tags 是用户对包的自定义分类，recipe 标签是 recipe 的分类，语义不同。

---

## 8. 触发实施的条件

5 个前置任务全部完成后，即可启动本任务。建议实施顺序：

1. 数据模型 + RecipeService（存储 CRUD）—— 可独立验证
2. Recipe Management 页面（创建/编辑/导入导出）—— 不依赖实例
3. 实例链接流程 —— 接通 Profile，依赖前置任务就位
4. 更新机制 + 分组 UI 组操作接通 —— 最后完善

---

## 附录：备选方案备案

### F.1 Recipe 存储：文件 vs SQLite

当前选：**每 recipe 一个 JSON 文件**（`recipes/<id>.json`）

| 备选 | 做法 | 取舍 |
|------|------|------|
| SQLite 表 | 像 FavoriteProject 那样存 | 不便导入导出（要导出还得序列化）、不便用户手改、不便版本管理；recipe 是文件级资产，JSON 更自然 |
| 单聚合 JSON | 一个 `recipes.json` 存全部 | 文件级并发风险、导入一个 recipe 要重写全文件；不如每 recipe 独立 |

### F.2 Recipe 列表数据源

当前选：**目录扫描反序列化**

| 备选 | 做法 | 取舍 |
|------|------|------|
| 维护 `index.json` 索引 | 扫描结果缓存 | 首版规模（几十个）无需；索引同步增加复杂度，真出现性能问题再加 |
| SQLite 元数据 + 文件内容 | 元数据快查，内容按需读 | 过度设计，recipe 数量级不支撑 |

### F.3 链接时刻：立即解析 vs 保留浮动

当前选：**链接即解析成固定写入**（§3.4）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 保留浮动 purl 到部署时解析 | Entry.Purl 存浮动 | 现有 Entry.Purl 全是固定，保留浮动要求部署管线处处处理浮动，改动大；链接即解析让 recipe 包行为与手动包一致 |
| 存浮动 + 锁定 vid | 双字段 | 已由 LOCKDATA-MODERNIZATION 统一处理，不需 recipe 特殊做 |

### F.4 Optional entry 的交互

当前选：**Optional 仅作元数据，首版不交互**（§2 非目标）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 链接时弹窗逐项勾选 | 用户选要哪些 optional | 增加流程复杂度；首版"导入即全要"更简单，交互式勾选按需迭代 |
| optional 默认不导入 | 仅导入必选项 | 与"导入即锁定一组"语义冲突，用户难预期 |

### F.5 Recipe 文件扩展名

当前选：**`.recipe.json` / `.recipe.zip`**

| 备选 | 做法 | 取舍 |
|------|------|------|
| 统一 `.zip` | 都打包 | 纯文本 recipe 也得进 zip，用户无法直接查看/手改，违背"便于分享手改"初衷 |
| `.json`（无 recipe 前缀） | 直接 json | 与 profile.json 等易混淆；`.recipe.json` 明确语义 |

### F.6 更新的粒度

当前选：**整体对齐到 recipe 当前定义**（§3.6）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 逐项选择更新 | 用户选更新哪些 | 粒度太细，recipe 更新语义是"同步到新定义"；逐项选择更像单包操作，用 LOCKDATA 的 UpdatePackageAsync |
| 仅提示有更新，用户手动逐个改 | 不提供批量更新 | 失去 recipe 整体管理的价值；用户得一个个操作，体验差 |
