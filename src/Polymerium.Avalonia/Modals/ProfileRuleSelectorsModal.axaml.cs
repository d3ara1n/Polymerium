using System.Collections.Generic;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Modals;

public partial class ProfileRuleSelectorsModal : Modal
{
    public static readonly DirectProperty<
        ProfileRuleSelectorsModal,
        MappingCollection<Profile.Rice.Rule.RuleSelector, ProfileRuleSelectorModel>
    > SelectorsProperty = AvaloniaProperty.RegisterDirect<
        ProfileRuleSelectorsModal,
        MappingCollection<Profile.Rice.Rule.RuleSelector, ProfileRuleSelectorModel>
    >(nameof(Selectors), o => o.Selectors, (o, v) => o.Selectors = v);

    public ProfileRuleSelectorsModal() => InitializeComponent();

    public required MappingCollection<
        Profile.Rice.Rule.RuleSelector,
        ProfileRuleSelectorModel
    > Selectors
    {
        get;
        set => SetAndRaise(SelectorsProperty, ref field, value);
    }

    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }
    public required OverlayService OverlayService { get; init; }

    #region Commands

    [RelayCommand]
    private void AddSelector() =>
        Selectors.Add(new(new() { Type = Profile.Rice.Rule.RuleSelector.SelectorType.Pref }));

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
            OverlayService.PopModal(
                new ProfileRuleSelectorModal
                {
                    Selector = model,
                    Packages = Packages,
                    OverlayService = OverlayService,
                }
            );
        }
    }

    #endregion
}
