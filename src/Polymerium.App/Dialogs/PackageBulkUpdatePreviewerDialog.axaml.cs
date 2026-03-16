using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
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

    public PackageBulkUpdatePreviewerDialog()
    {
        InitializeComponent();
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
}
