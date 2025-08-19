# Product Overview

Polymerium is a Minecraft instance manager that takes a fundamentally different approach from traditional launchers. Instead of managing game files directly, it manages instance metadata and uses a deployment engine to restore local game files to match the metadata description.

## Core Philosophy

- **Metadata-driven instances**: Game experiences are described by portable metadata rather than specific file collections
- **No version isolation**: Uses abstract "game experience" concepts with concrete "instances" 
- **Deployment-based**: Files are deployed from metadata rather than maintained directly
- **Resource pooling**: Shared resources use symlinks to save disk space
- **Integrity checking**: Comprehensive file validation during deployment

## Key Features

- Custom UI with rich visual effects using Avalonia
- Incremental deployment with symlink-based file sharing
- Multi-account support with instance-linked accounts
- Integration with CurseForge and Modrinth repositories
- Manual Java configuration with intelligent runtime version selection
- Layered attachment management for instance metadata
- Modpack publishing with automatic changelog generation
- Clean uninstall (no filesystem pollution outside instance folders)

## Target Platforms

- Windows 10+ (primary)
- Linux (work in progress)
- macOS (planned)

## Special Requirements

- Windows Developer Mode must be enabled for symlink creation
- No telemetry or data collection
- Privacy-conscious design with minimal data exposure