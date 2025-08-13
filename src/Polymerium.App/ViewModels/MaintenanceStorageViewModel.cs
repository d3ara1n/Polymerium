using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    public partial class MaintenanceStorageViewModel(
        ProfileManager profileManager,
        NavigationService navigationService,
        OverlayService overlayService,
        NotificationService notificationService) : ViewModelBase
    {
        #region Direct

        public ObservableCollection<StorageInstanceModel> Instances { get; } = [];

        #endregion

        protected override async Task OnInitializeAsync(CancellationToken token)
        {
            await Task.Run(Calculate, token);

            await base.OnInitializeAsync(token);
        }

        #region Other

        private void Calculate()
        {
            (PackageSize, PackageCount) = CalculateDirectorySize(PathDef.Default.CachePackageDirectory);
            (LibrarySize, _) = CalculateDirectorySize(PathDef.Default.CacheLibraryDirectory);
            (AssetSize, _) = CalculateDirectorySize(PathDef.Default.CacheAssetDirectory);
            (RuntimeSize, _) = CalculateDirectorySize(PathDef.Default.CacheRuntimeDirectory);

            foreach (var (key, profile) in profileManager.Profiles)
            {
                var dir = PathDef.Default.DirectoryOfHome(key);
                var (size, _) = CalculateDirectorySize(dir);
                Instances.Add(new StorageInstanceModel(key, profile.Name, size));
                InstanceSize += size;
            }

            CacheSize = PackageSize + LibrarySize + AssetSize + RuntimeSize;
            TotalSize = CacheSize + InstanceSize;
        }

        private static (ulong, ulong) CalculateDirectorySize(string path)
        {
            if (!Directory.Exists(path))
            {
                return (0ul, 0ul);
            }

            var directory = new DirectoryInfo(path);
            var (size, count) = directory
                               .GetFiles()
                               .Aggregate((0ul, 0ul),
                                          (current, file) => (current.Item1 + (ulong)file.Length, current.Item2 + 1));
            return directory
                  .GetDirectories()
                  .Aggregate((size, count),
                             (current, dir) =>
                             {
                                 var (subSize, subCount) = CalculateDirectorySize(dir.FullName);
                                 return (current.size + subSize, current.count + subCount);
                             });
        }

        private static void PurgeDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task PurgeCache()
        {
            if (await overlayService.RequestConfirmationAsync("Are you sure you want to purge the cache?"))
            {
                try
                {
                    PurgeDirectory(PathDef.Default.CacheDirectory);
                }
                catch (Exception ex)
                {
                    notificationService.PopMessage(ex, "Failed to purge cache");
                }

                Calculate();
            }
        }

        [RelayCommand]
        private void GotoInstance(StorageInstanceModel? model)
        {
            if (model != null)
            {
                navigationService.Navigate<InstanceView>(new InstanceViewModel.CompositeParameter(model.Key,
                                                             typeof(InstanceStorageView)));
            }
        }

        #endregion

        #region Reactive

        [ObservableProperty]
        public partial IReadOnlyList<ISeries> TotalSeries { get; set; }

        [ObservableProperty]
        public partial ulong TotalSize { get; set; }

        [ObservableProperty]
        public partial ulong CacheSize { get; set; }

        [ObservableProperty]
        public partial ulong InstanceSize { get; set; }

        [ObservableProperty]
        public partial ulong PackageSize { get; set; }

        [ObservableProperty]
        public partial ulong PackageCount { get; set; }

        [ObservableProperty]
        public partial ulong LibrarySize { get; set; }

        [ObservableProperty]
        public partial ulong AssetSize { get; set; }

        [ObservableProperty]
        public partial ulong RuntimeSize { get; set; }

        #endregion
    }
}
