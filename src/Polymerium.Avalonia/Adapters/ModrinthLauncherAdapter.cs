using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TridentCore.Abstractions.Adapters;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Adapters;

// Reads Modrinth App profile metadata from its SQLite store (app.db). Unlike the JSON-based launcher
// adapters in Trident core, this one needs SQLite — which only the desktop layer already depends on —
// so it lives here as a Polymerium augmentation over Trident's ILauncherAdapter contract.
public class ModrinthLauncherAdapter : ILauncherAdapter
{
    private const string INSTANCE_DB = "app.db";
    private const string PROFILES_FOLDER = "profiles";

    // One query, positional columns: path, name, game_version, mod_loader, mod_loader_version.
    private const string QUERY =
        "SELECT path, name, game_version, mod_loader, mod_loader_version FROM profiles";

    private static readonly string[] IDENTIFIABLE_SUBDIRS = ["mods", "resourcepacks", "shaderpacks"];

    // mod_loader is stored lowercase (vanilla/forge/fabric/quilt/neoforge); vanilla yields no loader.
    private static readonly Dictionary<string, string> LOADER_BY_NAME = new(StringComparer.OrdinalIgnoreCase)
    {
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        ["fabric"] = LoaderHelper.LOADERID_FABRIC,
        ["quilt"] = LoaderHelper.LOADERID_QUILT,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE
    };

    public IReadOnlyList<LauncherKind> SupportedKinds { get; } = [LauncherKind.ModrinthApp];

    public string? DefaultDataDirectory(LauncherKind kind)
    {
        if (kind != LauncherKind.ModrinthApp)
        {
            return null;
        }

        // Current builds store under ModrinthApp; pre-0.8.0 used the com.modrinth.theseus identifier.
        return LauncherDataDirHelper.LocateUnderConventional("ModrinthApp", "com.modrinth.theseus");
    }

    public async Task<IReadOnlyList<LauncherInstance>> ScanAsync(string rootDir, CancellationToken cancellationToken = default)
    {
        var dbPath = Path.Combine(rootDir, INSTANCE_DB);
        if (!File.Exists(dbPath))
        {
            return [];
        }

        var profilesDir = Path.Combine(rootDir, PROFILES_FOLDER);
        var results = new List<LauncherInstance>();

        // One connection, one query, scoped to this scan and disposed at the end of the method — no
        // handle held between scans. Read-only so a running Modrinth App's writes never conflict.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // WAL means readers never block; this only covers the rollback-journal edge so a brief write
        // lock is waited out instead of failing the scan at once.
        await using var timeout = connection.CreateCommand();
        timeout.CommandText = "PRAGMA busy_timeout=3000;";
        await timeout.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = QUERY;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.IsDBNull(0))
            {
                continue;
            }

            results.Add(BuildInstance(reader, profilesDir));
        }

        return results;
    }

    private static LauncherInstance BuildInstance(SqliteDataReader reader, string profilesDir)
    {
        var path = reader.GetString(0);
        var name = reader.IsDBNull(1) ? null : reader.GetString(1);
        var gameVersion = reader.IsDBNull(2) ? null : reader.GetString(2);
        var loaderName = reader.IsDBNull(3) ? null : reader.GetString(3);
        var loaderVersion = reader.IsDBNull(4) ? null : reader.GetString(4);

        // The profile folder IS the game directory — Modrinth App has no nested .minecraft layer.
        var profileDir = Path.Combine(profilesDir, path);

        return new()
        {
            Kind = LauncherKind.ModrinthApp,
            Key = path,
            HomeDirectory = profileDir,
            RuntimeDirectory = profileDir,
            Name = string.IsNullOrEmpty(name) ? path : name,
            MinecraftVersion = gameVersion,
            Loader = ResolveLoader(loaderName, loaderVersion),
            CorruptReason = string.IsNullOrEmpty(gameVersion) ? CorruptReason.MinecraftComponentMissing : null,
            IdentifiableSubdirs = [.. IDENTIFIABLE_SUBDIRS.Where(d => Directory.Exists(Path.Combine(profileDir, d)))]
        };
    }

    private static string? ResolveLoader(string? loaderName, string? loaderVersion)
    {
        if (string.IsNullOrEmpty(loaderName)
            || string.IsNullOrEmpty(loaderVersion)
            || !LOADER_BY_NAME.TryGetValue(loaderName, out var identity))
        {
            return null;
        }

        return LoaderHelper.ToLurl(identity, loaderVersion);
    }
}
