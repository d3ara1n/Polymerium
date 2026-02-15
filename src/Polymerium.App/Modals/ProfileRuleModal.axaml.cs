using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public static readonly DirectProperty<ProfileRuleModal, ProfileRuleModel> RuleProperty =
        AvaloniaProperty.RegisterDirect<ProfileRuleModal, ProfileRuleModel>(nameof(Rule),
                                                                            o => o.Rule,
                                                                            (o, v) => o.Rule = v);

    public required ProfileRuleModel Rule
    {
        get;
        set => SetAndRaise(RuleProperty, ref field, value);
    }

    public ProfileRuleModal() => InitializeComponent();

    public required OverlayService OverlayService { get; init; }
    public required IReadOnlyList<InstancePackageModel> Packages { get; init; }

    public IReadOnlyList<Profile.Rice.Rule.SelectorType> SelectorTypes { get; } =
        Enum.GetValues<Profile.Rice.Rule.SelectorType>();

    public IReadOnlyList<ResourceKind> Kinds { get; } = Enum.GetValues<ResourceKind>();

    #region Presents

    private void TemplateDataPackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Rule.Selector = Profile.Rice.Rule.SelectorType.Kind;
        Rule.Kind = ResourceKind.DataPack;
        Rule.Destination = "datapacks";
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

    [RelayCommand]
    private async Task PickTagAsync()
    {
        var dialog = new TagPickerDialog() { ExistingTags = Packages.SelectMany(x => x.Tags).Distinct().ToList() };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is string tag)
        {
            Rule.Tag = tag;
        }
    }

    [RelayCommand]
    private async Task PickPackageAsync()
    {
        var dialog = new PackagePickerDialog();
        dialog.SetItems(Packages);
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is InstancePackageModel package)
        {
            Rule.Purl = package.Entry.Purl;
        }
    }

    #endregion
}
