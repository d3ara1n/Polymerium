using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Dialogs;

public partial class ModpackExporterDialog : Dialog
{
    public static readonly StyledProperty<ModpackExporterFormatModel> SelectedExporterLabelProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, ModpackExporterFormatModel>(nameof(SelectedExporterLabel));

    public static readonly StyledProperty<string> NameOverrideProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(NameOverride));

    public static readonly StyledProperty<string> AuthorOverrideProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(AuthorOverride));

    public static readonly StyledProperty<int> PackageCountProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, int>(nameof(PackageCount));

    public static readonly StyledProperty<string> LoaderLabelProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(LoaderLabel));

    public static readonly StyledProperty<string> NameOriginalProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(NameOriginal));

    public static readonly StyledProperty<string> AuthorOriginalProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(AuthorOriginal));

    public static readonly StyledProperty<string> VersionOverrideProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(VersionOverride));

    public static readonly StyledProperty<string> VersionOriginalProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(VersionOriginal));

    public static readonly DirectProperty<ModpackExporterDialog, PackDataModel?> PackDataProperty =
        AvaloniaProperty.RegisterDirect<ModpackExporterDialog, PackDataModel?>(nameof(PackData),
                                                                               o => o.PackData,
                                                                               (o, v) => o.PackData = v);

    public ModpackExporterDialog() => InitializeComponent();

    public IReadOnlyList<ModpackExporterFormatModel> ExporterLabels { get; } =
    [
        new() { Icon = Symbol.FolderZip, Label = "trident", SupportsOffline = false, SupportsOnline = true },
        new() { Icon = Symbol.FolderZip, Label = "curseforge", SupportsOffline = false, SupportsOnline = true },
        new() { Icon = Symbol.FolderZip, Label = "modrinth", SupportsOffline = false, SupportsOnline = true },
        new() { Icon = Symbol.FolderZip, Label = "multimc", SupportsOffline = true, SupportsOnline = false }
    ];

    public ModpackExporterFormatModel SelectedExporterLabel
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
        // NOTE: Avalonia TabStrip 机制缺陷（销毁后置空 SelectedItem、首次选中不发变更通知），
        //  故需要这些代理属性与验证代码。
        if (result is ModpackExporterModel model)
        {
            if (!string.IsNullOrEmpty(SelectedExporterLabel.Label))
            {
                model.SelectedExporterLabel = SelectedExporterLabel.Label;
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
    private Task OpenImportFolder()
    {
        if (Result is ModpackExporterModel model)
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevel.GetTopLevel(this),
                                                           new(PathDef.Default.DirectoryOfImport(model.Key)),
                                                           LanguageManager.Instance.ModpackExporterDialog_OpenImportFolderDangerNotificationTitle.Current());
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddTag()
    {
        if (PackData is null)
        {
            return;
        }

        var dialog = new TagPickerDialog { ExistingTags = [.. AvailableTags.Except(PackData.ExcludedTags)] };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is string tag && !string.IsNullOrEmpty(tag))
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
