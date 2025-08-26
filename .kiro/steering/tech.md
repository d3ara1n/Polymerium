# Technology Stack

## Core Technologies

- **Framework**: .NET 9.0 with C# (preview language features enabled)
- **UI Framework**: Avalonia 11.3.4 (cross-platform XAML-based UI)
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Configuration**: Microsoft.Extensions.Configuration with JSON support
- **Logging**: Microsoft.Extensions.Logging with Console and Debug providers
- **HTTP**: Microsoft.Extensions.Http with Polly for resilience
- **Caching**: Microsoft.Extensions.Caching.Memory + NeoSmart.Caching.Sqlite

## Key Libraries

- **Database**: FreeSql with SQLite Core provider
- **HTTP Client**: Refit for type-safe REST API clients
- **JSON**: System.Text.Json (preview version)
- **Reactive**: DynamicData for reactive collections
- **UI Components**: 
  - Huskui.Avalonia (custom UI library via submodule)
  - FluentIcons.Avalonia for iconography
  - IconPacks.Avalonia.Lucide for additional icons
  - LiveChartsCore.SkiaSharpView.Avalonia for charts
  - Markdown.Avalonia for markdown rendering
- **Utilities**:
  - Semver for semantic versioning
  - Humanizer for human-readable text
  - CsvHelper for CSV processing
  - ReverseMarkdown for HTML to Markdown conversion
  - Mime-Detective for MIME type detection

## Build & Deployment

- **Build System**: MSBuild with .NET SDK
- **Versioning**: GitVersion for semantic versioning
- **Packaging**: Velopack for application deployment
- **Hot Reload**: HotAvalonia for development (Debug only)

## Development Tools

- **IDE**: JetBrains Rider (primary), Visual Studio (supported)
- **Code Style**: Comprehensive .editorconfig with ReSharper settings
- **Version Control**: Git with submodules for dependencies

## Common Commands

### Development
```powershell
# Build the solution
dotnet build

# Run in development mode
.\Development.ps1
# or manually:
dotnet run --project src/Polymerium.App/Polymerium.App.csproj --environment Development

# Run in production mode
.\Production.ps1
```

### Publishing
```powershell
# Full publish with versioning
.\Publish.ps1

# Manual publish
dotnet publish -c Release --self-contained -r win-x64 src/Polymerium.App/Polymerium.App.csproj
```

### Version Management
```powershell
# Get current version
dotnet gitversion

# Generate changelog (uses cliff.toml)
git cliff
```

## Code Style Guidelines

- **Language**: C# with preview features enabled
- **Nullable**: Enabled across all projects
- **Indentation**: 4 spaces, LF line endings
- **Naming**: 
  - Private fields: `_camelCase`
  - Constants: `ALL_UPPER`
  - Properties/Methods: `PascalCase`
- **Expression Bodies**: Preferred for simple members
- **Pattern Matching**: Encouraged where appropriate
- **Modern C# Features**: Use latest language features (records, pattern matching, etc.)

## Project Dependencies

- **Polymerium.App**: Main application (WinExe)
  - References: Trident.Core, Huskui.Avalonia
- **Trident.Core**: Core business logic library
  - References: Trident.Abstractions
- **Submodules**: 
  - Huskui.Avalonia: Custom UI components
  - Trident: Core abstractions and utilities