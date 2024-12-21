using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ViewModelBase
{
    #region Injected Services

    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    #endregion

    #region Models

    [ObservableProperty] private string _key;
    [ObservableProperty] private string _name;
    [ObservableProperty] private Bitmap _thumbnail;
    [ObservableProperty] private InstanceSourceModel? _source;
    [ObservableProperty] private Bitmap _screenshot;
    [ObservableProperty] private InstanceLaunchBarModel _launchBarModel;

    #endregion

    #region Command Handlers

    [RelayCommand]
    private void OpenSourceUrl(string? url)
    {
        if (url is not null) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    [RelayCommand]
    private void GotoPropertyView()
    {
        _navigationService.Navigate<InstancePropertyView>(Key);
    }

    [RelayCommand]
    private void GotoSetupView()
    {
        _navigationService.Navigate<InstanceSetupView>();
    }


    [RelayCommand]
    private void Play()
    {
        LaunchBarModel.State = LaunchBarState.Building;
    }

    [RelayCommand]
    private void Abort()
    {
        LaunchBarModel.State = LaunchBarState.Running;
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        LaunchBarModel.State = LaunchBarState.Idle;
    }

    #endregion

    public InstanceViewModel(ViewBag bag, ProfileService profileService, NavigationService navigationService)
    {
        _profileService = profileService;
        _navigationService = navigationService;

        if (bag.Parameter is string key)
        {
            if (profileService.TryGetImmutable(key, out var profile))
            {
                Key = key;
                Name = profile.Name;
                var iconPath = ProfileHelper.PickIcon(key);
                Thumbnail = iconPath is not null
                    ? new Bitmap(iconPath)
                    : new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE)));
                var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
                Screenshot = screenshotPath is not null
                    ? new Bitmap(screenshotPath)
                    : new Bitmap(AssetUriIndex.WALLPAPER_IMAGE);
                LaunchBarModel = new InstanceLaunchBarModel();
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                    $"Key '{key}' is not valid instance or not found");
            }
        }
        else throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
    }

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        // TODO: Load SourceUrl
        return base.OnInitializedAsync(dispatcher, token);
    }
}