using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Models.Eternal;

public struct EternalModLatestFile
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public uint ModId { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayName { get; set; }
    public string FileName { get; set; }
    public int ReleaseType { get; set; }
    public int FileStatus { get; set; }
    public IEnumerable<EternalFileHash> Hashes { get; set; }
    public DateTimeOffset FileDate { get; set; }
    public long FileLength { get; set; }
    public long DownloadCount { get; set; }
    public Uri DownloadUrl { get; set; }
    public IEnumerable<string> GameVersions { get; set; }
    public object SortableGameVersions { get; set; }
    public IEnumerable<EternalDependency> Dependencies { get; set; }
    public bool ExposeAsAlternative { get; set; }
    public uint ParentProjectFileId { get; set; }
    public uint AlternateFileId { get; set; }
    public bool IsServerPack { get; set; }
    public uint ServerPackFileId { get; set; }
    public long FileFingerprint { get; set; }
    public object Module { get; set; }

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
            if (Metadata.Layer.Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(v))
                loaderReq.Add(Metadata.Layer.Loader.MODLOADER_NAME_MAPPINGS[v]);
            else
                gameReq.Add(v);
        return new Requirement(gameReq, loaderReq);
    }
}