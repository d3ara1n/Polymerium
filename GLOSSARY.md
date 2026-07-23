# Polymerium Terminology

This glossary defines the canonical user-facing terms for Polymerium. Use these names consistently in the app, documentation, changelog, issue descriptions, and support messages. Explain a Polymerium-specific term on first use instead of replacing it with an unrelated generic term.

## Core Concepts

| English | Chinese | Definition and usage |
| --- | --- | --- |
| Polymerium launcher / instance manager | Polymerium 启动器 | The application. The product is positioned as an instance manager — its scope is broader than launching the game — but "launcher" is the market-facing category name because it is what users immediately recognize. Use "launcher" when naming the product category; "instance manager" remains accurate when describing the product's actual scope. |
| instance | 实例 | A Minecraft environment managed by Polymerium, including its profile and managed directories. |
| profile / instance profile | 配置 / 实例配置 | The metadata that defines an instance, such as the Minecraft version, loader, packages, and rules. Use "profile" for this representation, not as a synonym for the whole instance. |
| modpack | 整合包 | A distributable collection of game versions, packages, configuration, and other source files. Do not shorten it to "package" when the complete modpack is meant. |
| package | 包 | A managed reference to content from a resource repository. Packages include mods, resource packs, shader packs, data packs, and worlds; "package" and "mod" are not synonyms. |
| asset | 资产 | A physical file managed by Polymerium, such as a configuration file, screenshot, log, library, or version file. An asset is not the same concept as a repository package. |
| resource kind | 资源类型 | The category of a package, such as mod, resource pack, shader pack, data pack, or world. |
| resource repository | 资源仓库 | A source that hosts packages and their versions, such as Modrinth or CurseForge. |
| package reference / Pref | 包引用 / Pref | A stable reference to a repository package or version, commonly represented by a `pref://` URI. Keep `Pref` when discussing the format itself. |
| dependency | 依赖 | A package required by another package. |
| dependent package | 依赖者 / 被依赖包 | A package that depends on the package currently being inspected. |
| tag | 标签 | A user-defined label used to organize packages or target deployment rules. |

## Instance Lifecycle

| English | Chinese | Definition and usage |
| --- | --- | --- |
| deploy / deployment | 部署 | Turn an instance profile, packages, pack source, and local data into a runnable game directory. Deployment prepares files; it does not start Minecraft. |
| launch / play | 启动 / 开始游戏 | Start Minecraft. Launching may trigger deployment first, but launch and deployment remain distinct operations. |
| reset instance | 重置实例 | Permanently clear the run directory and deployment lock so it can be deployed again. Reset can delete saves, screenshots, logs, and game or mod configuration unless they were already moved to Local Data. It does not delete the Pack Source or Local Data. Never describe reset as harmless cleanup. |
| update modpack | 更新整合包 | Replace or update the Pack Source and refresh its working copy while preserving Local Data. An update is not the same operation as reset. |
| snapshot | 快照 | A restorable state containing the instance profile and Polymerium-managed data. A snapshot is not automatically a complete backup of every unmanaged file in the run directory. |
| shared cache | 共享缓存 | Downloaded packages, Minecraft assets, libraries, and other reusable files shared by instances. Cache entries are not user data. |
| symbolic link / symlink | 符号链接 | A filesystem link used to place shared or managed files into an instance without duplicating them. Use "符号链接", not "软链接", in Chinese documentation. |
| deployment rule | 部署规则 | A rule that changes how matching packages are deployed, such as skipping or redirecting them. |
| selector | 选择器 | The condition that chooses which packages a deployment rule applies to. |
| rule action | 规则操作 | The effect applied by a deployment rule after its selector matches. |

## Managed Directories

Use the product label followed by the physical directory on first mention, for example "Run Directory (`build/`)" or "运行目录（`build/`）".

| English | Chinese | Definition and usage |
| --- | --- | --- |
| Pack Source (`import/`) | 整合包源（`import/`） | The authoritative files supplied by a modpack or prepared for export. A modpack update may replace them; resetting does not delete them. Reserve "import" / "导入" by itself for the action. |
| Local Data (`persist/`) | 本地保留（`persist/`） | User-managed data that Polymerium preserves across reset and modpack updates. Files are protected only after the user explicitly places them here. In architecture-oriented prose, this directory may also be called the persistence layer / 持久层. |
| Run Directory (`build/`) | 运行目录（`build/`） | The deployed game directory that Minecraft reads and writes as its game home. It contains both reproducible files and potentially irreplaceable user data. Reset permanently clears it. |
| working copy | 工作副本 | The runtime copy under the Run Directory of files originating from the Pack Source. Workspace compares this copy with its source. Do not use "working copy" for the entire Run Directory. |
| Workspace | 工作区 | The tool that compares the Pack Source with its working copy and synchronizes selected changes between them. |
| Sync to Import | 同步到 Import | Copy or synchronize a selected working-copy change into the Pack Source so it becomes part of the modpack. "Promote" / "提升" may explain the concept, but the UI command remains "Sync to Import". |
| restore working copy | 还原工作副本 | Replace a selected working-copy file with the version from the Pack Source. Distinguish this from restoring an entire snapshot. |

## Runtime And Accounts

| English | Chinese | Definition and usage |
| --- | --- | --- |
| Minecraft version | Minecraft 版本 | The selected version of Minecraft itself. Do not call it a runtime. |
| mod loader | 模组加载器 | Fabric, Forge, NeoForge, Quilt, or another system that loads mods. Use the full term in explanatory text. |
| loader libraries | 加载器库 | Libraries required by a mod loader. Do not call these a Java runtime. |
| Java runtime | Java 运行时 | A Java installation and executable used to start Minecraft. "Runtime" alone is acceptable only where the Java context is already explicit. |
| game account | 游戏账号 | A Microsoft, offline, trial, or external-auth account used to launch the game. Prefer "账号" consistently in Chinese UI. |
| authentication | 身份验证 | Verification of an account or session. Do not use it as a synonym for account configuration. |

## Integrations And Formats

| English | Chinese | Definition and usage |
| --- | --- | --- |
| attachment | 实例附件 | A file associated with the instance itself, such as its icon, README, CHANGELOG, or LICENSE. |
| version lock / lock data | 版本锁 / 锁定数据 | Generated deployment state that records resolved versions. It is not user data and reset may remove it. |
| Model Context Protocol (MCP) | 模型上下文协议（MCP） | The protocol through which AI tools can operate Polymerium. Expand the name on first use. |
| export | 导出 | Produce a distributable modpack archive or another supported format from an instance. |
| import | 导入 | Create or update an instance from a modpack archive or supported format. Do not use this bare term as the name of the `import/` directory. |

## Writing Rules

- Use canonical product labels (Run Directory, Pack Source, Local Data) in user-facing UI and documentation. Code and internal identifiers keep the physical names `build`/`import`/`persist`.
- Physical names may still appear in prose that describes where files actually live — e.g. "files in build", "from the import layer", "stored in persist" — since that states a physical fact rather than naming a concept.
- Introduce unfamiliar Polymerium concepts with a short plain-language definition, then reuse the term consistently.
- State data-loss boundaries explicitly. In particular, reset clears the Run Directory and can delete player saves and configuration; only data already in Local Data is guaranteed to survive.
- Distinguish logical references from physical files: packages are references, assets are files, and mods are one package kind.
- Distinguish preparation from execution: deployment prepares the game directory, while launch starts the game.
