using Avalonia.Data.Converters;
using Huskui.Avalonia.Converters;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Core.Igniters;

namespace Polymerium.App.Converters;

public static class LocalizedEnumConverters
{
    public static IValueConverter LocalizedPackageBulkUpdatePreviewerTagPolicy { get; } =
        new RelayConverter(
            (v, _) =>
                v switch
                {
                    PackageBulkUpdatePreviewerTagPolicy.IGNORE =>
                        Resources.PackageBulkUpdatePreviewerTagPolicy_Ignore,
                    PackageBulkUpdatePreviewerTagPolicy.INCLUDE =>
                        Resources.PackageBulkUpdatePreviewerTagPolicy_Include,
                    PackageBulkUpdatePreviewerTagPolicy.EXCLUDE =>
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
                        ResourceKind.UNKNOWN => Resources.ResourceKind_Unknown,
                        ResourceKind.MODPACK => Resources.ResourceKind_Modpack,
                        ResourceKind.MOD => Resources.ResourceKind_Mod,
                        ResourceKind.RESOURCE_PACK => Resources.ResourceKind_ResourcePack,
                        ResourceKind.SHADER_PACK => Resources.ResourceKind_ShaderPack,
                        ResourceKind.DATA_PACK => Resources.ResourceKind_DataPack,
                        ResourceKind.WORLD => Resources.ResourceKind_World,
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
                        LaunchMode.MANAGED => Resources.LaunchMode_Managed,
                        LaunchMode.FIRE_AND_FORGET => Resources.LaunchMode_FireAndForget,
                        LaunchMode.DEBUG => Resources.LaunchMode_Debug,
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
                        Profile.Rice.Rule.RuleSelector.SelectorType.AND =>
                            Resources.SelectorType_And,
                        Profile.Rice.Rule.RuleSelector.SelectorType.OR => Resources.SelectorType_Or,
                        Profile.Rice.Rule.RuleSelector.SelectorType.NOT =>
                            Resources.SelectorType_Not,
                        Profile.Rice.Rule.RuleSelector.SelectorType.PURL =>
                            Resources.SelectorType_Purl,
                        Profile.Rice.Rule.RuleSelector.SelectorType.REPOSITORY =>
                            Resources.SelectorType_Repository,
                        Profile.Rice.Rule.RuleSelector.SelectorType.TAG =>
                            Resources.SelectorType_Tag,
                        Profile.Rice.Rule.RuleSelector.SelectorType.KIND =>
                            Resources.SelectorType_Kind,
                        _ => type,
                    };
                }

                return v;
            }
        );
}
