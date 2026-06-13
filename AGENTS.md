# AGENTS

## Use These Sources, Not The README Commands

- `README.md` is partially stale for development workflow. It mentions `Development.ps1`, `Production.ps1`, and `Publish.ps1`, but this repo only ships these root scripts:
- `pwsh scripts/Format-Files.ps1`
- `pwsh scripts/Publish-Folder.ps1 -Rid <rid>`
- `pwsh scripts/Publish-Velopack.ps1 -Version <semver> -Rid <rid>`
- `pwsh scripts/Workflow_Update-Changelog.ps1 -Version <semver>`

## Repo Shape

- This repo is a .NET 10 solution rooted at `Polymerium.slnx`.
- `src/Polymerium.Avalonia` is the only app in this repo.
- `submodules/Trident.Net` is a git submodule and is part of the solution build. Treat it as an integral part of this project: it participates in the same development workflow and should be edited freely alongside the main codebase. Do not treat submodule changes as out-of-scope — feel free to modify files under `submodules/Trident.Net` when the task requires it. `Huskui.Avalonia` is consumed as a NuGet package, not a submodule.
- Fresh clones need submodules initialized: `git submodule update --init --recursive`.

## Verified Commands

- Full solution build: `dotnet build "Polymerium.slnx"`
- Focused app build: `dotnet build "src/Polymerium.Avalonia/Polymerium.Avalonia.csproj"`
- There are no test projects in this repo right now. `dotnet test "Polymerium.slnx"` is not a meaningful verification step; use build plus targeted checks instead.
- **Do NOT run any formatting tools** (`scripts/Format-Files.ps1`, `csharpier`, `xstyler`, etc.). They operate across the entire repo including submodules and will produce unintended changes in unrelated files. Only the user may invoke formatting.

## Architecture Entry Points

- App bootstrap starts in `src/Polymerium.Avalonia/Program.cs`.
- DI wiring lives in `src/Polymerium.Avalonia/Startup.cs`.
- Window construction, global exception hooks, and startup of lifetime services live in `src/Polymerium.Avalonia/App.axaml.cs`.
- The first navigation goes to `LandingPage`; shell-level state, notifications, OOBE, and update prompts are coordinated from `src/Polymerium.Avalonia/MainWindowContext.cs`.

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

## Localization

- Localized strings live in `src/Polymerium.Avalonia/Properties/Resources.resx` and `Resources.zh-hans.resx`.
- `Resources.Designer.cs` is generated from the `.resx` files; edit the `.resx` and `Resource.Designer.cs` to pass the build process.
- If encountered build failure due to missing localized string references, tell the user to generate `Rsources.Designer.cs` in the IDE to fix that or fix it by yourself.

## Expected Build Noise

- `dotnet build "Polymerium.slnx"` currently emits Avalonia Accelerate Community telemetry notices and a warning in `submodules/Trident.Net`; those are existing build outputs, not necessarily regressions from your change.
- IDE or lsp will lock the .dll files and cause the build process failed, treat it as a success if there is no more errors.

@ROLLING.md