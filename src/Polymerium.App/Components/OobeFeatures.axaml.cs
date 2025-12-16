using System.Collections.Generic;
using Avalonia;
using FluentIcons.Common;
using Polymerium.App.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Components;

public partial class OobeFeatures : OobeStep
{
    public static readonly DirectProperty<OobeFeatures, IReadOnlyList<OobeFeatureModel>> FeaturesProperty =
        AvaloniaProperty.RegisterDirect<OobeFeatures, IReadOnlyList<OobeFeatureModel>>(
            nameof(Features),
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
        new OobeFeatureModel
        {
            Icon = Symbol.BranchFork,
            Title = Properties.Resources.OobeFeatures_GitIntegration_Title,
            Description = Properties.Resources.OobeFeatures_GitIntegration_Description
        },
        new OobeFeatureModel
        {
            Icon = Symbol.Document,
            Title = Properties.Resources.OobeFeatures_PortableMetadata_Title,
            Description = Properties.Resources.OobeFeatures_PortableMetadata_Description
        },
        new OobeFeatureModel
        {
            Icon = Symbol.Globe,
            Title = Properties.Resources.OobeFeatures_MultiRepository_Title,
            Description = Properties.Resources.OobeFeatures_MultiRepository_Description
        },
        new OobeFeatureModel
        {
            Icon = Symbol.FolderLink,
            Title = Properties.Resources.OobeFeatures_SmartResource_Title,
            Description = Properties.Resources.OobeFeatures_SmartResource_Description
        },
        new OobeFeatureModel
        {
            Icon = Symbol.ShieldCheckmark,
            Title = Properties.Resources.OobeFeatures_IntegrityCheck_Title,
            Description = Properties.Resources.OobeFeatures_IntegrityCheck_Description
        },
        new OobeFeatureModel
        {
            Icon = Symbol.Rocket,
            Title = Properties.Resources.OobeFeatures_OneClick_Title,
            Description = Properties.Resources.OobeFeatures_OneClick_Description
        }
    ];
}
