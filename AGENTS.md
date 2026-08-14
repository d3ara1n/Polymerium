# AGENTS

## Repo Shape

- This repo is a .NET 10 solution rooted at `Polymerium.slnx`.
- `src/Polymerium.Avalonia` is the only app in this repo.
- `submodules/Trident.Net` is a git submodule and is part of the solution build. Treat it as an integral part of this project: it participates in the same development workflow and should be edited freely alongside the main codebase. Do not treat submodule changes as out-of-scope — feel free to modify files under `submodules/Trident.Net` when the task requires it. `Huskui.Avalonia` is consumed as a NuGet package, not a submodule.
- Fresh clones need submodules initialized: `git submodule update --init --recursive`.
- `plans/` holds task plans — intent-only starting prompts, not maintained design docs. Read `plans/README.md` before writing one. Treat `plans/archived/` as a graveyard (no reference value, do not read).
- `GLOSSARY.md` defines canonical user-facing Polymerium terminology. Follow it when writing app strings, docs, changelog entries, issue text, or support messages.

@GLOSSARY.md

## Trident And Polymerium

This repo bundles two layers in one solution; knowing which is the core shapes every decision.

- **Trident** (`submodules/Trident.Net`) is the **core engine** — the .NET implementation of a declarative Minecraft instance toolchain. It owns every piece of real logic: the data model (`profile.json`, the `build/` + `import/` + `persist/` layering, `pref://` package references), the deploy and run engine, package repositories (Modrinth, CurseForge), account authentication (Microsoft / Xbox Live / Mojang / offline), and modpack import/export (Trident, Modrinth, CurseForge, MultiMC, Packwiz). Core exposes this capability through five managers — `ProfileManager`, `InstanceManager`, `RepositoryAgent`, `ImporterAgent`, `ExporterAgent` — plus DI extensions such as `AddPrismLauncher` / `AddMicrosoft` / `AddMinecraft`. The same core also powers the standalone `trident` CLI and an MCP server (`--mcp`), so Trident is what both the CLI and Polymerium sit on. For the authoritative model and integration guide, read `submodules/Trident.Net/README.md` and `submodules/Trident.Net/AGENTS.md` before working inside the submodule.
- **Polymerium** (`src/Polymerium.Avalonia`) is the **desktop shell** over Core — a peer to the `trident` CLI, not a re-implementation of Trident's logic. `Polymerium.Avalonia.csproj` references only `TridentCore.Core`, and `Startup.cs` re-registers the exact same Trident services and managers the CLI registers. Because Polymerium and the CLI wrap the same Core — same `profile.json`, same `.trident` data layout, same managers — an instance created or managed by one is directly readable and operable by the other; Polymerium is not a competing instance format, it is the desktop presentation of a Trident instance. Polymerium does **not** re-implement instance management, deployment, repositories, accounts, or import/export — it drives Trident's managers and layers a stylized desktop experience on top: the MVVM page/dialog/modal/toast UI, Huskui theming, local persistence (FreeSql), self-update (Velopack), crash reporting (Sentry), and HTTP caching.

Rule of thumb: when a behavior spans both layers, the real logic almost certainly belongs in Trident, with Polymerium adapting to the new surface — not the other way around.

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
- **Lifecycle** — override `OnInitializeAsync` / `OnDeinitializeAsync`. Initialize runs when the view enters the visual tree; Deinitialize runs when it leaves. Typical pattern: subscribe to events/observables in `OnInitializeAsync`, unsubscribe in `OnDeinitializeAsync` (see `LandingPageModel.cs`). Cancellation follows the same discipline: only genuinely long-running work earns a lifecycle-owned `CancellationTokenSource` (with its cancel/dispose plumbing); local-IO work that finishes in milliseconds passes `CancellationToken.None` and gets no token field, no cancel guards, and no dispose dance.
- **Commands & properties** — use CommunityToolkit source generators: `[RelayCommand]` (generates a `XxxCommand`, supports async and `CanExecute`) and `[ObservableProperty]`. Observable collections use `ObservableCollection<T>`; advanced reactive pipelines use DynamicData (`SourceCache<T, K>` + `.Connect().Filter().SortAndBind(...)`), e.g. `MainWindowContext.cs`.
- **Aggregate state over item collections** — derive it with a DynamicData pipeline (`ToObservableChangeSet()` / `Connect()` + `AutoRefresh(x => x.Flag)`) that pushes into plain `[ObservableProperty]` properties, fanning out to dependents with `[NotifyPropertyChangedFor]` / `[NotifyCanExecuteChangedFor]` (see `PackageSelectorDialog.axaml.cs`). Do not hand-subscribe per-item `PropertyChanged` and recompute aggregates in getters or manual `RaiseXxxChanged()` helpers — that splits one truth across N manual notification paths that drift.
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

