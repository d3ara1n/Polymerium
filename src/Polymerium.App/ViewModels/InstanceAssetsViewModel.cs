using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Trident.Abstractions;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceAssetsViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    OverlayService overlayService,
    NotificationService notificationService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Overrides

    protected override async Task OnInitializeAsync()
    {
        await Task.Run(LoadModAsync);
        await Task.Run(LoadResourcePacksAsync);
        await Task.Run(LoadDataPacksAsync);
        await Task.Run(LoadScreenshotsAsync);
    }

    #endregion

    #region Fields

    private readonly SourceCache<AssetModModel, string> _modCache = new(x => x.FilePath);
    private IDisposable? _modFilterSubscription;

    private readonly SourceCache<AssetResourcePackModel, string> _resourcePackCache = new(x => x.FilePath);
    private IDisposable? _resourcePackFilterSubscription;

    private readonly SourceCache<AssetDataPackModel, string> _dataPackCache = new(x => x.FilePath);
    private IDisposable? _dataPackFilterSubscription;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial AssetScreenshotCollection? ScreenshotGroups { get; set; }

    [ObservableProperty]
    public partial int PackageCount { get; set; }

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<AssetModModel>? Mods { get; set; }

    [ObservableProperty]
    public partial AssetModModel? SelectedMod { get; set; }

    [ObservableProperty]
    public partial string ModSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<AssetResourcePackModel>? ResourcePacks { get; set; }

    [ObservableProperty]
    public partial AssetResourcePackModel? SelectedResourcePack { get; set; }

    [ObservableProperty]
    public partial string ResourcePackSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<AssetDataPackModel>? DataPacks { get; set; }

    [ObservableProperty]
    public partial AssetDataPackModel? SelectedDataPack { get; set; }

    [ObservableProperty]
    public partial string DataPackSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModFilterActive))]
    public partial FilterModel? ModFilterEnability { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModFilterActive))]
    public partial FilterModel? ModFilterLockility { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModFilterActive))]
    public partial FilterModel? ModFilterLoader { get; set; }

    public bool IsModFilterActive =>
        ModFilterEnability?.Value != null || ModFilterLockility?.Value != null || ModFilterLoader?.Value != null;

    #endregion

    #region Other

    private Task LoadScreenshotsAsync()
    {
        var sourced = Basic.Source != null;
        var import = PathDef.Default.DirectoryOfImport(Basic.Key);
        var groups = new AssetScreenshotCollection();
        foreach (var files in AssetHelper
                             .ScanNonSymlinks(Basic.Key, "*.png", ["screenshots"])
                             .GroupBy(x => x.CreationTimeUtc.Date)
                             .OrderByDescending(x => x.Key))
        {
            var group = new AssetScreenshotGroupModel(files.Key);
            foreach (var file in files)
            {
                var imported = sourced && FileHelper.IsInDirectory(file.FullName, import);
                group.Screenshots.Add(new(new(file.FullName, UriKind.Absolute), file.CreationTimeUtc, imported));
            }

            groups.Add(group);
        }

        ScreenshotGroups = groups;
        ScreenshotGroups.ScreenshotCount = groups.Sum(x => x.Screenshots.Count);


        return Task.CompletedTask;
    }

    private Task LoadModAsync()
    {
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            PackageCount = profile.Setup.Packages.Count;
        }

        var sourced = Basic.Source != null;
        var mods = new List<AssetModModel>();
        foreach (var file in AssetHelper
                            .ScanNonSymlinks(Basic.Key, "*.jar", ["mods"])
                            .Concat(AssetHelper.ScanNonSymlinks(Basic.Key, "*.jar.disabled", ["mods"])))
        {
            // 解析 jar 文件中的 mod 元数据
            var metadata = AssetModHelper.ParseMetadata(file.FullName);

            // 尝试提取 Mod 图标
            Bitmap? icon = null;
            if (!string.IsNullOrEmpty(metadata.LogoFile))
            {
                icon = AssetModHelper.ExtractIcon(file.FullName, metadata.LogoFile);
            }

            var imported = sourced
                        && FileHelper.IsInDirectory(file.FullName, PathDef.Default.DirectoryOfImport(Basic.Key));
            var model = new AssetModModel(file, icon ?? AssetUriIndex.DirtImageBitmap, metadata, imported)
            {
                IsEnabled = file.Name.EndsWith(".jar")
            };

            mods.Add(model);
        }

        _modCache.AddOrUpdate(mods);

        // 设置过滤器
        _modFilterSubscription?.Dispose();
        var searchFilter = this.WhenValueChanged(x => x.ModSearchText).Select(BuildSearchFilter);
        var enabilityFilter = this.WhenValueChanged(x => x.ModFilterEnability).Select(BuildEnabilityFilter);
        var lockilityFilter = this.WhenValueChanged(x => x.ModFilterLockility).Select(BuildLockilityFilter);
        var loaderFilter = this.WhenValueChanged(x => x.ModFilterLoader).Select(BuildLoaderFilter);

        var combinedFilter = searchFilter.CombineLatest(enabilityFilter,
                                                        lockilityFilter,
                                                        loaderFilter,
                                                        (search, enability, lockility, loader) =>
                                                            new Func<AssetModModel, bool>(x => search(x)
                                                             && enability(x)
                                                             && lockility(x)
                                                             && loader(x)));

        _modFilterSubscription = _modCache.Connect().Filter(combinedFilter).Bind(out var view).Subscribe();

        Mods = view;

        return Task.CompletedTask;
    }

    private Task LoadResourcePacksAsync()
    {
        var sourced = Basic.Source != null;
        var resourcePacks = new List<AssetResourcePackModel>();
        foreach (var file in AssetHelper
                            .ScanNonSymlinks(Basic.Key, "*.zip", ["resourcepacks"])
                            .Concat(AssetHelper.ScanNonSymlinks(Basic.Key, "*.zip.disabled", ["resourcepacks"])))
        {
            // 解析 zip 文件中的资源包元数据
            var metadata = AssetResourcePackHelper.ParseMetadata(file.FullName);

            // 尝试提取资源包图标
            var icon = AssetResourcePackHelper.ExtractIcon(file.FullName);

            var imported = sourced
                        && FileHelper.IsInDirectory(file.FullName, PathDef.Default.DirectoryOfImport(Basic.Key));
            var model = new AssetResourcePackModel(file, icon ?? AssetUriIndex.DirtImageBitmap, metadata, imported)
            {
                IsEnabled = file.Name.EndsWith(".zip")
            };

            resourcePacks.Add(model);
        }

        _resourcePackCache.AddOrUpdate(resourcePacks);

        // 设置过滤器
        _resourcePackFilterSubscription?.Dispose();
        var searchFilter = this.WhenValueChanged(x => x.ResourcePackSearchText).Select(BuildResourcePackSearchFilter);

        _resourcePackFilterSubscription = _resourcePackCache
                                         .Connect()
                                         .Filter(searchFilter)
                                         .Bind(out var view)
                                         .Subscribe();

        ResourcePacks = view;

        return Task.CompletedTask;
    }

    private Task LoadDataPacksAsync()
    {
        var sourced = Basic.Source != null;
        var dataPacks = new List<AssetDataPackModel>();

        // 扫描 datapacks 目录下的 zip 文件
        foreach (var file in AssetHelper
                            .ScanNonSymlinks(Basic.Key, "*.zip", ["datapacks"])
                            .Concat(AssetHelper.ScanNonSymlinks(Basic.Key, "*.zip.disabled", ["datapacks"])))
        {
            // 解析 zip 文件中的数据包元数据
            var metadata = AssetDataPackHelper.ParseMetadata(file.FullName);

            // 尝试提取数据包图标
            var icon = AssetDataPackHelper.ExtractIcon(file.FullName);

            var imported = sourced
                        && FileHelper.IsInDirectory(file.FullName, PathDef.Default.DirectoryOfImport(Basic.Key));
            var model = new AssetDataPackModel(file, icon ?? AssetUriIndex.DirtImageBitmap, metadata, imported)
            {
                IsEnabled = file.Name.EndsWith(".zip")
            };

            dataPacks.Add(model);
        }

        _dataPackCache.AddOrUpdate(dataPacks);

        // 设置过滤器
        _dataPackFilterSubscription?.Dispose();
        var searchFilter = this.WhenValueChanged(x => x.DataPackSearchText).Select(BuildDataPackSearchFilter);

        _dataPackFilterSubscription = _dataPackCache.Connect().Filter(searchFilter).Bind(out var view).Subscribe();

        DataPacks = view;

        return Task.CompletedTask;
    }

    #endregion

    #region Filters

    private static Func<AssetModModel, bool> BuildSearchFilter(string? searchText) =>
        x => string.IsNullOrEmpty(searchText)
          || x.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
          || (x.Metadata.ModId?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
          || (x.Metadata.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);

    private static Func<AssetModModel, bool> BuildEnabilityFilter(FilterModel? filter) =>
        filter?.Value switch
        {
            true => x => x.IsEnabled,
            false => x => !x.IsEnabled,
            _ => _ => true
        };

    private static Func<AssetModModel, bool> BuildLockilityFilter(FilterModel? filter) =>
        filter?.Value switch
        {
            true => x => x.IsLocked,
            false => x => !x.IsLocked,
            _ => _ => true
        };

    private static Func<AssetModModel, bool> BuildLoaderFilter(FilterModel? filter) =>
        filter?.Value switch
        {
            ModLoaderKind loader => x => x.Metadata.LoaderType == loader,
            _ => _ => true
        };

    private static Func<AssetResourcePackModel, bool> BuildResourcePackSearchFilter(string? searchText) =>
        x => string.IsNullOrEmpty(searchText)
          || x.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
          || (x.Metadata.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);

    private static Func<AssetDataPackModel, bool> BuildDataPackSearchFilter(string? searchText) =>
        x => string.IsNullOrEmpty(searchText)
          || x.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
          || (x.Metadata.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);

    #endregion

    #region Commands

    [RelayCommand]
    private void ViewImage(AssetScreenshotModel? model)
    {
        if (model != null)
        {
            overlayService.PopToast(new ImageViewerToast { ImageSource = model.Image });
        }
    }

    [RelayCommand]
    private void OpenScreenshotFile(AssetScreenshotModel? model)
    {
        if (model != null)
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchFileInfoAsync(new(model.Image.LocalPath));
        }
    }

    [RelayCommand]
    private void OpenFolder(string? filePath)
    {
        if (filePath != null)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && Directory.Exists(dir))
            {
                TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
            }
        }
    }

    private bool CanDeleteScreenshot(AssetScreenshotModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanDeleteScreenshot))]
    private async Task DeleteScreenshotAsync(AssetScreenshotModel? model)
    {
        if (model != null
         && ScreenshotGroups is not null
         && File.Exists(model.Image.LocalPath)
         && await overlayService.RequestConfirmationAsync("Are you sure you want to delete this screenshot?"))
        {
            try
            {
                File.Delete(model.Image.LocalPath);

                var group = ScreenshotGroups.FirstOrDefault(x => x.Screenshots.Contains(model));
                group?.Screenshots.Remove(model);
                if (group?.Screenshots.Count == 0)
                {
                    ScreenshotGroups.Remove(group);
                }

                ScreenshotGroups.ScreenshotCount--;
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to delete screenshot file");
            }
        }
    }

    private bool CanToggleMod(AssetModModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanToggleMod))]
    private void ToggleMod(AssetModModel? model)
    {
        if (model == null || Mods == null)
        {
            return;
        }

        var oldPath = model.FilePath;
        var newPath = model.IsEnabled ? oldPath + ".disabled" : oldPath.Replace(".disabled", "");

        try
        {
            File.Move(oldPath, newPath);
            model.IsEnabled = !model.IsEnabled;
            model.FilePath = newPath;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to toggle mod");
        }
    }

    private bool CanDeleteMod(AssetModModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanDeleteMod))]
    private async Task DeleteModAsync(AssetModModel? model)
    {
        if (model != null
         && Mods is not null
         && File.Exists(model.FilePath)
         && await overlayService.RequestConfirmationAsync($"Are you sure you want to delete '{model.DisplayName}'?"))
        {
            try
            {
                File.Delete(model.FilePath);
                _modCache.Remove(model);
                if (SelectedMod == model)
                {
                    SelectedMod = null;
                }

                notificationService.PopMessage($"Mod '{model.DisplayName}' deleted successfully", "Mod Deleted");
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to delete mod file");
            }
        }
    }

    private bool CanToggleResourcePack(AssetResourcePackModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanToggleResourcePack))]
    private void ToggleResourcePack(AssetResourcePackModel? model)
    {
        if (model == null || ResourcePacks == null)
        {
            return;
        }

        var oldPath = model.FilePath;
        var newPath = model.IsEnabled ? oldPath + ".disabled" : oldPath.Replace(".disabled", "");

        try
        {
            File.Move(oldPath, newPath);
            model.IsEnabled = !model.IsEnabled;
            model.FilePath = newPath;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to toggle resource pack");
        }
    }

    private bool CanDeleteResourcePack(AssetResourcePackModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanDeleteResourcePack))]
    private async Task DeleteResourcePackAsync(AssetResourcePackModel? model)
    {
        if (model != null
         && ResourcePacks is not null
         && File.Exists(model.FilePath)
         && await overlayService.RequestConfirmationAsync($"Are you sure you want to delete '{model.DisplayName}'?"))
        {
            try
            {
                File.Delete(model.FilePath);
                _resourcePackCache.Remove(model);
                if (SelectedResourcePack == model)
                {
                    SelectedResourcePack = null;
                }

                notificationService.PopMessage($"Resource pack '{model.DisplayName}' deleted successfully",
                                               "Resource Pack Deleted");
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to delete resource pack file");
            }
        }
    }

    private bool CanToggleDataPack(AssetDataPackModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanToggleDataPack))]
    private void ToggleDataPack(AssetDataPackModel? model)
    {
        if (model == null || DataPacks == null)
        {
            return;
        }

        var oldPath = model.FilePath;
        var newPath = model.IsEnabled ? oldPath + ".disabled" : oldPath.Replace(".disabled", "");

        try
        {
            File.Move(oldPath, newPath);
            model.IsEnabled = !model.IsEnabled;
            model.FilePath = newPath;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to toggle data pack");
        }
    }

    private bool CanDeleteDataPack(AssetDataPackModel? model) => model is { IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanDeleteDataPack))]
    private async Task DeleteDataPackAsync(AssetDataPackModel? model)
    {
        if (model != null
         && DataPacks is not null
         && File.Exists(model.FilePath)
         && await overlayService.RequestConfirmationAsync($"Are you sure you want to delete '{model.DisplayName}'?"))
        {
            try
            {
                File.Delete(model.FilePath);
                _dataPackCache.Remove(model);
                if (SelectedDataPack == model)
                {
                    SelectedDataPack = null;
                }

                notificationService.PopMessage($"Data pack '{model.DisplayName}' deleted successfully",
                                               "Data Pack Deleted");
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to delete data pack file");
            }
        }
    }

    #endregion
}
