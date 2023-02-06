using System;
using System.Collections.Generic;

namespace Polymerium.Core.Models.Modrinth;

public struct ModrinthModpackFile
{
    public string Path { get; set; }
    public ModrinthModpackHashes Hashes { get; set; }
    public IEnumerable<Uri> Downloads { get; set; }
    public ModrinthModpackEnvs? Envs { get; set; }
    public uint FileSize { get; set; }
}