- Localization is powered by [Irihi.Lingua](https://github.com/irihitech/Irihi.Lingua). Strings live in `Properties/Resources.resx` (English, the source) and `Resources.zh-hans.resx` (Chinese), declared as `AdditionalFiles`; the Lingua source generator bakes them into `LanguageManager` at compile time. There is no `Designer.cs` and no runtime `ResourceManager`.
- The active language is set at startup (`Program.cs` → `LanguageManager.Instance.UpdateCulture`) and switched live from `SettingsPageModel.OnLanguageChanged` — no restart required.
- Add or edit a localized string by updating **all** `.resx` files (same key, localized value each) — English in `Resources.resx` is the source, every other language file mirrors it. Rebuild regenerates `LanguageManager`; no third file to sync.
- **Consumption patterns** — pick by where the string appears:
  - XAML UI text (the common case): `{Translate {x:Static app:LanguageManager+Keys.KeyName}}` — observable-backed, hot-switches with language.
  - Enum display: `{app:LocalizedEnum {Binding Xxx}}` — the converter resolves the value by convention `{EnumType.Name}_{Value}` (e.g. `ResourceKind_Mod`); just add the resx key, no converter or XAML change.
  - POCO/ViewModel holding a key string: `{app:LocalizedKey {Binding Key}}` — looks up by name, returns the literal as-is when no key matches (for brand names like `Fabric`).
  - C# one-shot values (toasts, notifications, progress dialogs): `LanguageManager.Instance.KeyName.Current()` — reads the value for the current language at call time.
- `{Translate}`, `{app:LocalizedEnum}`, and `{app:LocalizedKey}` bind to `AvaloniaProperty` only — never assign them to a POCO `string` property (use `LocalizedKey` or `.Current()` instead).
- The `EnumName_Value` key convention for enum localization is canonical; follow it when adding new enums.

## Expected Build Noise

- `dotnet build "Polymerium.slnx"` currently emits Avalonia Accelerate Community telemetry notices and a warning in `submodules/Trident.Net`; those are existing build outputs, not necessarily regressions from your change.
- IDE or lsp will lock the .dll files and cause the build process failed, treat it as a success if there is no more errors.

## Code Organization

- **One type per `.cs` file.** Never declare more than one top-level type in a single file. A new type has only two valid homes: its own file, or nested inside the type it belongs to.
- **Choose by semantic ownership, not by visibility or who references it.** The question is whether the type is that other type's own concept — not whether it is public or used elsewhere.
  - **Nested type** when it is dedicated to an outer class, even if that class exposes it through its public API (e.g. as a parameter or return type). The fact that callers must supply/pass values of that type does **not** make it independent. Example: `SkinView` nests inside `AccountHelper` because it exists only to describe `AccountHelper`'s body-render URLs.
  - **Own file** when it is a shared model — a type with its own data/properties that View, ViewModel, and Services may all consume is a standalone entity and gets its own file (under `Models/` for models). Example: `SkinFrame` is a model the view binds to and view models build, so it lives in `Models/SkinFrame.cs`, not tucked inside the control.
- **Stateless helper classes use the `Helper` suffix and live in `Utilities`.** A stateless helper is a `public static class XxxHelper` (never instantiated) under the `<Project>.Utilities` namespace; extension-method classes use the separate `XxxExtensions` suffix and live in `Extensions/`. Do not mix the two.

## Comments

**The default is no comment.** Names, types, and control flow are the documentation; a method that reads `Close(); Dispose();` needs no `// Close the dialog and release resources` above it. Write to explain the *why*, never the *what*.

Comments earn their place when they carry information the code cannot, in three forms:

- **Intent** — what a non-trivial block or method is for, or the trade-off behind an approach. A plain `//` comment; no tag needed.
- **Block signposts** — a short label above a section of a long method or file (e.g. `// Parse author`, `// Game info`) aids navigation. This is not the "restating the obvious" that per-line narration is.
- **Constraint / gotcha** — an invariant, ordering requirement, or library behavior the reader would otherwise get wrong. This is the one form that earns a leading tag (see Emphasis comments below): tagging is deliberately reserved for it so a tag signals "misreading this is costly". Tagging ordinary intent comments defeats that.

Two anti-patterns to avoid:

- **Restating the obvious.** Paraphrasing what the code already says in plain sight is noise — narrating each loop iteration, assignment, or branch. If the names and types already tell the story, the comment goes.
- **Repeating a project-wide mechanism at a single call site.** If a convention is already described in this AGENTS.md (e.g. the activator-driven ViewModel lifecycle) or is the default behavior shared by every sibling method (e.g. every `Pop*`/`Navigate*` goes through the same activator), do **not** single out one site to re-explain it. Doing so implies the others differ when they don't, and misleads future readers. Comment the **exception**, never the rule.

**Emphasis comments.** When a comment earns a place as a constraint, gotcha, or warning the next reader will get wrong without the hint, promote it above ordinary commentary with a leading tag. This is one tier higher than a plain `//` comment: a tagged line signals "this matters, read me carefully."

The format is fixed regardless of tag: first line `// TAG: ` (two slashes, one space, the tag, one space); continuation lines `//  ` (two slashes, **two** spaces — one more than a normal comment — so the line visibly belongs to the tagged block). No variants such as `//NOTE:`, `// note:`, or `// NB:`.

Tags are `PascalCase` and pick the intent: `NOTE` for a non-obvious constraint or invariant the code relies on; `TODO` for known unfinished work; `HACK` for a deliberate workaround that should ideally not exist; `FIXME`/`BUG` for a known defect; `WARNING` for a footgun. Use the most specific tag that fits.

When in doubt, leave the comment out — a stale or meaningless comment is debt, not documentation.

## Styling Conventions (Avalonia)

When writing or editing `.axaml` ControlThemes, follow the project's (and the upstream Huskui theme's) naming rules for selectors:

