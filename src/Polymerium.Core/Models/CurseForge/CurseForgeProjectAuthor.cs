using System;

namespace Polymerium.Core.Models.CurseForge;

public struct CurseForgeProjectAuthor
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public Uri Url { get; set; }
}