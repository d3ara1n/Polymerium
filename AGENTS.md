# AGENTS

## Use These Sources, Not The README Commands

- `README.md` is partially stale for development workflow. It mentions `Development.ps1`, `Production.ps1`, and `Publish.ps1`, but this repo only ships these root scripts:
- `pwsh scripts/Format-Files.ps1`
- `pwsh scripts/Publish-Folder.ps1 -Rid <rid>`
- `pwsh scripts/Publish-Velopack.ps1 -Version <semver> -Rid <rid>`
- `pwsh scripts/Workflow_Update-Changelog.ps1 -Version <semver>`

## Repo Shape

- This repo is a .NET 10 solution rooted at `Polymerium.slnx`.
- `src/Polymerium.App` is the only app in this repo.
- `submodules/Huskui.Avalonia` and `submodules/Trident.Net` are real git submodules and are part of the solution build. Treat changes there as nested-repo changes, not normal folders.
- Fresh clones need submodules initialized: `git submodule update --init --recursive`.

## Verified Commands

- Full solution build: `dotnet build "Polymerium.slnx"`
- Focused app build: `dotnet build "src/Polymerium.App/Polymerium.App.csproj"`
- There are no test projects in this repo right now. `dotnet test "Polymerium.slnx"` is not a meaningful verification step; use build plus targeted checks instead.
- Formatting is repo-specific: run `pwsh scripts/Format-Files.ps1`.
- `scripts/Format-Files.ps1` requires globally installed `csharpier` and `xstyler`; there is no root `.config/dotnet-tools.json` manifest to restore them automatically.

## Architecture Entry Points

- App bootstrap starts in `src/Polymerium.App/Program.cs`.
- DI wiring lives in `src/Polymerium.App/Startup.cs`.
- Window construction, global exception hooks, and startup of lifetime services live in `src/Polymerium.App/App.axaml.cs`.
- The first navigation goes to `LandingPage`; shell-level state, notifications, OOBE, and update prompts are coordinated from `src/Polymerium.App/MainWindowContext.cs`.

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

- Localized strings live in `src/Polymerium.App/Properties/Resources.resx` and `Resources.zh-hans.resx`.
- `Resources.Designer.cs` is generated from the `.resx` files; edit the `.resx`, not the designer file.
- If encountered build failure due to missing localized string references, tell the user to generate `Rsources.Designer.cs` in the IDE to fix that. Do not fix it by yourself.

## Expected Build Noise

- `dotnet build "Polymerium.slnx"` currently emits Avalonia Accelerate Community telemetry notices and a warning in `submodules/Trident.Net`; those are existing build outputs, not necessarily regressions from your change.
- IDE or lsp will lock the .dll files and cause the build process failed, treat it as a success if there is no more errors.