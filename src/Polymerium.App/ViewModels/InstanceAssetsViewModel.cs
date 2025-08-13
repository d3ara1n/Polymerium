using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    public class InstanceAssetsViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager)
        : InstanceViewModelBase(bag, instanceManager, profileManager)
    {
        #region Reactive

        public ObservableCollection<ScreenshotGroupModel> Groups { get; } = [];

        #endregion

        #region Overrides

        protected override Task OnInitializeAsync(CancellationToken token) => Task.Run(LoadAsync, token);

        #endregion

        #region Other

        private Task LoadAsync()
        {
            var dir = new DirectoryInfo(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "screenshots"));
            if (!dir.Exists)
            {
                return Task.CompletedTask;
            }

            foreach (var files in dir
                                 .GetFiles("*.png", SearchOption.TopDirectoryOnly)
                                 .GroupBy(x => x.CreationTimeUtc.Date))
            {
                var group = new ScreenshotGroupModel(files.Key);
                foreach (var file in files)
                {
                    group.Screenshots.Add(new ScreenshotModel(new Uri(file.FullName, UriKind.Absolute),
                                                              file.CreationTimeUtc));
                }

                Groups.Add(group);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
