using System.Collections.Generic;
using Avalonia;
using FluentIcons.Common;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Components;

public partial class OobeFeatures : OobeStep
{
    public static readonly DirectProperty<OobeFeatures, IReadOnlyList<OobeFeatureModel>> FeaturesProperty =
        AvaloniaProperty.RegisterDirect<OobeFeatures, IReadOnlyList<OobeFeatureModel>>(nameof(Features),
            o => o.Features);

    public OobeFeatures()
    {
        InitializeComponent();
        Features = CreateFeatures();
    }

    public IReadOnlyList<OobeFeatureModel> Features
    {
        get;
        private set => SetAndRaise(FeaturesProperty, ref field, value);
    }

    private static IReadOnlyList<OobeFeatureModel> CreateFeatures() =>
    [
        new()
        {
            Icon = Symbol.BranchFork,
            Title = LanguageManager.Instance.OobeFeatures_GitIntegration_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_GitIntegration_Description.Current()
        },
        new()
        {
            Icon = Symbol.Document,
            Title = LanguageManager.Instance.OobeFeatures_PortableMetadata_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_PortableMetadata_Description.Current()
        },
        new()
        {
            Icon = Symbol.Globe,
            Title = LanguageManager.Instance.OobeFeatures_MultiRepository_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_MultiRepository_Description.Current()
        },
        new()
        {
            Icon = Symbol.FolderLink,
            Title = LanguageManager.Instance.OobeFeatures_SmartResource_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_SmartResource_Description.Current()
        },
        new()
        {
            Icon = Symbol.ShieldCheckmark,
            Title = LanguageManager.Instance.OobeFeatures_IntegrityCheck_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_IntegrityCheck_Description.Current()
        },
        new()
        {
            Icon = Symbol.Rocket,
            Title = LanguageManager.Instance.OobeFeatures_OneClick_Title.Current(),
            Description = LanguageManager.Instance.OobeFeatures_OneClick_Description.Current()
        }
    ];
}
