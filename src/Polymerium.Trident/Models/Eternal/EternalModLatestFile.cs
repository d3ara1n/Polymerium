using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Models.Eternal;

public struct EternalModLatestFile
{
    public uint Id { get; init; }
    public uint GameId { get; init; }
    public uint ModId { get; init; }
    public bool IsAvailable { get; init; }
    public string DisplayName { get; init; }
    public string FileName { get; init; }
    public int ReleaseType { get; init; }
    public int FileStatus { get; init; }
    public IEnumerable<EternalFileHash> Hashes { get; init; }
    public DateTimeOffset FileDate { get; init; }
    public long FileLength { get; init; }
    public long DownloadCount { get; init; }
    public Uri DownloadUrl { get; init; }
    public IEnumerable<string> GameVersions { get; init; }
    public object SortableGameVersions { get; init; }
    public IEnumerable<EternalDependency> Dependencies { get; init; }
    public bool ExposeAsAlternative { get; init; }
    public uint ParentProjectFileId { get; init; }
    public uint AlternateFileId { get; init; }
    public bool IsServerPack { get; init; }
    public uint ServerPackFileId { get; init; }
    public long FileFingerprint { get; init; }
    public object Module { get; init; }

    public Uri ExtractDownloadUrl()
    {
        return DownloadUrl
               ?? new Uri($"https://mediafilez.forgecdn.net/files/{Id / 1000}/{Id % 1000}/{FileName}");
    }

    public string? ExtractSha1()
    {
        return Hashes.Any(x => x.Algo == 1) ? Hashes.First(x => x.Algo == 1).Value : null;
    }

    public ReleaseType ExtractReleaseType()
    {
        return ReleaseType switch
        {
            1 => global::Trident.Abstractions.Resources.ReleaseType.Release,
            2 => global::Trident.Abstractions.Resources.ReleaseType.Beta,
            3 => global::Trident.Abstractions.Resources.ReleaseType.Alpha,
            _ => global::Trident.Abstractions.Resources.ReleaseType.Release
        };
    }

    public Requirement ExtractRequirement()
    {
        var gameReq = new List<string>();
        var loaderReq = new List<string>();
        foreach (var v in GameVersions)
            if (Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(v))
                loaderReq.Add(Loader.MODLOADER_NAME_MAPPINGS[v]);
            else
                gameReq.Add(v);
        return new Requirement(gameReq, loaderReq);
    }
}