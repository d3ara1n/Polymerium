# GC 功能设计文档

## 概述

在设置页面的维护板块中新增「垃圾回收」按钮，点击后弹出确认对话框向用户展示将要清理的内容，确认后弹出 ProgressModal 展示清理进度，完成后通知用户结果。

---

## 1. UI 层

### 1.1 入口：SettingsPage 维护板块

在现有 DataManagement (`SettingsEntryItem`) 的 `StackPanel` 中，追加一个按钮：

```xml
<!-- SettingsPage.axaml, 约第 611-623 行 -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <Button Classes="Small" Command="{Binding ClearStatisticsCommand}" ... />
    <Button Classes="Small" Command="{Binding ClearRecordsCommand}" ... />
    <!-- 新增 -->
    <Button Classes="Small" Command="{Binding GarbageCollectCommand}"
            Content="{x:Static lang:Resources.SettingsPage_GarbageCollectButtonText}" />
</StackPanel>
```

按钮与现有按钮一样受 SafeLock 控制（`IsEnabled="{Binding #Lock.IsUnlocked}"`），无需额外处理。

### 1.2 确认对话框

复用 `OverlayService.RequestConfirmationAsync()` 模式。确认消息需要向用户说明：

- 将清理 persistence 数据库中不再关联任何实例的数据行
- 将清理各实例快照数据库中无主的引用记录
- 将清理快照对象存储中不被任何引用记录指向的文件
- 将清理不再关联任何账号的实例-账号选择器映射
- 清理是安全的，不会影响现有实例的数据

### 1.3 进度展示：ProgressModal

复用现有 `ProgressModal`，采用确定进度模式（`IsIndeterminate = false`），步骤计数作为进度值。

```
步骤总数 = N（由扫描结果决定）
进度值   = 已完成步骤数
StatusText = 当前正在处理的描述
```

完成后 `Dismiss()`，弹出通知报告清理结果（清理了多少条记录、多少个文件）。

---

## 2. ViewModel 层

### 2.1 SettingsPageModel 新增

```csharp
// 新增依赖注入：SnapshotManager, ProfileManager
// 构造函数追加参数

[RelayCommand]
private async Task GarbageCollectAsync()
{
    // Phase 1: 扫描 — 确认对话框中展示扫描结果
    var report = await ScanOrphanDataAsync();

    var message = FormatScanReport(report); // 拼接展示文本
    var confirmed = await OverlayService.RequestConfirmationAsync(
        message,
        Resources.SettingsPage_GarbageCollectConfirmationTitle
    );
    if (!confirmed) return;

    // Phase 2: 清理 — ProgressModal
    var progress = new ProgressModal
    {
        Title = Resources.SettingsPage_GarbageCollectProgressTitle,
        IsIndeterminate = false,
    };
    OverlayService.PopModal(progress);

    try
    {
        var result = await Task.Run(() => ExecuteGarbageCollectAsync(report, progress));
        progress.Dismiss();
        _notificationService.PopMessage(
            FormatResultMessage(result),
            Resources.SettingsPage_GarbageCollectSuccessTitle,
            GrowlLevel.Success
        );
    }
    catch (Exception ex)
    {
        progress.Dismiss();
        _notificationService.PopMessage(ex, Resources.SettingsPage_GarbageCollectDangerTitle);
    }
}
```

**注意**：`progress.StatusText` 和 `progress.ProgressValue` 的设置需要通过 `Dispatcher.UIThread.Post()` 回到 UI 线程，与 `SnapshotManagementPageModel.RestoreAsync` 中的用法一致。

---

## 3. 服务层：GarbageCollector

新建 `src/Polymerium.Avalonia/Services/GarbageCollector.cs`，作为单例服务注册到 DI。

### 3.1 扫描阶段

