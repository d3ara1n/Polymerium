using System;

namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalProjectLogo
{
    public uint Id { get; set; }
    public uint ModId { get; set; }
    public string Title { get; set; }
    public Uri ThumbnailUrl { get; set; }
    public Uri Url { get; set; }
}