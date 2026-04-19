using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.PageModels;
using Polymerium.App.Services;

namespace Polymerium.App.Dialogs;

public partial class PackageBulkUpdatePreviewerDialog : Dialog
{
    public static readonly DirectProperty<
        PackageBulkUpdatePreviewerDialog,
        bool
    > IsEnabledOnlyProperty = AvaloniaProperty.RegisterDirect<
        PackageBulkUpdatePreviewerDialog,
        bool
    >(nameof(IsEnabledOnly), o => o.IsEnabledOnly, (o, v) => o.IsEnabledOnly = v);

    public static readonly DirectProperty<
        PackageBulkUpdatePreviewerDialog,
        ObservableCollection<string>?
    > TagsProperty = AvaloniaProperty.RegisterDirect<
        PackageBulkUpdatePreviewerDialog,
        ObservableCollection<string>?
    >(nameof(Tags), o => o.Tags, (o, v) => o.Tags = v);

    public static readonly DirectProperty<
        PackageBulkUpdatePreviewerDialog,
        PackageBulkUpdatePreviewerTagPolicy
    > TagPolicyProperty = AvaloniaProperty.RegisterDirect<
        PackageBulkUpdatePreviewerDialog,
        PackageBulkUpdatePreviewerTagPolicy
    >(nameof(TagPolicy), o => o.TagPolicy, (o, v) => o.TagPolicy = v);

    public static readonly DirectProperty<
        PackageBulkUpdatePreviewerDialog,
        InstanceSetupPageModel.StateView?
    > ViewStateProperty = AvaloniaProperty.RegisterDirect<
        PackageBulkUpdatePreviewerDialog,
        InstanceSetupPageModel.StateView?
    >(nameof(ViewState), o => o.ViewState, (o, v) => o.ViewState = v);

    public PackageBulkUpdatePreviewerDialog()
    {
        InitializeComponent();
        AddHandler(LoadedEvent, OnLoadedHandler);
        AddHandler(UnloadedEvent, OnUnloadedHandler);
    }

    public bool IsEnabledOnly
    {
        get;
        set => SetAndRaise(IsEnabledOnlyProperty, ref field, value);
    }

    public ObservableCollection<string>? Tags
    {
        get;
        set => SetAndRaise(TagsProperty, ref field, value);
    }

    public required InstanceSetupPageModel.StateView? ViewState
    {
        get;
        set => SetAndRaise(ViewStateProperty, ref field, value);
    }

    public PackageBulkUpdatePreviewerTagPolicy TagPolicy
    {
        get;
        set => SetAndRaise(TagPolicyProperty, ref field, value);
    }

    public required IReadOnlyList<string> ExistingTags { get; init; }
    public required OverlayService OverlayService { get; init; }

    #region Overrides

    protected override bool ValidateResult(object? result)
    {
        // 应用之前把数据写入 Result
        Result ??= new PackageBulkUpdatePreviewerModel();
        if (Result is PackageBulkUpdatePreviewerModel model)
        {
            model.Tags = Tags?.ToList() ?? [];
            model.TagPolicy = TagPolicy;
            model.IsEnabledOnly = IsEnabledOnly;
        }

        return true;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task AddTag()
    {
        var dialog = new TagPickerDialog
        {
            ExistingTags = ExistingTags.Where(x => !Tags?.Contains(x) ?? true).ToList(),
        };
        if (
            await OverlayService.PopDialogAsync(dialog)
            && dialog.Result is string tag
            && !string.IsNullOrEmpty(tag)
        )
        {
            Tags ??= [];
            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
        }
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        Tags?.Remove(tag);
    }

    #endregion

    #region Event Handlers
    private void OnLoadedHandler(object? sender, RoutedEventArgs args)
    {
        if (ViewState?.LastChosenIsEnabledOnly is { } enabledOnly)
        {
            IsEnabledOnly = enabledOnly;
        }
        if (ViewState?.LastChosenTagPolicy is { } policy)
        {
            TagPolicy = policy;
        }
        if (ViewState?.LastChosenTags is { } tags)
        {
            Tags ??= [];
            foreach (var tag in tags)
            {
                if (!Tags.Contains(tag))
                {
                    Tags.Add(tag);
                }
            }
        }
    }

    public void OnUnloadedHandler(object? sender, RoutedEventArgs args)
    {
        ViewState?.LastChosenIsEnabledOnly = IsEnabledOnly;
        ViewState?.LastChosenTagPolicy = TagPolicy;
        ViewState?.LastChosenTags = Tags;
    }
    #endregion
}