```csharp
public class GarbageCollector
{
    public GarbageCollector(
        IFreeSql freeSql,
        SnapshotManager snapshotManager,
        ProfileManager profileManager
    ) { ... }

    public GarbageCollectReport Scan()
    {
        var activeKeys = _profileManager.Profiles.Select(x => x.Key).ToHashSet();
        var activeUuids = _freeSql.Select<PersistenceService.Account>()
            .ToList(x => x.Uuid).ToHashSet();

        var report = new GarbageCollectReport();

        // 1. Persistence 数据库：Key 无主的行
        report.OrphanAccountSelectors = _freeSql.Select<PersistenceService.AccountSelector>()
            .ToList().Count(x => !activeKeys.Contains(x.Key));
        // 同时检查 Uuid 无主的
        report.DanglingAccountSelectors = _freeSql.Select<PersistenceService.AccountSelector>()
            .ToList().Count(x => !activeUuids.Contains(x.Uuid));

        report.OrphanActions = _freeSql.Select<PersistenceService.Action>()
            .ToList().Count(x => !activeKeys.Contains(x.Key));
        report.OrphanActivities = _freeSql.Select<PersistenceService.Activity>()
            .ToList().Count(x => !activeKeys.Contains(x.Key));
        report.OrphanViewStates = _freeSql.Select<PersistenceService.ViewState>()
            .ToList().Count(x => !activeKeys.Contains(x.Key));
        report.OrphanWidgetLocalSections = _freeSql.Select<PersistenceService.WidgetLocalSection>()
            .ToList().Count(x => !activeKeys.Contains(x.Key));

        // 2. 快照：遍历每个活跃实例
        foreach (var key in activeKeys)
        {
            var snapshotDir = PathDef.Default.DirectoryOfSnapshots(key);
            if (!Directory.Exists(snapshotDir)) continue;

            using var handle = _snapshotManager.Open(key);

            // 孤儿 ReferenceRecord：SnapshotId 不在 SnapshotRecord 中
            var snapshotIds = handle.List().Select(x => x.Id).ToHashSet();
            // 需要直接查 SnapshotStore，或给 ISnapshotStore 加方法
            // 见 3.3 扩展点

            // 快照对象文件：不在任何 ReferenceRecord.Hash 中的文件
            var referencedHashes = handle.Store.GetAllReferencedHashes();
            var objectsDir = PathDef.Default.DirectoryOfSnapshotObjects(key);
            if (Directory.Exists(objectsDir))
            {
                var diskHashes = new HashSet<string>();
                foreach (var prefixDir in Directory.GetDirectories(objectsDir))
                foreach (var file in Directory.GetFiles(prefixDir))
                    diskHashes.Add(Path.GetFileName(file));

                report.OrphanSnapshotObjects += diskHashes.Count(x => !referencedHashes.Contains(x));
            }
        }

        return report;
    }
}
```

### 3.2 清理阶段

```csharp
public GarbageCollectResult Execute(
    GarbageCollectReport report,
    ProgressModal progress
)
{
    var result = new GarbageCollectResult();
    var activeKeys = _profileManager.Profiles.Select(x => x.Key).ToHashSet();
    var activeUuids = _freeSql.Select<PersistenceService.Account>()
        .ToList(x => x.Uuid).ToHashSet();

    var totalSteps = /* 计算总步骤 */;
    var completed = 0;

    // Step 1: 清理 persistence 中 Key 无主的行
    UpdateProgress(progress, ref completed, totalSteps, "Cleaning persistence data...");
    result.DeletedAccountSelectors += _freeSql.Delete<PersistenceService.AccountSelector>()
        .Where(x => !activeKeys.Contains(x.Key)).ExecuteAffrows();
    result.DeletedAccountSelectors += _freeSql.Delete<PersistenceService.AccountSelector>()
        .Where(x => !activeUuids.Contains(x.Uuid)).ExecuteAffrows();
    result.DeletedActions += _freeSql.Delete<PersistenceService.Action>()
        .Where(x => !activeKeys.Contains(x.Key)).ExecuteAffrows();
    result.DeletedActivities += _freeSql.Delete<PersistenceService.Activity>()
        .Where(x => !activeKeys.Contains(x.Key)).ExecuteAffrows();
    result.DeletedViewStates += _freeSql.Delete<PersistenceService.ViewState>()
        .Where(x => !activeKeys.Contains(x.Key)).ExecuteAffrows();
    result.DeletedWidgetLocalSections += _freeSql.Delete<PersistenceService.WidgetLocalSection>()
        .Where(x => !activeKeys.Contains(x.Key)).ExecuteAffrows();

    // Step 2: 清理快照孤儿
    foreach (var key in activeKeys)
    {
        UpdateProgress(progress, ref completed, totalSteps, $"Cleaning snapshots: {key}");
        // 清理孤儿 ReferenceRecord
        // 清理孤儿对象文件
    }

    return result;
}
```