- **Style classes (variant selectors) are `PascalCase`.** A class denotes a **variant** — a named look the consumer opts into — and the name is an adjective or noun describing that variant. Examples in use: `Primary`, `Danger`, `Small`, `Status`, `Warning`, `Success`, applied as `Classes="Primary Small"`.
- **Pseudo-classes (state selectors) are `all-lowercase`.** A pseudo-class denotes a **runtime state** of the control and is almost always an adjective describing that state. Examples: `:pointerover`, `:pressed`, `:checked`, `:disabled`, `:focus`, `:selected`, `:error`. Never capitalize a pseudo-class.

The distinguishing question: **is the consumer choosing a look (`Primary`) or is the control reporting its own state (`:pressed`)?** Variant → PascalCase class; state → lowercase pseudo-class.

- **A variant class may only set the control's own exposed properties** (e.g. `Background`, `Foreground`, `BorderBrush`, `CornerRadius`, `Padding`). It must **not** reach into the control template and restyle named parts (`/template/ Border#PART_Xxx`). Restyling template internals is reserved for pseudo-class-driven states inside the same ControlTheme; variant classes stay at the public-property surface so they compose cleanly when consumers stack them (`Classes="Primary Small"`). For example, a `Primary` class on a Button changes `Background`/`Foreground`; it does **not** touch the inner `ContentPresenter` or `PART_Background` rectangle directly.
- Consumer-facing usage (`Classes="..."`) uses the variant names above; the corresponding selector definitions live in the Huskui theme and the per-control ControlThemes under `src/Polymerium.Avalonia/Controls/*.axaml`. When adding a new variant, define its `Style Selector="... .YourVariant"` setter on exposed properties only, and use it as `Classes="YourVariant"`.

## Layout Spacing
Inter-item gaps belong to the container, not the children. Reach for the panel's own spacing property — `StackPanel.Spacing`, `DockPanel.VerticalSpacing` / `HorizontalSpacing`, `ColumnSpacing` / `RowSpacing` on `Grid` — before touching `Margin` on the items; for an `ItemsControl`, set an `ItemsPanel` with `Spacing` instead of per-item margins. Per-child margins scatter one gap value across N items, double up at edges, and hide the rhythm the layout is trying to keep. `Margin` is reserved for what containers cannot express (e.g. `UniformGrid` has no spacing property) and for genuine outer insets, never for "put space between siblings".

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

## View State Representation

Pick the tier by what the state *is*:

