using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Trident.Abstractions;

namespace Polymerium.App.Dialogs;

public partial class ModpackExporterDialog : Dialog
{
    public static readonly StyledProperty<string> SelectedExporterLabelProperty =
        AvaloniaProperty.Register<ModpackExporterDialog, string>(nameof(SelectedExporterLabel));

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

    public ModpackExporterDialog() => InitializeComponent();
    public IReadOnlyList<string> ExporterLabels { get; } = ["curseforge", "modrinth"];

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

    #region Commands

    [RelayCommand]
    private void OpenImportFolder()
    {
        if (Result is ModpackExporterModel model)
        {
            TopLevel
               .GetTopLevel(MainWindow.Instance)
              ?.Launcher.LaunchDirectoryInfoAsync(new(PathDef.Default.DirectoryOfImport(model.Key)));
        }
    }

    #endregion
}