### 3.3 需要对 SnapshotStore 的扩展

当前 `ISnapshotStore` 接口没有暴露获取孤儿 `ReferenceRecord` 的方法。需要新增：

```csharp
// ISnapshotStore.cs 新增
ISet<Guid> GetOrphanSnapshotIds();
// 返回 ReferenceRecord 中存在但 SnapshotRecord 中不存在的 SnapshotId 集合

int DeleteReferencesBySnapshotIds(IEnumerable<Guid> snapshotIds);
// 批量删除指定 SnapshotId 的 ReferenceRecord
```

```csharp
// SnapshotStore.cs 实现
public ISet<Guid> GetOrphanSnapshotIds()
{
    var existingSnapshotIds = _freeSql.Select<SnapshotRecord>().ToList(x => x.Id).ToHashSet();
    var referencedSnapshotIds = _freeSql.Select<ReferenceRecord>()
        .ToList(x => x.SnapshotId).ToHashSet();
    referencedSnapshotIds.ExceptWith(existingSnapshotIds);
    return referencedSnapshotIds;
}

public int DeleteReferencesBySnapshotIds(IEnumerable<Guid> snapshotIds)
{
    var ids = snapshotIds.ToList();
    return _freeSql.Delete<ReferenceRecord>()
        .Where(x => ids.Contains(x.SnapshotId))
        .ExecuteAffrows();
}
```

> 同时需要在 `SnapshotManager.InstanceSnapshots` 上暴露对应方法，或者 GC 直接通过 `ISnapshotStoreFactory` 打开 store 操作。推荐后者，因为 GC 本身就是底层操作。

### 3.4 FreeSql where 条件注意事项

FreeSql 的 `Where` 中不能直接用 `!activeKeys.Contains(x.Key)` 这样的客户端集合操作，因为 SQLite 提供者可能无法将其翻译为 SQL。替代方案：

- **方案 A**：先查出所有行，在内存中过滤，再按主键批量删除
- **方案 B**：对每个 orphan key 逐个删除（循环调用）

推荐方案 A，因为扫描阶段已经收集了所有 Key，清理时可以直接用：

```csharp
var orphanKeys = _freeSql.Select<PersistenceService.AccountSelector>()
    .ToList(x => x.Key)
    .Where(k => !activeKeys.Contains(k))
    .ToList();

if (orphanKeys.Count > 0)
{
    result.DeletedAccountSelectors += _freeSql.Delete<PersistenceService.AccountSelector>()
        .Where(x => orphanKeys.Contains(x.Key))
        .ExecuteAffrows();
}
```

> `Contains` 在 FreeSql 中会被翻译为 `IN (...)` SQL 语句，这在 SQLite 中是支持的。

---

## 4. 数据模型

