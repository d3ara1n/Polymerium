using System.Collections.Generic;

namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalFeatured
{
    public IEnumerable<EternalProject> Featured { get; set; }
    public IEnumerable<EternalProject> Popular { get; set; }
    public IEnumerable<EternalProject> RecentlyUpdated { get; set; }
}