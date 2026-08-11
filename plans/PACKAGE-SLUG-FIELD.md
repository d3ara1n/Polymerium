# Package/Project Slug 字段计划

> 状态：构想，未实施。为 `Package` 与 `Project` 抽象模型增加 `Slug` 字段，支撑 V3 包文件名与展示消费；实施时对照当下真实代码临场决定 HOW，不回写本文件。

## 做什么

- 抽象模型 `Package` 与 `Project`（`TridentCore.Abstractions/Repositories/Resources/`）增加 `Slug` 字段，各 Repository 构造时从已有项目信息填充。

## 想要什么效果

- 包/项目获得 API 权威的可读标识（slug），可作 V3 包文件名（`{repository}.{slug}.json`）、UI 显示、URL 构造的素材。
- 身份键不变：pref 的 ProjectId 仍是唯一身份，slug 是展示/命名标识。

## 调研结论（注意事项）

- **slug 在所有 Package 构造点都可得**：Modrinth 构造路径 `ToPackage(label, project, version, ...)` 持有 `ProjectInfo`（含 `Slug`）；CurseForge 路径 `ToPackage(label, mod, file)` 持有 `ModInfo`（含 `Slug`）。填充是纯增量，无需额外请求。
- **slug 是项目层属性，版本/文件层没有**：Modrinth `VersionInfo`、CurseForge `FileInfo` 均无 slug（但持 ProjectId/ModId 可关联）。Package 是版本层记录，其 slug 从同次解析的项目信息带出。
- **各来源 slug 现状**：Modrinth `ProjectInfo`/`SearchHit` ✓；CurseForge `ModInfo` ✓；Packwiz `mods/*.toml` 文件名即 slug 语义（生态惯例），但 Trident 的 Packwiz 路径是隐藏 modpack 仓库（GitHub repo 身份），无独立项目 slug，且导入时消费 `[update]` 块 ID 而非文件名。
- **slug 可能变化**：项目改名会改 slug，属罕见事件，不做身份用途即可接受；V3 场景下改名表现为包文件 delete + add，符合 git 语义。
- **字段可空**：若某来源无 slug 概念（如 Packwiz repo），`Slug` 为 null，消费点自行退化（退回 ProjectId 命名）。
- **消费点广**：V3 文件名（`plans/TRIDENT-V3-MIGRATION.md`）、UI 包名显示、URL 构造——用户手动创建包文件时文件名随意，程序创建时 slug 必得（添加包必先查仓库）。

## 关联

- V3 迁移计划：`plans/TRIDENT-V3-MIGRATION.md`
- V3 规范：`notes/TRIDENT_V3.md`