```csharp
// GarbageCollector.cs 内部或单独文件
public class GarbageCollectReport
{
    public int OrphanAccountSelectors { get; set; }
    public int DanglingAccountSelectors { get; set; }
    public int OrphanActions { get; set; }
    public int OrphanActivities { get; set; }
    public int OrphanViewStates { get; set; }
    public int OrphanWidgetLocalSections { get; set; }
    public int OrphanSnapshotReferences { get; set; }
    public int OrphanSnapshotObjects { get; set; }

    public bool HasOrphans => OrphanAccountSelectors > 0
        || DanglingAccountSelectors > 0
        || OrphanActions > 0
        || OrphanActivities > 0
        || OrphanViewStates > 0
        || OrphanWidgetLocalSections > 0
        || OrphanSnapshotReferences > 0
        || OrphanSnapshotObjects > 0;

    public int TotalOrphans =>
        OrphanAccountSelectors + DanglingAccountSelectors
        + OrphanActions + OrphanActivities
        + OrphanViewStates + OrphanWidgetLocalSections
        + OrphanSnapshotReferences + OrphanSnapshotObjects;
}

public class GarbageCollectResult
{
    public int DeletedAccountSelectors { get; set; }
    public int DeletedActions { get; set; }
    public int DeletedActivities { get; set; }
    public int DeletedViewStates { get; set; }
    public int DeletedWidgetLocalSections { get; set; }
    public int DeletedSnapshotReferences { get; set; }
    public int DeletedSnapshotFiles { get; set; }

    public int TotalDeleted =>
        DeletedAccountSelectors + DeletedActions + DeletedActivities
        + DeletedViewStates + DeletedWidgetLocalSections
        + DeletedSnapshotReferences + DeletedSnapshotFiles;
}
```

---

## 5. DI 注册

```csharp
// Startup.cs, App 区域追加
.AddSingleton<GarbageCollector>()
```

`GarbageCollector` 依赖 `IFreeSql`、`SnapshotManager`、`ProfileManager`，这些均已注册为单例。

`SettingsPageModel` 构造函数追加 `GarbageCollector` 参数。

---

## 6. 本地化字符串

### Resources.resx（英文）

```xml
<data name="SettingsPage_GarbageCollectButtonText" xml:space="preserve">
    <value>Garbage Collect</value>
</data>
<data name="SettingsPage_GarbageCollectConfirmationTitle" xml:space="preserve">
    <value>Garbage Collect</value>
</data>
<data name="SettingsPage_GarbageCollectConfirmationMessage" xml:space="preserve">
    <value>The following orphan data will be cleaned:
{0}
This operation is safe and will not affect existing instance data.</value>
</data>
<data name="SettingsPage_GarbageCollectProgressTitle" xml:space="preserve">
    <value>Garbage Collecting</value>
</data>
<data name="SettingsPage_GarbageCollectSuccessTitle" xml:space="preserve">
    <value>Garbage Collected</value>
</data>
<data name="SettingsPage_GarbageCollectDangerTitle" xml:space="preserve">
    <value>Garbage Collect Failed</value>
</data>
<data name="SettingsPage_GarbageCollectReportAccountSelectors" xml:space="preserve">
    <value>Instance-Account mappings: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportActions" xml:space="preserve">
    <value>Action records: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportActivities" xml:space="preserve">
    <value>Play activities: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportViewStates" xml:space="preserve">
    <value>View states: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportWidgetSections" xml:space="preserve">
    <value>Widget data: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportSnapshotReferences" xml:space="preserve">
    <value>Snapshot references: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportSnapshotObjects" xml:space="preserve">
    <value>Snapshot files: {0}</value>
</data>
<data name="SettingsPage_GarbageCollectReportNone" xml:space="preserve">
    <value>No orphan data found.</value>
</data>
```

### Resources.zh-hans.resx（中文）

