using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    #region Reactive

    [ObservableProperty]
    public partial AssetScreenshotCollection? ScreenshotGroups { get; set; }

    [ObservableProperty]
    public partial int PackageCount { get; set; }

    [ObservableProperty]
    public partial AssetModCollection? Mods { get; set; }

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

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync()
    {
        await Task.Run(LoadModAsync);
        await Task.Run(LoadScreenshotsAsync);
    }

    #endregion

    #region Other

    private Task LoadScreenshotsAsync()
    {
        var dir = new DirectoryInfo(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "screenshots"));
        if (!dir.Exists)
        {
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

        return Task.CompletedTask;
    }

    #endregion

    #region Commands

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

    #endregion
}
