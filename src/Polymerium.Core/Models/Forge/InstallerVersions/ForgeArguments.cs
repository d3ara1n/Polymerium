using System.Collections.Generic;

namespace Polymerium.Core.Models.Forge.InstallerVersions;

public struct ForgeArguments
{
    public IEnumerable<string> Game { get; set; }
    public IEnumerable<string> Jvm { get; set; }
}