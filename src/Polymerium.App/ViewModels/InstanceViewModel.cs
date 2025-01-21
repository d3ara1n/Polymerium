using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
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
    public InstanceViewModel(ViewBag bag, ProfileManager profileManager, NavigationService navigationService)
    {
        _profileManager = profileManager;
        _navigationService = navigationService;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                Basic = new InstanceBasicModel(key, profile.Name, profile.Setup.Version, profile.Setup.Loader,
                    profile.Setup.Source);
                var screenshotPath = ProfileHelper.PickScreenshotRandomly(key);
                Screenshot = screenshotPath is not null
                    ? new Bitmap(screenshotPath)
                    : new Bitmap(AssetUriIndex.WALLPAPER_IMAGE);
                LaunchBarModel = new InstanceLaunchBarModel();
                PackageCount = profile.Setup.Stage.Count + profile.Setup.Stash.Count;
                StatsChartSeries =
                [
                    new ColumnSeries<double>
                    {
                        Name = "Daily Playing Hours",
                        Values = [11, 4, 5, 14, 19, 1, 9]
                    }
                ];
                StatsChartXAxes =
                [
                    new Axis
                    {
                        Labels = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Yesterday", "Today"]
                    }
                ];
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                    $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }
    }

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        // TODO: Load SourceUrl
        return base.OnInitializedAsync(dispatcher, token);
    }

    #region Injected Services

    private readonly ProfileManager _profileManager;
    private readonly NavigationService _navigationService;

    #endregion

    #region Rectives Models

    [ObservableProperty] private InstanceBasicModel _basic;
    [ObservableProperty] private Bitmap _screenshot;
    [ObservableProperty] private Uri? _sourceUrl;
    [ObservableProperty] private InstanceLaunchBarModel _launchBarModel;
    [ObservableProperty] private int _packageCount;
    [ObservableProperty] private IEnumerable<ISeries<double>> _statsChartSeries;
    [ObservableProperty] private IEnumerable<Axis> _statsChartXAxes;

    #endregion

    #region Command Handlers

    [RelayCommand]
    private void OpenSourceUrl(Uri? url)
    {
        if (url is not null) Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    [RelayCommand]
    private void GotoPropertyView()
    {
        _navigationService.Navigate<InstancePropertyView>(Basic.Key);
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
}