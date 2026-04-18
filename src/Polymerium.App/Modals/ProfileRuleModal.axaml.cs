using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
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
    public static readonly DirectProperty<ProfileRuleModal, ProfileRuleModel> RuleProperty =
        AvaloniaProperty.RegisterDirect<ProfileRuleModal, ProfileRuleModel>(
            nameof(Rule),
            o => o.Rule,
            (o, v) => o.Rule = v
        );

    public ProfileRuleModal() => InitializeComponent();

    public required ProfileRuleModel Rule
    {
        get;
        set => SetAndRaise(RuleProperty, ref field, value);
    }

    public required OverlayService OverlayService { get; init; }
    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }

    public IReadOnlyList<Profile.Rice.Rule.RuleSelector.SelectorType> SelectorTypes { get; } =
        Enum.GetValues<Profile.Rice.Rule.RuleSelector.SelectorType>();

    public IReadOnlyList<ResourceKind> Kinds { get; } = Enum.GetValues<ResourceKind>();

    #region Presents

    private void TemplateDataPackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector.Type = Profile.Rice.Rule.RuleSelector.SelectorType.Kind;
        Rule.Selector.Kind = ResourceKind.DataPack;
        Rule.Destination = "datapacks";
    }

    private void TemplateTaCZButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector.Type = Profile.Rice.Rule.RuleSelector.SelectorType.Tag;
        Rule.Selector.Tag = "TaC:Z";
        Rule.Destination = "tacz";
    }

    private void TemplatePointBlankButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector.Type = Profile.Rice.Rule.RuleSelector.SelectorType.Tag;
        Rule.Selector.Tag = "PointBlank";
        Rule.Destination = "pointblank";
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void EditSelectors()
    {
        if (Rule.Selector.Children is not null)
        {
            OverlayService.PopModal(
                new ProfileRuleSelectorsModal
                {
                    Selectors = Rule.Selector.Children,
                    Packages = Packages,
                    OverlayService = OverlayService,
                }
            );
        }
    }

    [RelayCommand]
    private async Task PickTagAsync()
    {
        var dialog = new TagPickerDialog
        {
            ExistingTags = Packages.SelectMany(x => x.Tags).Distinct().ToList(),
        };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is string tag)
        {
            Rule.Selector.Tag = tag;
        }
    }

    [RelayCommand]
    private async Task PickPackageAsync()
    {
        var dialog = new PackagePickerDialog();
        dialog.SetItems(Packages);
        if (
            await OverlayService.PopDialogAsync(dialog)
            && dialog.Result is InstancePackageModel package
        )
        {
            Rule.Selector.Purl = package.Entry.Purl;
        }
    }

    #endregion
}
