using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    public partial class InstanceAssetsViewModel(
        ViewBag bag,
        InstanceManager instanceManager,
        ProfileManager profileManager,
        OverlayService overlayService) : InstanceViewModelBase(bag, instanceManager, profileManager)
    {
        #region Reactive

        public ObservableCollection<ScreenshotGroupModel> Groups { get; } = [];

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
                    group.Screenshots.Add(new(new(file.FullName, UriKind.Absolute), file.CreationTimeUtc));
                }

                Groups.Add(group);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
