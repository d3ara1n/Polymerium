# Project Structure

## Root Directory Layout

```
Polymerium/
├── src/                    # Source code
├── submodules/            # Git submodules for dependencies
├── docs/                  # Documentation
├── changelogs/           # Version changelogs
├── assets/               # Project assets (screenshots, etc.)
├── Releases/             # Build outputs and release artifacts
├── .kiro/                # Kiro IDE configuration
├── .github/              # GitHub workflows and templates
├── *.ps1                 # PowerShell build scripts
├── *.yml/*.toml          # Configuration files
└── Polymerium.slnx       # Solution file
```

## Source Code Organization (`src/`)

### Polymerium.App (Main Application)
```
Polymerium.App/
├── Assets/               # Application resources (icons, images)
├── Components/           # Reusable UI components
├── Controls/             # Custom Avalonia controls
├── Converters/           # Value converters for data binding
├── Dialogs/              # Modal dialogs
├── Exceptions/           # Application-specific exceptions
├── Facilities/           # Infrastructure services
├── Modals/               # Modal windows and popups
├── Models/               # Data models and DTOs
├── Properties/           # Assembly properties and resources
├── Services/             # Application services
├── Themes/               # UI themes and styling
├── Toasts/               # Toast notification components
├── Utilities/            # Helper classes and extensions
├── ViewModels/           # MVVM view models
├── Views/                # XAML views and pages
├── Widgets/              # Specialized UI widgets
├── App.axaml[.cs]        # Application entry point
├── MainWindow.axaml[.cs] # Main application window
├── Program.cs            # Application bootstrap
├── Startup.cs            # Dependency injection configuration
└── appsettings.json      # Application configuration
```

### Polymerium.Trident (Core Library)
```
Polymerium.Trident/
├── Accounts/             # Account management
├── Clients/              # API clients (CurseForge, Modrinth)
├── Engines/              # Deployment and build engines
├── Exceptions/           # Domain-specific exceptions
├── Extensions/           # Extension methods
├── Igniters/             # Game launch logic
├── Importers/            # Modpack import functionality
├── Models/               # Domain models
├── Repositories/         # Data access layer
├── Services/             # Business logic services
├── Utilities/            # Core utilities
└── InstanceState.cs      # Instance state management
```

## Architecture Patterns

### MVVM Structure
- **Views**: XAML files in `Views/` folder
- **ViewModels**: Corresponding ViewModels in `ViewModels/` folder
- **Models**: Data models in `Models/` folder
- **Services**: Business logic in `Services/` folder

### Dependency Injection
- Services registered in `ServiceCollectionExtensions.cs`
- Configuration in `Startup.cs`
- Use constructor injection throughout

### File Naming Conventions
- **Views**: `{Name}View.axaml` + `{Name}View.axaml.cs`
- **ViewModels**: `{Name}ViewModel.cs`
- **Services**: `I{Name}Service.cs` (interface) + `{Name}Service.cs` (implementation)
- **Models**: `{Name}Model.cs` or `{Name}.cs`
- **Exceptions**: `{Name}Exception.cs`

## Configuration Files

### Build Configuration
- `Polymerium.slnx`: Solution file with project references
- `GitVersion.yml`: Semantic versioning configuration
- `cliff.toml`: Changelog generation configuration
- `.editorconfig`: Code style and formatting rules

### Development Scripts
- `Development.ps1`: Run application in development mode
- `Production.ps1`: Run application in production mode  
- `Publish.ps1`: Build and package for release

## Submodules (`submodules/`)
- `Huskui.Avalonia/`: Custom UI component library
- `Trident/`: Core abstractions and utilities

## Documentation (`docs/`)
- `Features.md`: Feature comparison and unique selling points
- `TRIDENT_V2.md`: Technical architecture documentation
- `Localization.md`: Internationalization guidelines
- `BRANCH_PROTECTION.md`: Git workflow documentation

## Asset Management
- Application icons and images in `src/Polymerium.App/Assets/`
- Screenshots and marketing assets in `assets/screenshots/`
- Build outputs in `Releases/`

## Key Architectural Principles

1. **Separation of Concerns**: UI (App) separate from business logic (Trident)
2. **Dependency Inversion**: Use interfaces and dependency injection
3. **MVVM Pattern**: Clear separation between View, ViewModel, and Model
4. **Resource Sharing**: Submodules for shared components
5. **Configuration-Driven**: External configuration files for build and deployment
6. **Clean Architecture**: Domain logic isolated from infrastructure concerns

## Development Workflow

1. Main development in `src/Polymerium.App/` for UI features
2. Core logic and services in `src/Polymerium.Trident/`
3. Shared utilities in submodules
4. Use PowerShell scripts for common development tasks
5. Follow GitVersion for semantic versioning
6. Update changelogs in `changelogs/` directory