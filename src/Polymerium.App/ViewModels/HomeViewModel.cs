using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly ProfileManager _profileManger;
    private readonly TaskService _taskService;
    private readonly ThumbnailSaver _thumbnailSaver;

    public HomeViewModel(ProfileManager profileManger, NavigationService navigationService,
        ThumbnailSaver thumbnailSaver, TaskService taskService)
    {
        _profileManger = profileManger;
        _navigationService = navigationService;
        _thumbnailSaver = thumbnailSaver;
        _taskService = taskService;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);
        OpenExternalUrlCommand = new RelayCommand<string>(OpenExternalUrl);
        LearnMoreCommand = new RelayCommand(LearnMore);

        Recents = _profileManger.Managed
            .Select(x => (
                x.Value.Value.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play),
                x.Value.Value, x.Key))
            .Where(x => x.Item1 != null)
            .OrderByDescending(x => x.Item1)
            .Select(x => new RecentModel(x.Item3, x.Item2, thumbnailSaver.Get(x.Item3), GotoInstanceViewCommand))
            .ToList();

        IsDeveloperModeRequired = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
                "AllowDevelopmentWithoutDevLicense", 0) is not 1;
        // TODO: 这里无法识别，所以彻底关闭掉了
        IsDeveloperModeRequired = false;
    }

    public IList<RecentModel> Recents { get; }
    public ICommand OpenExternalUrlCommand { get; }
    public ICommand GotoInstanceViewCommand { get; }

    public ICommand LearnMoreCommand { get; }

    public bool IsDeveloperModeRequired { get; }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
        {
            _navigationService.Navigate(typeof(InstanceView), key);
        }
    }

    private void OpenExternalUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url)) UriFileHelper.OpenInExternal(url);
    }

    private void LearnMore() =>
        UriFileHelper.OpenInExternal(
            "https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development");
}