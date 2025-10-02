using System;
using System.Collections.ObjectModel;
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
    OverlayService overlayService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Reactive

    public ObservableCollection<ScreenshotGroupModel> ScreenshotGroups { get; } = [];

    [ObservableProperty]
    public partial int ScreenshotCount { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private void ViewImage(ScreenshotModel? model)
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
        await LoadModAsync();
        await LoadScreenshotsAsync();
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

        foreach (var files in dir
                             .GetFiles("*.png", SearchOption.TopDirectoryOnly)
                             .GroupBy(x => x.CreationTimeUtc.Date)
                             .OrderByDescending(x => x.Key))
        {
            var group = new ScreenshotGroupModel(files.Key);
            foreach (var file in files)
            {
                group.Screenshots.Add(new(new(file.FullName, UriKind.Absolute), file.CreationTimeUtc));
            }

            ScreenshotGroups.Add(group);
        }

        ScreenshotCount = ScreenshotGroups.Sum(x => x.Screenshots.Count);


        return Task.CompletedTask;
    }

    private async Task LoadModAsync() { }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenScreenshotFile(ScreenshotModel? model)
    {
        if (model != null)
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchFileInfoAsync(new(model.Image.LocalPath));
        }
    }


    [RelayCommand]
    private async Task DeleteScreenshotAsync(ScreenshotModel? model)
    {
        if (model != null
         && await overlayService.RequestConfirmationAsync("Are you sure you want to delete this screenshot?"))
        {
            File.Delete(model.Image.LocalPath);
            var group = ScreenshotGroups.FirstOrDefault(x => x.Screenshots.Contains(model));
            group?.Screenshots.Remove(model);
            if (group?.Screenshots.Count == 0)
            {
                ScreenshotGroups.Remove(group);
            }

            ScreenshotCount--;
        }
    }

    #endregion
}
