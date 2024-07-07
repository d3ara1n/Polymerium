﻿using Polymerium.Trident.Helpers;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Models.Eternal;

public struct EternalModInfo
{
    public uint Id { get; init; }
    public uint GameId { get; init; }
    public uint ModId { get; init; }
    public bool IsAvailable { get; init; }
    public string DisplayName { get; init; }
    public string FileName { get; init; }
    public uint ReleaseType { get; init; }
    public uint FileStatus { get; init; }
    public EternalFileHash[] Hashes { get; init; }
    public DateTimeOffset FileDate { get; init; }
    public ulong FileLength { get; init; }
    public ulong DownloadCount { get; init; }
    public ulong? FileSizeOnDisk { get; init; }
    public Uri? DownloadUrl { get; set; }
    public string[] GameVersions { get; init; }
    public object SortableGameVersions { get; init; }
    public EternalDependency[] Dependencies { get; init; }
    public bool? ExposeAsAlternative { get; init; }
    public uint? ParentProjectFileId { get; init; }
    public uint? AlternateFileId { get; init; }
    public bool IsServerPack { get; init; }
    public uint? ServerPackFileId { get; init; }
    public bool? IsEarlyAccessContent { get; init; }
    public DateTimeOffset? EarlyAccessEndDate { get; init; }
    public long FileFingerprint { get; init; }
    public object Modules { get; init; }

    public Uri ExtractDownloadUrl() =>
        DownloadUrl
        ?? new Uri(
            $"https://mediafilez.forgecdn.net/files/{Id / 1000}/{Id % 1000}/{Uri.EscapeDataString(FileName)}");

    public string? ExtractSha1() => Hashes.Any(x => x.Algo == 1) ? Hashes.First(x => x.Algo == 1).Value : null;

    public ReleaseType ExtractReleaseType() =>
        ReleaseType switch
        {
            1 => global::Trident.Abstractions.Resources.ReleaseType.Release,
            2 => global::Trident.Abstractions.Resources.ReleaseType.Beta,
            3 => global::Trident.Abstractions.Resources.ReleaseType.Alpha,
            _ => global::Trident.Abstractions.Resources.ReleaseType.Release
        };

    public Requirement ExtractRequirement()
    {
        List<string> gameReq = new();
        List<string> loaderReq = new();
        foreach (var v in GameVersions)
        {
            if (CurseForgeHelper.MODLOADER_MAPPINGS.Keys.Contains(v))
            {
                loaderReq.Add(CurseForgeHelper.MODLOADER_MAPPINGS[v]);
            }
            else
            {
                gameReq.Add(v);
            }
        }

        return new Requirement(gameReq, loaderReq);
    }

    public IEnumerable<Dependency> ExtractDependencies() =>
        Dependencies.Where(x => x.RelationType == 3 || x.RelationType == 2).Select(x =>
            new Dependency(RepositoryLabels.CURSEFORGE, x.ModId.ToString(), null, x.RelationType == 3));
}