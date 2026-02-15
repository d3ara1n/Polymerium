using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Modals;

public partial class ProfileRuleModal : Modal
{
    public static readonly DirectProperty<ProfileRuleModal, SelectorTypeModel?> SelectedSelectorTypeProperty =
        AvaloniaProperty.RegisterDirect<ProfileRuleModal, SelectorTypeModel?>(nameof(SelectedSelectorType),
                                                                              o => o.SelectedSelectorType,
                                                                              (o, v) => o.SelectedSelectorType = v);

    public SelectorTypeModel? SelectedSelectorType
    {
        get;
        set => SetAndRaise(SelectedSelectorTypeProperty, ref field, value);
    }

    public static readonly DirectProperty<ProfileRuleModal, ResourceKindModel?> SelectedResourceKindProperty =
        AvaloniaProperty.RegisterDirect<ProfileRuleModal, ResourceKindModel?>(nameof(SelectedResourceKind),
                                                                              o => o.SelectedResourceKind,
                                                                              (o, v) => o.SelectedResourceKind = v);

    public ResourceKindModel? SelectedResourceKind
    {
        get;
        set => SetAndRaise(SelectedResourceKindProperty, ref field, value);
    }

    public ProfileRuleModal() => InitializeComponent();

    public required ProfileRuleModel Rule
    {
        get;
        init
        {
            field = value;
            SelectedSelectorType = SelectorTypes.FirstOrDefault(x => x.Value == value.Selector);
        }
    }

    public required OverlayService OverlayService { get; init; }
    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }

    public IReadOnlyList<SelectorTypeModel> SelectorTypes { get; } =
    [
        new() { Value = Profile.Rice.Rule.SelectorType.And, Display = Properties.Resources.SelectorType_And },
        new() { Value = Profile.Rice.Rule.SelectorType.Or, Display = Properties.Resources.SelectorType_Or },
        new() { Value = Profile.Rice.Rule.SelectorType.Not, Display = Properties.Resources.SelectorType_Not },
        new() { Value = Profile.Rice.Rule.SelectorType.Purl, Display = Properties.Resources.SelectorType_Purl },
        new()
        {
            Value = Profile.Rice.Rule.SelectorType.Repository,
            Display = Properties.Resources.SelectorType_Repository
        },
        new() { Value = Profile.Rice.Rule.SelectorType.Tag, Display = Properties.Resources.SelectorType_Tag },
        new() { Value = Profile.Rice.Rule.SelectorType.Kind, Display = Properties.Resources.SelectorType_Kind }
    ];

    public IReadOnlyList<ResourceKindModel> Kinds { get; } =
    [
        new() { Value = ResourceKind.Unknown, Display = Properties.Resources.ResourceKind_Unknown },
        new() { Value = ResourceKind.Modpack, Display = Properties.Resources.ResourceKind_Modpack },
        new() { Value = ResourceKind.Mod, Display = Properties.Resources.ResourceKind_Mod },
        new() { Value = ResourceKind.ResourcePack, Display = Properties.Resources.ResourceKind_ResourcePack },
        new() { Value = ResourceKind.ShaderPack, Display = Properties.Resources.ResourceKind_ShaderPack },
        new() { Value = ResourceKind.DataPack, Display = Properties.Resources.ResourceKind_DataPack }
    ];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedSelectorTypeProperty)
        {
            var value = change.GetNewValue<SelectorTypeModel?>()?.Value;
            if (value is not null)
            {
                Rule.Selector = value.Value;
                // NOTE: 当 SwitchPresenter 切换时会移除 ComboBox，导致 SelectedResourceKind 为 null。
                //  不懂为什么要搞这种奇葩设计导致一堆和 ComboBox 有关的问题。
                if (value.Value == Profile.Rice.Rule.SelectorType.Kind)
                {
                    SelectedResourceKind = Kinds.FirstOrDefault(x => x.Value == Rule.Kind);
                }
            }
        }

        if (change.Property == SelectedResourceKindProperty)
        {
            var value = change.GetNewValue<ResourceKindModel?>()?.Value;
            if (value is not null)
                Rule.Kind = value.Value;
        }
    }

    #region Presents

    private void TemplateDataPackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector = Profile.Rice.Rule.SelectorType.Kind;
        SelectedResourceKind = Kinds.FirstOrDefault(x => x.Value == ResourceKind.DataPack);
    }

    private void TemplateTaCZButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector = Profile.Rice.Rule.SelectorType.Tag;
        Rule.Tag = "TaC:Z";
        Rule.Destination = "tacz";
    }

    private void TemplatePointBlankButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector = Profile.Rice.Rule.SelectorType.Tag;
        Rule.Tag = "PointBlank";
        Rule.Destination = "pointblank";
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void EditRules()
    {
        if (Rule.Children is not null)
        {
            OverlayService.PopModal(new ProfileRulesModal
            {
                Rules = Rule.Children, Packages = Packages, OverlayService = OverlayService
            });
        }
    }

    #endregion
}
