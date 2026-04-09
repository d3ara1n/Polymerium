using System.Collections.Generic;

namespace Polymerium.App.Models;

public class PackageBulkUpdatePreviewerModel
{
    public IList<string> Tags { get; set; } = [];
    public PackageBulkUpdatePreviewerTagPolicy TagPolicy { get; set; }
    public bool IsEnabledOnly { get; set; } = true;
}
