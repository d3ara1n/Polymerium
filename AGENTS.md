# AGENTS

## Repo Shape

- This repo is a .NET 10 solution rooted at `Polymerium.slnx`.
- `src/Polymerium.Avalonia` is the only app in this repo.
- `submodules/Trident.Net` is a git submodule and is part of the solution build. Treat it as an integral part of this project: it participates in the same development workflow and should be edited freely alongside the main codebase. Do not treat submodule changes as out-of-scope — feel free to modify files under `submodules/Trident.Net` when the task requires it. `Huskui.Avalonia` is consumed as a NuGet package, not a submodule.
- Fresh clones need submodules initialized: `git submodule update --init --recursive`.
- `plans/` holds task blueprints — independent design docs written so a fresh session can pick up a task from the plan alone, without re-deriving the decisions or reconstructing progress from code. See `plans/README.md` for the writing conventions; treat `plans/archived/` as a graveyard (no reference value, do not read).
- `GLOSSARY.md` defines canonical user-facing Polymerium terminology. Follow it when writing app strings, docs, changelog entries, issue text, or support messages.

@GLOSSARY.md

## Documentation Website

- The project's public-facing docs site lives at `website/` — a Next.js app built with [Fumadocs](https://fumadocs.dev).
- Deployed on Vercel at **polymerium.dearain.dev**.
- Content is written in MDX under `website/content/docs/`, organized into sections: `getting-started`, `concepts`, `managing`, `advanced`, `guides`, `comparisons`.
- Every `.mdx` page has a Chinese counterpart (`.zh.mdx`). When editing content, update both files.
- Navigation structure is defined per-section via `meta.json` (and `meta.zh.json`).
- Dev server: `cd website && npm run dev` → opens at `http://localhost:3000`.
- Build: `cd website && npm run build`. Post-build syncs search index to Algolia via `scripts/sync-algolia.mjs`.

## Verified Commands

- Full solution build: `dotnet build "Polymerium.slnx"`
- Focused app build: `dotnet build "src/Polymerium.Avalonia/Polymerium.Avalonia.csproj"`
- There are no test projects in this repo right now. `dotnet test "Polymerium.slnx"` is not a meaningful verification step; use build plus targeted checks instead.
- **Do NOT run any formatting tools** (`scripts/Format-Files.ps1`, `csharpier`, `xstyler`, etc.). They operate across the entire repo including submodules and will produce unintended changes, and can also corrupt parts of the code. Only the user may invoke formatting.

## Architecture Entry Points

- App bootstrap starts in `src/Polymerium.Avalonia/Program.cs`.
- DI wiring lives in `src/Polymerium.Avalonia/Startup.cs`.
- Window construction, global exception hooks, and startup of lifetime services live in `src/Polymerium.Avalonia/App.axaml.cs`.
- The first navigation goes to `LandingPage`; shell-level state, notifications, OOBE, and update prompts are coordinated from `src/Polymerium.Avalonia/MainWindowContext.cs`.

## Directory Layout

Under `src/Polymerium.Avalonia/`, directories are organized by role. View + ViewModel pairs always live in sibling `Xxx/` + `XxxModels/` folders and are paired by naming convention (see ViewModel Mechanism):

- `Pages/` + `PageModels/` — full-screen pages and their view models (the navigated content).
- `Dialogs/` + `DialogModels/` — modal dialogs (centered, blocking).
- `Modals/` + `ModalModels/` — modal overlays (non-blocking, cover the host).
- `Sidebars/` + `SidebarModels/` — drawer sidebars (slide in from an edge).
- `Toasts/` — transient toast notifications. There is no `ToastsModels/` folder **yet**, so existing toasts are still constructed inline and passed to `OverlayService.PopToast(Toast)`. The mechanism is fully wired, though: `OverlayService.PopToast<TToast>(parameter)` is provided and routes through the same activator as other overlays — add `ToastsModels/` + a `ToastModel` the moment a toast needs a view model.
- `Components/` / `Controls/` / `Widgets/` — reusable Avalonia controls, grouped by scope (larger composite components vs. small atomic widgets).
- `Services/` — application services (navigation, overlay, data, persistence, instance management, etc.).
- `Repositories/` — data access / storage adapters.
- `Snapshots/` — snapshot/version store for instances.
- `Facilities/` — framework-level glue (base classes, activators, mixins, persistence helpers). `ViewModelBase`, `SimpleViewActivator`, `SimpleViewStatePersistence` live here.
- `Converters/` / `Rendering/` / `Themes/` — value converters, custom rendering helpers, and styling/ControlThemes.
- `Models/` — shared data models (DTOs/entities) consumed by views, view models, and services; these get their own files, not nested types.
- `Utilities/` — stateless helpers and extension methods.
- `Migrations/` — database migration definitions.
- `Assets/` / `Properties/` / `Exceptions/` — static assets, `.resx` localization, and domain exception types.

