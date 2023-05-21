using System;
using System.Collections.Generic;

namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalProject
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public object Links { get; set; }
    public string Summary { get; set; }
    public int Status { get; set; }
    public uint DownloadCount { get; set; }
    public bool IsFeatured { get; set; }
    public uint PrimaryCategoryId { get; set; }
    public IEnumerable<EternalProjectCategory> Categories { get; set; }
    public uint ClassId { get; set; }
    public IEnumerable<EternalProjectAuthor> Authors { get; set; }
    public EternalProjectLogo? Logo { get; set; }
    public IEnumerable<EternalScreenshot> Screenshots { get; set; }
    public uint MainFileId { get; set; }
    public IEnumerable<EternalProjectLatestFile> LatestFiles { get; set; }
    public IEnumerable<EternalProjectLatestFileIndex> LatestFilesIndexes { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public DateTimeOffset DateModified { get; set; }
    public DateTimeOffset DateReleased { get; set; }
    public bool? AllowModDistribution { get; set; }
    public uint GamePopularityRank { get; set; }
    public bool IsAvailable { get; set; }
    public int ThumbsUpCount { get; set; }
}