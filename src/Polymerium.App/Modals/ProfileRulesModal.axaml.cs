using System.Collections.Generic;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Modals;

public partial class ProfileRulesModal : Modal
{
    public static readonly DirectProperty<ProfileRulesModal, MappingCollection<Profile.Rice.Rule, ProfileRuleModel>>
        RulesProperty =
            AvaloniaProperty
               .RegisterDirect<ProfileRulesModal, MappingCollection<Profile.Rice.Rule, ProfileRuleModel>>(nameof(Rules),
                    o => o.Rules,
                    (o, v) => o.Rules = v);

    public ProfileRulesModal() => InitializeComponent();

    public required MappingCollection<Profile.Rice.Rule, ProfileRuleModel> Rules
    {
        get;
        set => SetAndRaise(RulesProperty, ref field, value);
    }

    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }
    public required OverlayService OverlayService { get; init; }

    #region Commands

    [RelayCommand]
    private void AddRule() => Rules.Add(new(new() { Enabled = false, Selector = new() }));

    [RelayCommand]
    private void RemoveRule(ProfileRuleModel? model)
    {
        if (model != null)
        {
            Rules.Remove(model);
        }
    }

    [RelayCommand]
    private void EditRule(ProfileRuleModel? model)
    {
        if (model != null)
        {
            OverlayService.PopModal(new ProfileRuleModal
            {
                Rule = model, Packages = Packages, OverlayService = OverlayService
            });
        }
    }

    [RelayCommand]
    private void MoveRuleToTop(ProfileRuleModel? model)
    {
        if (model != null)
        {
            var index = Rules.IndexOf(model);
            if (index > 0)
            {
                Rules.RemoveAt(index);
                Rules.Insert(0, model);
            }
        }
    }

    [RelayCommand]
    private void MoveRuleUp(ProfileRuleModel? model)
    {
        if (model != null)
        {
            var index = Rules.IndexOf(model);
            if (index > 0)
            {
                Rules.RemoveAt(index);
                Rules.Insert(index - 1, model);
            }
        }
    }

    [RelayCommand]
    private void MoveRuleDown(ProfileRuleModel? model)
    {
        if (model != null)
        {
            var index = Rules.IndexOf(model);
            if (index >= 0 && index < Rules.Count - 1)
            {
                Rules.RemoveAt(index);
                Rules.Insert(index + 1, model);
            }
        }
    }

    #endregion
}