App-level files at the project root: `Program.cs` (entry), `Startup.cs` (DI), `App.axaml(.cs)` (window/lifetime), `MainWindow.axaml(.cs)` + `MainWindowContext.cs` (shell), `Configuration.cs`, `ErrorReporter.cs`, `AppBuilderExtensions.cs`.

## ViewModel Mechanism

Built on **CommunityToolkit.Mvvm** (source-generator-based) + the **Huskui.Avalonia** activation system, with **DynamicData** for reactive collections. There is **no** ReactiveUI and **no** string-based routing.

- **Base class** — `ViewModelBase` (`Facilities/ViewModelBase.cs`) extends CommunityToolkit's `ObservableObject` and implements Huskui's `IViewModel`. It exposes **only** two lifecycle hooks, `OnInitializeAsync(CancellationToken)` / `OnDeinitializeAsync()`, overridable as virtual methods. It does **not** provide `IsBusy`, global exception handling, or navigation awareness — each page/overlay implements those itself when needed.
- **View ↔ ViewModel pairing** — by naming convention, resolved at runtime by `SimpleViewActivator` (`Facilities/SimpleViewActivator.cs`). A type `Pages.FooPage` is paired with `PageModels.FooPageModel`; `Dialogs.FooDialog` ↔ `DialogModels.FooDialogModel`; the same pattern applies to `Modals/`↔`ModalModels/` and `Sidebars/`↔`SidebarModels/`. **This convention is universal across pages and overlays.**
- **DataContext is set by the activator**, not by code-behind and not by a ViewLocator. View `.axaml` files use `x:DataType="..."` purely for compile-time binding checks; view code-behind is minimal (constructor + `InitializeComponent()`).
- **Two creation entry points, same activator** — and five view-model kinds share it: `Page`/`PageModel`, `Dialog`/`DialogModel`, `Modal`/`ModalModel`, `Sidebar`/`SidebarModel`, and `Toast`/`ToastModel`. The pairing convention and the `IViewActivator`-based DataContext wiring are identical across all five.
  - Navigated content (pages) → `NavigationService.Navigate<TPage>(parameter)` (`Services/NavigationService.cs`), hosted by the `<husk:Frame>` in `MainWindow.axaml`. Type-safe, parameterized.
  - Overlays (dialogs/modals/sidebars/toasts) → `OverlayService` (`Services/OverlayService.cs`) calls the **same** `IViewActivator.Activate(typeof(T), parameter)`; the host is the overlay host instead of the frame. e.g. `overlayService.PopModal<TModal>(param)`, `.PopSidebar<TSidebar>(param)`, `.CreateDialog<TDialog>(param)` / `.PopDialogAsync(dialog)`, `.PopToast<TToast>(param)`. There is also a pass-through `PopToast(Toast)` for the common case where a toast is assembled inline with no view model.
- **Dependency injection & parameters** — PageModels/OverlayModels are **not** pre-registered in the container; the activator constructs them via `IServiceProvider` on demand and injects services through the constructor. Navigation/overlay parameters are delivered by injecting `IViewContext` or `IViewContext<T>` (where `T` is the parameter type). Registration is one line in `Startup.cs`: `AddViewModelActivation<SimpleViewActivator>()`.
- **Lifecycle** — override `OnInitializeAsync` / `OnDeinitializeAsync`. Initialize runs when the view enters the visual tree; Deinitialize runs when it leaves. Typical pattern: subscribe to events/observables in `OnInitializeAsync`, unsubscribe in `OnDeinitializeAsync` (see `LandingPageModel.cs`).
- **Commands & properties** — use CommunityToolkit source generators: `[RelayCommand]` (generates a `XxxCommand`, supports async and `CanExecute`) and `[ObservableProperty]`. Observable collections use `ObservableCollection<T>`; advanced reactive pipelines use DynamicData (`SourceCache<T, K>` + `.Connect().Filter().SortAndBind(...)`), e.g. `MainWindowContext.cs`.
- **Optional state persistence** — implement `IStatefulViewModel<TState>`; `SimpleViewStatePersistence` persists it through `PersistenceService`.
- **Shell exception** — `MainWindowContext` is **not** a `ViewModelBase`/PageModel; it only extends `ObservableObject` and is instantiated explicitly as `MainWindow.DataContext` in `App.axaml.cs`.

