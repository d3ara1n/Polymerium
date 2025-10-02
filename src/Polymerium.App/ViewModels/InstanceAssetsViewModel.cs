using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Trident.Abstractions;
using Trident.Core.Services;

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
        await Task.Run(LoadScreenshotsAsync);
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial AssetScreenshotCollection? ScreenshotGroups { get; set; }

    [ObservableProperty]
    public partial int PackageCount { get; set; }

    [ObservableProperty]
    public partial AssetModCollection? Mods { get; set; }

    [ObservableProperty]
    public partial AssetModModel? SelectedMod { get; set; }

    [ObservableProperty]
    public partial string ModSearchText { get; set; } = string.Empty;

    #endregion

    #region Other

    private Task LoadScreenshotsAsync()
    {
        var dir = new DirectoryInfo(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "screenshots"));
        if (!dir.Exists)
        {
            ScreenshotGroups = [];
            return Task.CompletedTask;
        }

        var groups = new AssetScreenshotCollection();
        foreach (var files in dir
                             .GetFiles("*.png", SearchOption.TopDirectoryOnly)
                             .GroupBy(x => x.CreationTimeUtc.Date)
                             .OrderByDescending(x => x.Key))
        {
            var group = new AssetScreenshotGroupModel(files.Key);
            foreach (var file in files)
            {
                group.Screenshots.Add(new(new(file.FullName, UriKind.Absolute), file.CreationTimeUtc));
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

        var modsDir = new DirectoryInfo(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "mods"));
        if (!modsDir.Exists)
        {
            Mods = [];
            return Task.CompletedTask;
        }

        var mods = new AssetModCollection();
        foreach (var file in modsDir
                            .GetFiles("*.jar", SearchOption.TopDirectoryOnly)
                            .Concat(modsDir.GetFiles("*.jar.disabled", SearchOption.TopDirectoryOnly)))
        {
            if (File.ResolveLinkTarget(file.FullName, false) is not null)
            {
                // 跳过包引用
                continue;
            }

            // 解析 jar 文件中的 mod 元数据
            var metadata = ModMetadataHelper.ParseMetadata(file.FullName);

            // 尝试提取 Mod 图标
            Bitmap? icon = null;
            if (!string.IsNullOrEmpty(metadata.LogoFile))
            {
                icon = ModMetadataHelper.ExtractIcon(file.FullName, metadata.LogoFile);
            }

            var model = new AssetModModel(file, icon ?? AssetUriIndex.DirtImageBitmap, metadata)
            {
                IsEnabled = file.Name.EndsWith(".jar")
            };

            mods.Add(model);
        }

        Mods = mods;

        return Task.CompletedTask;
    }

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

    [RelayCommand]
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

    [RelayCommand]
    private void OpenModFile(AssetModModel? model)
    {
        if (model != null && File.Exists(model.FilePath))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchFileInfoAsync(new(model.FilePath));
        }
    }

    [RelayCommand]
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
                Mods.Remove(model);
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

    #endregion
}