```xml
<data name="SettingsPage_GarbageCollectButtonText" xml:space="preserve">
    <value>垃圾回收</value>
</data>
<data name="SettingsPage_GarbageCollectConfirmationTitle" xml:space="preserve">
    <value>垃圾回收</value>
</data>
<data name="SettingsPage_GarbageCollectConfirmationMessage" xml:space="preserve">
    <value>以下孤儿数据将被清理：
{0}
此操作是安全的，不会影响现有实例的数据。</value>
</data>
<data name="SettingsPage_GarbageCollectProgressTitle" xml:space="preserve">
    <value>正在回收</value>
</data>
<data name="SettingsPage_GarbageCollectSuccessTitle" xml:space="preserve">
    <value>回收完成</value>
</data>
<data name="SettingsPage_GarbageCollectDangerTitle" xml:space="preserve">
    <value>回收失败</value>
</data>
<data name="SettingsPage_GarbageCollectReportAccountSelectors" xml:space="preserve">
    <value>实例-账号映射：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportActions" xml:space="preserve">
    <value>操作记录：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportActivities" xml:space="preserve">
    <value>游玩记录：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportViewStates" xml:space="preserve">
    <value>视图状态：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportWidgetSections" xml:space="preserve">
    <value>小组件数据：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportSnapshotReferences" xml:space="preserve">
    <value>快照引用记录：{0} 条</value>
</data>
<data name="SettingsPage_GarbageCollectReportSnapshotObjects" xml:space="preserve">
    <value>快照文件：{0} 个</value>
</data>
<data name="SettingsPage_GarbageCollectReportNone" xml:space="preserve">
    <value>未发现孤儿数据。</value>
</data>
```

---

## 7. 需要修改的文件清单

| 文件 | 改动 |
|------|------|
| `src/Polymerium.Avalonia/Services/GarbageCollector.cs` | **新建** — 扫描 + 清理逻辑 |
| `src/Polymerium.Avalonia/PageModels/SettingsPageModel.cs` | 注入 `GarbageCollector`，新增 `GarbageCollectCommand` |
| `src/Polymerium.Avalonia/Pages/SettingsPage.axaml` | 维护板块 DataManagement 区域追加 GC 按钮 |
| `src/Polymerium.Avalonia/Startup.cs` | DI 注册 `GarbageCollector` |
| `src/Polymerium.Avalonia/Properties/Resources.resx` | 新增英文字符串 |
| `src/Polymerium.Avalonia/Properties/Resources.zh-hans.resx` | 新增中文字符串 |
| `src/Polymerium.Avalonia/Properties/Resources.Designer.cs` | 重新生成或手动添加属性 |
| `submodules/Trident.Net/src/TridentCore.Abstractions/Snapshots/ISnapshotStore.cs` | 新增 `GetOrphanSnapshotIds()` 和 `DeleteReferencesBySnapshotIds()` |
| `src/Polymerium.Avalonia/Snapshots/SnapshotStore.cs` | 实现上述两个新方法 |

---

## 8. 不清理的内容

根据要求，**缓存目录 (`cache/`) 下的所有资源不算清理对象**：

- `cache/packages/` — 包缓存
- `cache/assets/` — Minecraft 资源
- `cache/libraries/` — Java 库
- `cache/runtimes/` — Java 运行时
- `cache/icons/` — 图标缓存
- `cache.sqlite.db` — HTTP 缓存

这些已有或将有其他清理机制。

---

## 9. 交互流程图

```
用户点击 "Garbage Collect" 按钮
        │
        ▼
SettingsPageModel.GarbageCollectAsync()
        │
        ├─► GarbageCollector.Scan() (后台线程)
        │       │
        │       ├─ 收集 activeKeys (当前实例)
        │       ├─ 收集 activeUuids (当前账号)
        │       ├─ 扫描 persistence 孤儿行
        │       └─ 扫描快照孤儿
        │
        ▼
  FormatScanReport(report) → 拼接确认消息
        │
        ▼
  OverlayService.RequestConfirmationAsync(message, title)
        │
   ┌────┴────┐
   │ Cancel  │ Confirm
   ▼         ▼
  返回     弹出 ProgressModal
              │
              ├─► GarbageCollector.Execute() (后台线程)
              │       │
              │       ├─ 清理 persistence 孤儿行
              │       ├─ 清理快照孤儿引用
              │       └─ 清理快照孤儿文件
              │
              ▼
         ProgressModal.Dismiss()
              │
              ▼
         NotificationService.PopMessage(结果摘要)
```