Rule of thumb: to add a new screen, create `FooPage.axaml` + `FooPageModel.cs` (or the `Foo*` overlay equivalents) in the right pair of folders, follow the naming, and navigate/activate it — no manual DataContext wiring or container registration is needed.

## Persistence And Runtime Paths

- User settings are not stored in `appsettings.json`; they are persisted by `ConfigurationService` to `PathDef.Default.PrivateDirectory(Program.Brand)/settings.json`.
- FreeSql uses `PathDef.Default.PrivateDirectory(Program.Brand)/persistence.sqlite.db` and `UseAutoSyncStructure(true)`, so schema changes can mutate the local DB on startup.
- HTTP cache lives at `PathDef.Default.PrivateDirectory(Program.Brand)/cache.sqlite.db`.
- `~/.trident.home` can override `PathDef.Default`; check that file before assuming where app data is written.

## Platform And Packaging Gotchas

- Windows symlink capability matters. `OobePrivilege` explicitly tests symbolic-link creation in the app data directory, so Windows Developer Mode is a real prerequisite for local workflows that depend on instance deployment.
- Release CI is tag-driven (`v*`) and publishes self-contained builds for `win-x64`, `linux-x64`, and `osx-arm64`.
- Local publish order matters: `Publish-Folder.ps1` creates `Publish/<rid>` first, then `Publish-Velopack.ps1` packs from that directory.
- The release workflow contains a case-fix for published localization output: `zh-hans` must become `zh-Hans`. Preserve that quirk if you touch packaging or localization.
- `scripts/Workflow_Update-Changelog.ps1` rewrites `CHANGELOG.md`, `RELEASE_CHANGELOG.md`, and `changelogs/rolling.md`, and archives into `changelogs/v<major>.<minor>.md`.

## Release Flow

Releases are **tag-driven**: pushing a `v*` tag triggers `.github/workflows/publish.yml`, which builds self-contained `win-x64` / `linux-x64` / `osx-arm64` artifacts, runs `Workflow_Update-Changelog.ps1` **inside CI** (so never run that script locally right before a release — CI will archive the rolling section itself), and pushes the changelog commit back to `main`. The workflow ends by creating a **draft** GitHub Release; a human must click *Publish release* to flip it to published, which in turn fires `mirrorchyan_release.yml` to upload to Mirror酱.

The human-side sequence is therefore:

1. `git push origin main` — land the code (PR merge, etc.).
2. `git tag vX.Y.Z && git push origin vX.Y.Z` — trigger the build. Version number is whatever the tag says (`GitVersion.yml` runs with `increment: None`).
3. On GitHub, review the draft Release and click *Publish release*.

Version-numbering convention: **`minor` increments mark milestones, not individual features** — until a milestone lands, ship under a `patch` bump (e.g. `v1.10.3`, not `v1.11.0`).

## Localization

- Localized strings live in `src/Polymerium.Avalonia/Properties/Resources.resx` (English, the source) and `Resources.zh-hans.resx` (Chinese). Both files use the same set of resource keys.
- `Resources.Designer.cs` holds the `public static string` accessors that XAML references as `{x:Static lang:Resources.KeyName}`. It mirrors the keys in `Resources.resx` one-to-one.
- Every change to a localized string — add, rename, edit a value, or delete — must be applied to all three files: `Resources.resx`, `Resources.zh-hans.resx`, and `Resources.Designer.cs`. Apply it to the two `.resx` files first (same key, localized value each), then to `Designer.cs`. Do not expect the IDE or any codegen to sync them; the agent does it by hand each time. Renaming or deleting a key means updating every `{x:Static}` reference too.

## Expected Build Noise

- `dotnet build "Polymerium.slnx"` currently emits Avalonia Accelerate Community telemetry notices and a warning in `submodules/Trident.Net`; those are existing build outputs, not necessarily regressions from your change.
- IDE or lsp will lock the .dll files and cause the build process failed, treat it as a success if there is no more errors.

## Code Organization

