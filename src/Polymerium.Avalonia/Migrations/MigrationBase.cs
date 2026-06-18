using System;
using System.Collections;
using System.Collections.Generic;

namespace Polymerium.Avalonia.Migrations;

public abstract class MigrationBase
{
    public sealed class FileMigration : MigrationBase
    {
        public required string OldPath { get; init; }
        public required string NewPath { get; init; }
    }

    public sealed class DirectoryMigration : MigrationBase
    {
        public required string OldPath { get; init; }
        public required string NewPath { get; init; }
    }

    public sealed class ConfigKeyMigration:MigrationBase
    {
        public required string OldKey { get; init; }
        public required string NewKey { get; init; }
    }

    public sealed class ConfigMapMigration:MigrationBase
    {
        public required string Key { get; init; }
        public required IDictionary<object?, object?> Map { get; init; }
    }

    public sealed class ConfigCustomMigration: MigrationBase
    {
        public required string Key { get; init; }
        public required Action<object?, object?> Map { get; init; }
    }
}
