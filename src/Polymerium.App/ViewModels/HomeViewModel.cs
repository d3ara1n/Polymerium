﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Tasks;
using Polymerium.App.Views;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    public class HomeViewModel : ObservableObject
    {
        private readonly NavigationService _navigationService;
        private readonly ProfileManager _profileManger;
        private readonly ThumbnailSaver _thumbnailSaver;
        private readonly TaskService _taskService;

        public HomeViewModel(ProfileManager profileManger, NavigationService navigationService,
            ThumbnailSaver thumbnailSaver, TaskService taskService)
        {
            _profileManger = profileManger;
            _navigationService = navigationService;
            _thumbnailSaver = thumbnailSaver;
            _taskService = taskService;
            GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);
            OpenExternalUrlCommand = new RelayCommand<string>(OpenExternalUrl);

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
        public ICommand OpenExternalUrlCommand { get; }
        public ICommand GotoInstanceViewCommand { get; }

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

        public void QuerySubmitted(string query)
        {
            var start = "/update";
            if (query.StartsWith(start))
            {
                var key = query.Substring(start.Length);
                if (_profileManger.Managed.TryGetValue(key, out var profile))
                {
                    var task = new UpdateTask(key, profile.Value.Metadata);
                    Task.Run(() => UpdateInternal(profile.Value.Metadata));
                    _taskService.Enqueue(task);
                }
            }
        }

        private async Task UpdateInternal(Metadata metadata)
        {
            foreach (var layer in metadata.Layers)
            {
                // TODO
            }
        }
    }
}