- **One type per `.cs` file.** Never declare more than one top-level type in a single file. A new type has only two valid homes: its own file, or nested inside the type it belongs to.
- **Choose by semantic ownership, not by visibility or who references it.** The question is whether the type is that other type's own concept — not whether it is public or used elsewhere.
  - **Nested type (类中类)** when it is dedicated to an outer class, even if that class exposes it through its public API (e.g. as a parameter or return type). The fact that callers must supply/pass values of that type does **not** make it independent. Example: `SkinView` nests inside `AccountHelper` because it exists only to describe `AccountHelper`'s body-render URLs.
  - **Own file** when it is a shared model — a type with its own data/properties that View, ViewModel, and Services may all consume is a standalone entity and gets its own file (under `Models/` for models). Example: `SkinFrame` is a model the view binds to and view models build, so it lives in `Models/SkinFrame.cs`, not tucked inside the control.
- **静态工具类用 `Helper` 后缀、放 `Utilities`。** 无状态工具类是 `public static class XxxHelper`（不可实例化），归属 `<Project>.Utilities` 命名空间；扩展方法类另用 `XxxExtensions` 后缀、放 `Extensions/`，两者不混。

## Comments

**The default is no comment.** Names, types, and control flow are the documentation; a method that reads `Close(); Dispose();` needs no `// Close the dialog and release resources` above it. A comment earns its place only when the code is **counter-intuitive** — when a reader following the obvious reading would reach the wrong conclusion without a hint. Write to explain the *why*, never the *what*.

Two anti-patterns to avoid:

- **Restating the obvious.** Paraphrasing what the code already says in plain sight is noise. If the names and types already tell the story, the comment goes.
- **Repeating a project-wide mechanism at a single call site.** If a convention is already described in this AGENTS.md (e.g. the activator-driven ViewModel lifecycle) or is the default behavior shared by every sibling method (e.g. every `Pop*`/`Navigate*` goes through the same activator), do **not** single out one site to re-explain it. Doing so implies the others differ when they don't, and misleads future readers. Comment the **exception**, never the rule.

**Emphasis comments.** When a comment does earn its place — a non-obvious constraint, gotcha, or warning the next reader will get wrong without the hint — promote it above ordinary commentary with a leading tag. This is one tier higher than a plain `//` comment: a tagged line signals "this matters, read me carefully."

The format is fixed regardless of tag: first line `// TAG: ` (two slashes, one space, the tag, one space); continuation lines `//  ` (two slashes, **two** spaces — one more than a normal comment — so the line visibly belongs to the tagged block). No variants such as `//NOTE:`, `// note:`, or `// NB:`.

Tags are `PascalCase` and pick the intent: `NOTE` for a non-obvious constraint or invariant the code relies on; `TODO` for known unfinished work; `HACK` for a deliberate workaround that should ideally not exist; `FIXME`/`BUG` for a known defect; `WARNING` for a footgun. Use the most specific tag that fits.

When in doubt, leave the comment out. The legitimate reasons to write one are narrow and non-local: why a workaround exists, a non-obvious constraint or invariant, a link to an upstream issue, or a warning about something the next reader will get wrong without the hint.

## Styling Conventions (Avalonia)

When writing or editing `.axaml` ControlThemes, follow the project's (and the upstream Huskui theme's) naming rules for selectors:

- **Style classes (variant selectors) are `PascalCase`.** A class denotes a **variant** — a named look the consumer opts into — and the name is an adjective or noun describing that variant. Examples in use: `Primary`, `Danger`, `Small`, `Status`, `Warning`, `Success`, applied as `Classes="Primary Small"`.
- **Pseudo-classes (state selectors) are `all-lowercase`.** A pseudo-class denotes a **runtime state** of the control and is almost always an adjective describing that state. Examples: `:pointerover`, `:pressed`, `:checked`, `:disabled`, `:focus`, `:selected`, `:error`. Never capitalize a pseudo-class.

The distinguishing question: **is the consumer choosing a look (`Primary`) or is the control reporting its own state (`:pressed`)?** Variant → PascalCase class; state → lowercase pseudo-class.