- **Independent visibility of simple chrome → `IsVisible`.** A badge, a hint row, an element that shows or hides on its own. **Never fan one logical state out across multiple `IsVisible` bindings** — when a UI region swaps between alternatives, do not model it as several boolean properties each gating a separate control. That splits a single decision into N properties and N controls that can drift out of sync, and hides which alternative is actually active.
- **A fixed set of alternatives → one discriminant, switched.** When the alternatives are known up front (e.g. a preview pane that differs for a local import vs an online source), represent the alternative as one value: data side an `enum`, or a base type with one derived class per case; view side `SwitchContainer` for an enum/bool discriminant, or `DataTemplate` selection by type for derived classes. One source of truth on the data side, one switching mechanism on the view side.
- **A produced result → the result object's own nullability.** When a region first collects input (a form) and then shows what the operation produced (scan results, a computed report), the discriminant is the result object itself — do not invent a `Step` enum or a `HasResult` flag alongside it. Data side: one nullable `TResult?` property. View side: `husk:PlaceholderContainer` with `Source="{Binding Result}"`, the result view in `SourceTemplate`, and the input form in `Placeholder`. Producing assigns the object; discarding is `Result = null`, which returns the region to the form for another round. The result reference is the single source of truth for which side is shown.

## Model Purity

Models — the data items a view or view-model binds to (anything under `Models/`, or a per-item DTO bound in an `ItemsControl`/`DataTemplate`) — hold **atomic, raw values only**. A model property stores one piece of data; it never derives, decorates, coalesces, or re-shapes it.

Forbidden in a model:

- **Formatted/decorated values** — e.g. `LoaderDisplay => Format(Loader)`. The raw value stays; formatting is the view's job.
- **Derived booleans** — e.g. `IsCorrupt => CorruptReason is not null`, `HasLoader => Loader is not null`. These duplicate information the raw value already carries.
- **Coalesced/fallback values** — e.g. `Name => Name ?? Key`. Fallback is presentation policy.

All such transformation belongs in the XAML via its native mechanisms: `Converter` (value→display), `SwitchContainer`/`DataTemplate` (conditional rendering), `IsVisible` + a null/existence converter (presence). The model supplies the atom; the view composes the presentation.

Rationale: a model that decorates couples the data shape to one presentation — the same atom cannot render a second way without editing the model, and the derived value silently drifts from the raw one it wraps. Keeping models data-only makes them reusable and concentrates every presentation decision in exactly one layer.

This pairs with, but is distinct from, *View State Representation* above: that decides *which* property drives a region; this decides *what kind* of property a model may expose.

## Property Setters

A property setter assigns the value and raises the change — nothing else. Reactions to that change (derived state, cascades, side effects) live in the change callback, the single point that fires whether the value comes from two-way binding, code-behind, or initialization. Two carriers, one rule: custom controls (`AvaloniaObject` + `DirectProperty`/`StyledProperty`) override `OnPropertyChanged(change)` and dispatch on `change.Property`; ViewModels (`[ObservableProperty]`) implement `partial void OnXxxChanged(T value)`.

## External Tracking (Jira / GitHub / Sentry)

Fixed parameters — reuse these directly when calling MCP, do not rediscover them each time:

- **Atlassian site**: https://d3ara1n.atlassian.net
- **cloudId**: `88eb6a79-a7aa-49eb-8e71-5fffb7d4896b`
- **Jira project key**: `POLY`
- **Issue types** (pass the Chinese name as `issueTypeName`): 故障 (Bug)=`10070`, 任务 (Task)=`10001`, 长篇故事 (Epic)=`10002`, 子任务 (Sub-task)=`10003`
- **GitHub**: owner=`d3ara1n`, repo=`Polymerium`
- **Sentry**: organizationSlug=`gravitylab`, regionUrl=`https://us.sentry.io`, projectSlug=`polymerium`
  - Issue search uses `projectSlugOrId="polymerium"`.
  - Event search uses `projectSlug="polymerium"`.

Bidirectional linking convention (follow when transcribing a GitHub issue to Jira):

- Embed the GitHub issue URL in the Jira issue description.
- Add a `[POLY-XX](https://d3ara1n.atlassian.net/browse/POLY-XX)` comment on the GitHub issue linking back to Jira.
- Prefix the fix commit's first line with `POLY-XX: type(scope): ...` (see Git Commit below).

NOTE: the site URL / cloudId / project key are not secret on their own — without an API token nobody can call the Jira API, and the actually-sensitive token is held by the MCP layer, not written into this file. The Jira site is private by default; external GitHub users have no access, so progress must be synced back as a comment on the GitHub issue for the reporter to see.

## Git Commit

- **Do not commit on your own initiative.** Make all the edits you need, then stop and wait for the user to explicitly tell you to commit (e.g. "提交"). Never auto-commit after editing without being asked.
- First line follows Conventional Commits: `type(scope): description`.
- When linking a Jira issue, put the issue key at the start of the first line: `POLY-XX: type(scope): description`.
- When linking a GitHub issue, put the issue key in parentheses at the end of the first line: `type(scope): description (#nnn)`.
- Write the change summary in the body, separated from the first line by a blank line.

@ROLLING.md
