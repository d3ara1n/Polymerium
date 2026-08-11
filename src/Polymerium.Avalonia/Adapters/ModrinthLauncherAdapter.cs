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

// NOTE: 读 Modrinth App 的 SQLite 元数据（app.db）。核心侧适配器基于 JSON，
//  只有桌面层已依赖 SQLite，故此适配器作为 Polymerium 对 ILauncherAdapter 的增强放在这里。
public class ModrinthLauncherAdapter : ILauncherAdapter
{
    private const string INSTANCE_DB = "app.db";
    private const string PROFILES_FOLDER = "profiles";

    // NOTE: the Modrinth App splits profile metadata across two tables — instances holds path/name,
    //  instance_content_sets holds the game version and loader — joined by the applied content set.
    //  Positional columns: path, name, game_version, loader, loader_version.
    private const string QUERY = "SELECT i.path, i.name, c.game_version, c.loader, c.loader_version "
                               + "FROM instances i LEFT JOIN instance_content_sets c ON c.id = i.applied_content_set_id";

    private static readonly string[] IDENTIFIABLE_SUBDIRS = ["mods", "resourcepacks", "shaderpacks"];

    // NOTE: mod_loader 以小写存储（vanilla/forge/fabric/quilt/neoforge）；vanilla 无 loader。
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

        // NOTE: 现行版本存于 ModrinthApp 目录；pre-0.8.0 用 com.modrinth.theseus 标识。
        return LauncherDataDirHelper.LocateUnderConventional("ModrinthApp", "com.modrinth.theseus");
    }

    public async Task<IReadOnlyList<LauncherInstance>> ScanAsync(
        string rootDir,
        CancellationToken cancellationToken = default)
    {
        var dbPath = Path.Combine(rootDir, INSTANCE_DB);
        if (!File.Exists(dbPath))
        {
            return [];
        }

        var profilesDir = Path.Combine(rootDir, PROFILES_FOLDER);
        var results = new List<LauncherInstance>();

        // NOTE: 单连接单查询，方法结束即释放，两次扫描之间不持有句柄；只读打开，
        //  与运行中的 Modrinth App 写入不冲突。
        var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadOnly }
           .ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // NOTE: WAL 下读不阻塞；busy timeout 只兜 rollback-journal 边角，短暂写锁等待而非立即失败。
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

        // NOTE: profile 文件夹即游戏目录——Modrinth App 没有嵌套 .minecraft 层。
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
            IdentifiableSubdirs =
            [
                .. IDENTIFIABLE_SUBDIRS.Where(d => Directory.Exists(Path.Combine(profileDir, d)))
            ]
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