- **A variant class may only set the control's own exposed properties** (e.g. `Background`, `Foreground`, `BorderBrush`, `CornerRadius`, `Padding`). It must **not** reach into the control template and restyle named parts (`/template/ Border#PART_Xxx`). Restyling template internals is reserved for pseudo-class-driven states inside the same ControlTheme; variant classes stay at the public-property surface so they compose cleanly when consumers stack them (`Classes="Primary Small"`). For example, a `Primary` class on a Button changes `Background`/`Foreground`; it does **not** touch the inner `ContentPresenter` or `PART_Background` rectangle directly.
- Consumer-facing usage (`Classes="..."`) uses the variant names above; the corresponding selector definitions live in the Huskui theme and the per-control ControlThemes under `src/Polymerium.Avalonia/Controls/*.axaml`. When adding a new variant, define its `Style Selector="... .YourVariant"` setter on exposed properties only, and use it as `Classes="YourVariant"`.

## Template Part Naming

When a control's `ControlTheme` template contains named elements that are referenced from code-behind (via `OnApplyTemplate` / `NameScope.Find<T>`), follow the Huskui convention:

- **Code-behind referenced parts get the `PART_` prefix.** Declare them in code-behind exactly as Huskui does:
  ```csharp
  [TemplatePart(PART_ScrollViewer, typeof(ScrollViewer))]
  public const string PART_ScrollViewer = nameof(PART_ScrollViewer);
  ```
  Then use `e.NameScope.Find<ScrollViewer>(PART_ScrollViewer)` instead of string literals like `Find<ScrollViewer>("PART_ScrollViewer")`.
- **Template-internal elements that are NOT referenced from code-behind do NOT use the `PART_` prefix.** Give them descriptive, short names like `Background`, `Border`, `Indicator`, `ContentPresenter`, `GlowBorder` — these names serve only styling selectors within the same ControlTheme and never appear in C#.
- Every code-behind referenced part gets its own `[TemplatePart]` attribute + `public const string` declaration; do not skip the attribute or use bare strings.
- `nameof(PART_Xxx)` self-checks: if you rename the constant, the `nameof` string updates automatically, keeping the XAML name and the C# constant in sync.

## Pseudo-class Registration

**Pseudo-class names used in code-behind must be declared as `public const string` with a `CLASS_` prefix**, same principle as `PART_`. Never use bare pseudo-class string literals in `PseudoClasses.Set` / `PseudoClasses.Remove`:

```csharp
public const string CLASS_Error = ":error";
public const string CLASS_Selected = ":selected";
```

Then use `PseudoClasses.Set(CLASS_Error, true)` instead of `PseudoClasses.Set(":error", true)`. Pseudo-class selectors in `.axaml` are still written as bare `:error`/`:selected` in style selectors — the constant is only for code-behind references.

## External Tracking (Jira / GitHub / Sentry)

固定参数，调用 MCP 时直接复用，不要每次重新发现：

- **Atlassian site**: https://d3ara1n.atlassian.net
- **cloudId**: `88eb6a79-a7aa-49eb-8e71-5fffb7d4896b`
- **Jira project key**: `POLY`
- **Issue types**（`issueTypeName` 传中文名即可）: 故障=`10070`, 任务=`10001`, 长篇故事=`10002`, 子任务=`10003`
- **GitHub**: owner=`d3ara1n`, repo=`Polymerium`
- **Sentry**: organizationSlug=`gravitylab`, regionUrl=`https://us.sentry.io`, projectSlug=`polymerium`
  - Issue search uses `projectSlugOrId="polymerium"`.
  - Event search uses `projectSlug="polymerium"`.

双向链接约定（把 GitHub issue 转录到 Jira 时遵循）：

- Jira issue 描述内嵌 GitHub issue URL；
- GitHub issue 评论附 `[POLY-XX](https://d3ara1n.atlassian.net/browse/POLY-XX)` 指回 Jira；
- 修复 commit 首行以 `POLY-XX: type(scope): ...` 关联（见下文 Git Commit）。

NOTE: site URL / cloudId / project key 本身不私密——没有 API token 任何人都调不动 Jira API，真正敏感的 token 由 MCP 层保管，不写进本文件。Jira 站点默认私有，外部 GitHub 用户无权访问，进度需同步回 GitHub issue 评论才能被 reporter 看见。

## Git Commit

- **Do not commit on your own initiative.** Make all the edits you need, then stop and wait for the user to explicitly tell you to commit (e.g. "提交"). Never auto-commit after editing without being asked.
- 首行按 Conventional Commits 格式：`type(scope): description`
- 关联 Jira issue 时，issue key 放首行开头：`POLY-XX: type(scope): description`
- 关联 GitHub issue 时，issue key 放首行末尾括号内：`type(scope): description (#nnn)`
- body 写变更要点，与首行空一行隔开

@ROLLING.md