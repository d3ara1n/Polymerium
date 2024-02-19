using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    public class HomeViewModel : ObservableObject
    {
        private readonly NavigationService _navigationService;
        private readonly ProfileManager _profileManger;
        private readonly ThumbnailSaver _thumbnailSaver;

        public HomeViewModel(ProfileManager profileManger, NavigationService navigationService,
            ThumbnailSaver thumbnailSaver)
        {
            _profileManger = profileManger;
            _navigationService = navigationService;
            _thumbnailSaver = thumbnailSaver;
            GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

            Recents = _profileManger.Managed
                .Select(x => (
                    x.Value.Value.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play),
                    x.Value.Value, x.Key))
                .Where(x => x.Item1 != null)
                .OrderByDescending(x => x.Item1)
                .Select(x => new RecentModel(x.Item3, x.Item2, thumbnailSaver.Get(x.Item3), GotoInstanceViewCommand))
                .ToList();
        }

        public IList<RecentModel> Recents { get; }
        public ICommand GotoInstanceViewCommand { get; }

        private void GotoInstanceView(string? key)
        {
            if (key != null)
                // 使用主要页面 DesktopView 来向下跳转到 InstanceView
            {
                _navigationService.Navigate(typeof(InstanceView), key);
            }
        }
    }
}