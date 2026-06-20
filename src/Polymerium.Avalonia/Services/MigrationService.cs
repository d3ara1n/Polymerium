using System.Collections.Generic;
using System.Linq;
using Polymerium.Avalonia.Migrations;

namespace Polymerium.Avalonia.Services;

public class MigrationService
{
    private readonly List<MigrationBase> _migrations = [];

    private readonly List<MigrationBase> _failedMigrations = [];

    private readonly List<MigrationBase> _completedMigrations = [];

    public void Add(MigrationBase migration)
    {
        _migrations.Add(migration);
    }

    public void EvaluateDirectories()
    {
        foreach (var migration in _migrations.OfType<MigrationBase.DirectoryMigration>())
        {

        }
    }

    public void EvaluateFiles()
    {
        foreach (var migration in _migrations.OfType<MigrationBase.FileMigration>())
        { }
    }

    public void EvaluateKeys()
    {
        foreach (var migration in _migrations.OfType<MigrationBase.ConfigKeyMigration>())
        { }
    }

    public void EvaluateMaps()
    {
        foreach (var migration in _migrations.OfType<MigrationBase.ConfigMapMigration>())
        { }
    }

    public void EvaluateValues()
    {
        foreach (var migration in _migrations.OfType<MigrationBase.ConfigCustomMigration>())
        { }
    }
}
