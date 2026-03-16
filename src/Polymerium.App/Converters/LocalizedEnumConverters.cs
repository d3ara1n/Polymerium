using Avalonia.Data.Converters;
using Huskui.Avalonia.Converters;
using Polymerium.App.Models;
using Polymerium.App.Properties;

namespace Polymerium.App.Converters;

public static class LocalizedEnumConverters
{
    public static IValueConverter LocalizedPackageBulkUpdatePreviewerTagPolicy { get; } = new RelayConverter((v, _) => v switch
    {
        PackageBulkUpdatePreviewerTagPolicy.Ignore => Resources.PackageBulkUpdatePreviewerTagPolicy_Ignore,
        PackageBulkUpdatePreviewerTagPolicy.Include => Resources.PackageBulkUpdatePreviewerTagPolicy_Include,
        PackageBulkUpdatePreviewerTagPolicy.Exclude => Resources.PackageBulkUpdatePreviewerTagPolicy_Exclude,
        _ => v
    });
}
