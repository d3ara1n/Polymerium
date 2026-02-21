using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Modals;

public partial class ProfileRuleSelectorModal : Modal
{
    public static readonly DirectProperty<ProfileRuleSelectorModal, ProfileRuleSelectorModel> SelectorProperty =
        AvaloniaProperty.RegisterDirect<ProfileRuleSelectorModal, ProfileRuleSelectorModel>(nameof(Selector),
                                                                            o => o.Selector,
                                                                            (o, v) => o.Selector = v);

    public required ProfileRuleSelectorModel Selector
    {
        get;
        set => SetAndRaise(SelectorProperty, ref field, value);
    }

    public ProfileRuleSelectorModal() => InitializeComponent();

    public required OverlayService OverlayService { get; init; }
    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }

    public IReadOnlyList<Profile.Rice.Rule.RuleSelector.SelectorType> SelectorTypes { get; } =
        Enum.GetValues<Profile.Rice.Rule.RuleSelector.SelectorType>();

    public IReadOnlyList<ResourceKind> Kinds { get; } = Enum.GetValues<ResourceKind>();

    #region Commands

    [RelayCommand]
    private void EditSelectors()
    {
        if (Selector.Children is not null)
        {
            OverlayService.PopModal(new ProfileRuleSelectorsModal
            {
                Selectors = Selector.Children,
                Packages = Packages,
                OverlayService = OverlayService
            });
        }
    }

    [RelayCommand]
    private async Task PickTagAsync()
    {
        var dialog = new TagPickerDialog() { ExistingTags = Packages.SelectMany(x => x.Tags).Distinct().ToList() };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is string tag)
        {
            Selector.Tag = tag;
        }
    }

    [RelayCommand]
    private async Task PickPackageAsync()
    {
        var dialog = new PackagePickerDialog();
        dialog.SetItems(Packages);
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is InstancePackageModel package)
        {
            Selector.Purl = package.Entry.Purl;
        }
    }

    #endregion
}
