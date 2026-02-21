using System.Collections.Generic;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Modals;

public partial class ProfileRuleSelectorsModal : Modal
{
    public ProfileRuleSelectorsModal() => InitializeComponent();

    public static readonly DirectProperty<ProfileRuleSelectorsModal, MappingCollection<Profile.Rice.Rule.RuleSelector, ProfileRuleSelectorModel>>
        SelectorsProperty =
        AvaloniaProperty
           .RegisterDirect<ProfileRuleSelectorsModal, MappingCollection<Profile.Rice.Rule.RuleSelector, ProfileRuleSelectorModel>>(nameof(Selectors),
                o => o.Selectors,
                (o, v) => o.Selectors = v);

    public required MappingCollection<Profile.Rice.Rule.RuleSelector, ProfileRuleSelectorModel> Selectors
    {
        get;
        set => SetAndRaise(SelectorsProperty, ref field, value);
    }

    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }
    public required OverlayService OverlayService { get; init; }

    #region Commands

    [RelayCommand]
    private void AddSelector()
    {
        Selectors.Add(new(new() { Type = Profile.Rice.Rule.RuleSelector.SelectorType.Purl }));
    }

    [RelayCommand]
    private void RemoveSelector(ProfileRuleSelectorModel? model)
    {
        if (model != null)
        {
            Selectors.Remove(model);
        }
    }

    [RelayCommand]
    private void EditSelector(ProfileRuleSelectorModel? model)
    {
        if (model != null)
        {
            OverlayService.PopModal(new ProfileRuleSelectorModal
            {
                Selector = model,
                Packages = Packages,
                OverlayService = OverlayService
            });
        }
    }

    #endregion
}
