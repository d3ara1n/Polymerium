using Avalonia.Data.Converters;
using Huskui.Avalonia.Converters;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;
using Trident.Core.Igniters;

namespace Polymerium.App.Converters;

public static class LocalizedEnumConverters
{
    public static IValueConverter LocalizedPackageBulkUpdatePreviewerTagPolicy { get; } =
        new RelayConverter(
            (v, _) =>
                v switch
                {
                    PackageBulkUpdatePreviewerTagPolicy.Ignore =>
                        Resources.PackageBulkUpdatePreviewerTagPolicy_Ignore,
                    PackageBulkUpdatePreviewerTagPolicy.Include =>
                        Resources.PackageBulkUpdatePreviewerTagPolicy_Include,
                    PackageBulkUpdatePreviewerTagPolicy.Exclude =>
                        Resources.PackageBulkUpdatePreviewerTagPolicy_Exclude,
                    _ => v,
                }
        );

    public static IValueConverter LocalizedResourceKind { get; } =
        new RelayConverter(
            (v, _) =>
            {
                if (v is ResourceKind kind)
                {
                    return kind switch
                    {
                        ResourceKind.Modpack => Resources.ResourceKind_Modpack,
                        ResourceKind.Mod => Resources.ResourceKind_Mod,
                        ResourceKind.ResourcePack => Resources.ResourceKind_ResourcePack,
                        ResourceKind.ShaderPack => Resources.ResourceKind_ShaderPack,
                        ResourceKind.DataPack => Resources.ResourceKind_DataPack,
                        ResourceKind.World => Resources.ResourceKind_World,
                        _ => kind,
                    };
                }

                return v;
            }
        );

    public static IValueConverter LocalizedLaunchMode { get; } =
        new RelayConverter(
            (v, _) =>
            {
                if (v is LaunchMode mode)
                {
                    return mode switch
                    {
                        LaunchMode.Managed => Resources.LaunchMode_Managed,
                        LaunchMode.FireAndForget => Resources.LaunchMode_FireAndForget,
                        LaunchMode.Debug => Resources.LaunchMode_Debug,
                        _ => mode,
                    };
                }

                return v;
            }
        );

    public static IValueConverter LocalizedSelectorType { get; } =
        new RelayConverter(
            (v, _) =>
            {
                if (v is Profile.Rice.Rule.RuleSelector.SelectorType type)
                {
                    return type switch
                    {
                        Profile.Rice.Rule.RuleSelector.SelectorType.And =>
                            Resources.SelectorType_And,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Or => Resources.SelectorType_Or,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Not =>
                            Resources.SelectorType_Not,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Purl =>
                            Resources.SelectorType_Purl,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Repository =>
                            Resources.SelectorType_Repository,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Tag =>
                            Resources.SelectorType_Tag,
                        Profile.Rice.Rule.RuleSelector.SelectorType.Kind =>
                            Resources.SelectorType_Kind,
                        _ => type,
                    };
                }

                return v;
            }
        );
}
