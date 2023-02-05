using System;
using System.Collections.Generic;

namespace Polymerium.Core.Models.BmclApi.Forge;

public struct ForgeBuild
{
    public IEnumerable<ForgeVersionFile> Files { get; set; }
    public string McVersion { get; set; }
    public DateTimeOffset Modified { get; set; }
    public string Version { get; set; }
}