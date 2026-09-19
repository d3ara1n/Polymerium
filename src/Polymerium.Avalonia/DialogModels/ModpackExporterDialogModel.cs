using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.DialogModels;

public partial class ModpackExporterDialogModel : ViewModelBase
{
    private readonly OverlayService _overlayService;

    public ModpackExporterDialogModel(IViewContext<Parameter> context, OverlayService overlayService)
    {
        _overlayService = overlayService;
        Source = context.Parameter!;
        PackData = new(Source.Pack);
        Formats =
        [
            new()
            {
                Label = "curseforge",
                DisplayName = "CurseForge",
                SupportsOnline = true,
                SupportsOffline = true,
                SupportsAuthor = true,
                SupportsVersion = true,
                CapabilityKey = nameof(LanguageManager.Keys.ModpackExporterDialog_CapabilityCurseForgeText)
            },
            new()
            {
                Label = "modrinth",
                DisplayName = "Modrinth",
                SupportsOnline = true,
                SupportsOffline = true,
                SupportsAuthor = false,
                SupportsVersion = true,
                CapabilityKey = nameof(LanguageManager.Keys.ModpackExporterDialog_CapabilityModrinthText)
            },
            new()
            {
                Label = "multimc",
                DisplayName = "MultiMC",
                SupportsOnline = false,
                SupportsOffline = true,
                SupportsAuthor = false,
                SupportsVersion = false,
                CapabilityKey = nameof(LanguageManager.Keys.ModpackExporterDialog_CapabilityMultiMcText)
            }
        ];
        var home = PathDef.Default.DirectoryOfHome(Source.Key);
        var icon = InstanceHelper.PickIcon(Source.Key);
        Options =
        [
            new NativeExportOptionModel
            {
                Icon = Symbol.Home,
                LabelKey = nameof(LanguageManager.Keys.ModpackExporterDialog_NativeLabelText),
                ReleaseFiles =
                [
                    new() { Name = "README.md", Exists = File.Exists(Path.Combine(home, "README.md")) },
                    new() { Name = "CHANGELOG.md", Exists = File.Exists(Path.Combine(home, "CHANGELOG.md")) },
                    new() { Name = "LICENSE", Exists = File.Exists(Path.Combine(home, "LICENSE.txt")) },
                    new() { Name = icon is null ? "icon.*" : Path.GetFileName(icon), Exists = File.Exists(icon) }
                ]
            },
            new ExternalExportOptionModel
            {
                Icon = Symbol.Cloud,
                LabelKey = nameof(LanguageManager.Keys.ModpackExporterDialog_ExternalLabelText)
            }
        ];
        SelectedFormat = Formats[0];
        SelectedOption = Options[0];
    }

    public Parameter Source { get; }
    public PackDataModel PackData { get; }
    public IReadOnlyList<ExportOptionModel> Options { get; }
    public IReadOnlyList<ModpackExporterFormatModel> Formats { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthorVisible), nameof(IsVersionVisible), nameof(IsOfflineModeVisible),
        nameof(IsPatchesWarningVisible), nameof(Result))]
    public partial ExportOptionModel? SelectedOption { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthorVisible), nameof(IsVersionVisible), nameof(IsOfflineModeVisible), nameof(Result))]
    public partial ModpackExporterFormatModel? SelectedFormat { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Result))]
    public partial string? NameOverride { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Result))]
    public partial string? AuthorOverride { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Result))]
    public partial string? VersionOverride { get; set; }

    public bool IsAuthorVisible => SelectedOption is NativeExportOptionModel || SelectedFormat?.SupportsAuthor == true;
    public bool IsVersionVisible => SelectedOption is NativeExportOptionModel || SelectedFormat?.SupportsVersion == true;
    public bool IsOfflineModeVisible => SelectedOption is NativeExportOptionModel
                                       || SelectedFormat is { SupportsOnline: true, SupportsOffline: true };
    public bool IsPatchesWarningVisible => Source.HasPatches && SelectedOption is ExternalExportOptionModel;

    public ModpackExporterModel? Result
    {
        get
        {
            var label = SelectedOption switch
            {
                NativeExportOptionModel => "trident",
                ExternalExportOptionModel => SelectedFormat?.Label,
                _ => null
            };
            return label is null
                       ? null
                       : new(Source.Key,
                             label,
                             string.IsNullOrWhiteSpace(NameOverride) ? Source.Name : NameOverride,
                             string.IsNullOrWhiteSpace(AuthorOverride) ? Source.Author : AuthorOverride,
                             string.IsNullOrWhiteSpace(VersionOverride) ? Source.Version : VersionOverride);
        }
    }

    [RelayCommand]
    private Task OpenImportFolder() =>
        TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
            new(PathDef.Default.DirectoryOfImport(Source.Key)),
            LanguageManager.Instance.ModpackExporterDialog_OpenImportFolderDangerNotificationTitle.Current());

    [RelayCommand]
    private async Task AddTag()
    {
        var dialog = new TagPickerDialog { ExistingTags = [.. Source.AvailableTags.Except(PackData.ExcludedTags)] };
        if (await _overlayService.PopDialogAsync(dialog) && dialog.Result is string tag
            && !string.IsNullOrEmpty(tag) && !PackData.ExcludedTags.Contains(tag))
        {
            PackData.ExcludedTags.Add(tag);
        }
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag is not null)
        {
            PackData.ExcludedTags.Remove(tag);
        }
    }

    public sealed record Parameter(
        string Key,
        string InstanceName,
        PackData Pack,
        IReadOnlyList<string> AvailableTags,
        int PackageCount,
        string LoaderLabel,
        bool HasPatches,
        string Name,
        string Author,
        string Version);
}
