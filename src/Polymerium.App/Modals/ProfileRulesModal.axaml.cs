using System.Collections.Generic;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Modals;

public partial class ProfileRulesModal : Modal
{
    public ProfileRulesModal() => InitializeComponent();

    public required MappingCollection<Profile.Rice.Rule, ProfileRuleModel> Rules { get; init; }
    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }
    public required OverlayService OverlayService { get; init; }
}
