using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Extensions;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Reactive.Bindings;
using Trident.Abstractions;
using static Trident.Abstractions.Profile.RecordData.TimelinePoint;

namespace Polymerium.App.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly ProfileManager _profileManger;
    private readonly NavigationService _navigationService;
    public IEnumerable<RecentModel> Recents { get; }
    public ICommand GotoInstanceViewCommand { get; }

    public HomeViewModel(ProfileManager profileManger, NavigationService navigationService)
    {
        _profileManger = profileManger;
        _navigationService = navigationService;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Recents = _profileManger.Managed
            .Select(x => (x.Value.Value.ExtractDateTime(TimelimeAction.Play), x.Value.Value, x.Key))
            .Where(x => x.Item1 != null)
            .OrderByDescending(x => x.Item1)
            .Select(x => new RecentModel(x.Item3, x.Item2,  GotoInstanceViewCommand));
    }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
            // 使用主要页面 DesktopView 来向下跳转到 InstanceView
            _navigationService.Navigate(typeof(InstanceView), key);
    }
}