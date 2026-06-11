using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public class PackageBulkUpdatePreviewerModel
{
    public IList<string> Tags { get; set; } = [];
    public PackageBulkUpdatePreviewerTagPolicy TagPolicy { get; set; }
    public bool IsEnabledOnly { get; set; } = true;
}
