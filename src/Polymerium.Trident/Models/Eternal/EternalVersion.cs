using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Trident.Models.Eternal
{
    public struct EternalVersion
    {
        public uint Id { get; set; }
        public uint GameId { get; set; }
        public uint ModId { get; set; }
        public bool IsAvailable { get; set; }
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public uint ReleaseType { get; set; }
        public uint FileStatus { get; set; }
        public IEnumerable<EternalFileHash> Hashes { get; set; }
        public DateTimeOffset FileDate { get; set; }
        public ulong FileLength { get; set; }
        public ulong DownloadCount { get; set; }
        public ulong? FileSizeOnDisk { get; set; }
        public Uri? DownloadUrl { get; set; }
        public IEnumerable<string> GameVersions { get; set; }
        public object SortableGameVersions { get; set; }
        public IEnumerable<EternalDependency> Dependencies { get; set; }
        public bool? ExposeAsAlternative { get; set; }
        public uint? ParentProjectFileId { get; set; }
        public uint? AlternateFileId { get; set; }
        public bool IsServerPack { get; set; }
        public uint? ServerPackFileId { get; set; }
        public bool? IsEarlyAccessContent { get; set; }
        public DateTimeOffset? EarlyAccessEndDate { get; set; }
        public long FileFingerpring { get; set; }
        public object Modules { get; set; }

        public Uri ExtractDownloadUrl()
        {
            return DownloadUrl
                ?? new Uri($"https://mediafilez.forgecdn.net/files/{Id / 1000}/{Id % 1000}/{FileName}");
        }

        public string? ExtractSha1()
        {
            return Hashes.Any(x => x.Algo == 1) ? Hashes.First(x => x.Algo == 1).Value : null;
        }
    }
}
