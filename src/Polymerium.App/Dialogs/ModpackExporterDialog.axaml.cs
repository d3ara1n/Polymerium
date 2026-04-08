using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Dialogs;

public partial class ModpackExporterDialog : Dialog
{
    public static readonly StyledProperty<string> SelectedExporterLabelProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(SelectedExporterLabel));

    public static readonly StyledProperty<string> NameOverrideProperty = AvaloniaProperty.Register<
        ModpackExporterDialog,
        string
    >(nameof(NameOverride));

    public static readonly StyledProperty<string> AuthorOverrideProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(AuthorOverride));

    public static readonly StyledProperty<int> PackageCountProperty = AvaloniaProperty.Register<
        ModpackExporterDialog,
        int
    >(nameof(PackageCount));

    public static readonly StyledProperty<string> LoaderLabelProperty = AvaloniaProperty.Register<
        ModpackExporterDialog,
        string
    >(nameof(LoaderLabel));

    public static readonly StyledProperty<string> NameOriginalProperty = AvaloniaProperty.Register<
        ModpackExporterDialog,
        string
    >(nameof(NameOriginal));

    public static readonly StyledProperty<string> AuthorOriginalProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(AuthorOriginal));

    public static readonly StyledProperty<string> VersionOverrideProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(VersionOverride));

    public static readonly StyledProperty<string> VersionOriginalProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(VersionOriginal));

    public static readonly DirectProperty<ModpackExporterDialog, PackDataModel?> PackDataProperty =
        AvaloniaProperty.RegisterDirect<ModpackExporterDialog, PackDataModel?>(
            nameof(PackData),
            o => o.PackData,
            (o, v) => o.PackData = v
        );

    public ModpackExporterDialog()
    {
        InitializeComponent();
    }

    public IReadOnlyList<string> ExporterLabels { get; } = ["trident", "curseforge", "modrinth"];

    public string SelectedExporterLabel
    {
        get => GetValue(SelectedExporterLabelProperty);
        set => SetValue(SelectedExporterLabelProperty, value);
    }

    public string NameOverride
    {
        get => GetValue(NameOverrideProperty);
        set => SetValue(NameOverrideProperty, value);
    }

    public string AuthorOverride
    {
        get => GetValue(AuthorOverrideProperty);
        set => SetValue(AuthorOverrideProperty, value);
    }

    public required int PackageCount
    {
        get => GetValue(PackageCountProperty);
        set => SetValue(PackageCountProperty, value);
    }

    public required string LoaderLabel
    {
        get => GetValue(LoaderLabelProperty);
        set => SetValue(LoaderLabelProperty, value);
    }

    public required string NameOriginal
    {
        get => GetValue(NameOriginalProperty);
        set => SetValue(NameOriginalProperty, value);
    }

    public required string AuthorOriginal
    {
        get => GetValue(AuthorOriginalProperty);
        set => SetValue(AuthorOriginalProperty, value);
    }

    public string VersionOverride
    {
        get => GetValue(VersionOverrideProperty);
        set => SetValue(VersionOverrideProperty, value);
    }

    public required string VersionOriginal
    {
        get => GetValue(VersionOriginalProperty);
        set => SetValue(VersionOriginalProperty, value);
    }

    public PackDataModel? PackData
    {
        get;
        set => SetAndRaise(PackDataProperty, ref field, value);
    }

    public required PackData Pack
    {
        get;
        init
        {
            field = value;
            PackData = new(value);
        }
    }

    public required IReadOnlyList<string> AvailableTags { get; init; }
    public required OverlayService OverlayService { get; init; }

    #region Overrides

    protected override bool ValidateResult(object? result)
    {
        // 由于 Avalonia 的 TabStrip 机制导致其根本没法用，所以需要写一大堆代理属性和验证代码
        // 1. TabStrip 会在销毁后将 SelectedItem 设置为 null，导致 SelectedExporterLabel 为 null
        // 2. TabStrip 不会在第一次选中时触发 SelectedItem 的变更通知，导致 SelectedExporterLabel 默认为空
        if (result is ModpackExporterModel model)
        {
            if (!string.IsNullOrEmpty(SelectedExporterLabel))
            {
                model.SelectedExporterLabel = SelectedExporterLabel;
            }

            if (!string.IsNullOrEmpty(NameOverride))
            {
                model.NameOverride = NameOverride;
            }

            if (!string.IsNullOrEmpty(AuthorOverride))
            {
                model.AuthorOverride = AuthorOverride;
            }

            if (!string.IsNullOrEmpty(VersionOverride))
            {
                model.VersionOverride = VersionOverride;
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenImportFolder()
    {
        if (Result is ModpackExporterModel model)
        {
            TopLevel
                .GetTopLevel(MainWindow.Instance)
                ?.Launcher.LaunchDirectoryInfoAsync(
                    new(PathDef.Default.DirectoryOfImport(model.Key))
                );
        }
    }

    [RelayCommand]
    private async Task AddTag()
    {
        if (PackData is null)
            return;
        var dialog = new TagPickerDialog
        {
            ExistingTags = [.. AvailableTags.Except(PackData.ExcludedTags)],
        };
        if (
            await OverlayService.PopDialogAsync(dialog)
            && dialog.Result is string tag
            && !string.IsNullOrEmpty(tag)
        )
        {
            if (!PackData.ExcludedTags.Contains(tag))
            {
                PackData.ExcludedTags.Add(tag);
            }
        }
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag == null || PackData == null)
        {
            return;
        }

        PackData.ExcludedTags.Remove(tag);
    }

    #endregion
}
