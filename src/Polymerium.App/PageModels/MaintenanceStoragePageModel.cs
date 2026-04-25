using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Pages;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.PageModels;

public partial class MaintenanceStoragePageModel(
    ProfileManager profileManager,
    NavigationService navigationService,
    OverlayService overlayService,
    NotificationService notificationService
) : ViewModelBase
{
    #region Direct

    public ObservableCollection<StorageInstanceModel> Instances { get; } = [];

    #endregion

    protected override async Task OnInitializeAsync(CancellationToken token) =>
        await Task.Run(Calculate, token);

    #region Other

    private void Calculate()
    {
        (PackageSize, PackageCount) = FileHelper.CalculateDirectorySize(
            PathDef.Default.CachePackageDirectory
        );
        (LibrarySize, _) = FileHelper.CalculateDirectorySize(PathDef.Default.CacheLibraryDirectory);
        (AssetSize, _) = FileHelper.CalculateDirectorySize(PathDef.Default.CacheAssetDirectory);
        (RuntimeSize, _) = FileHelper.CalculateDirectorySize(PathDef.Default.CacheRuntimeDirectory);

        foreach (var (key, profile) in profileManager.Profiles)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            var (size, _) = FileHelper.CalculateDirectorySize(dir);
            Instances.Add(new(key, profile.Name, size));
            InstanceSize += size;
        }

        CacheSize = PackageSize + LibrarySize + AssetSize + RuntimeSize;
        TotalSize = CacheSize + InstanceSize;
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
        if (
            await overlayService.RequestConfirmationAsync(
                Resources.MaintenanceStoragePage_PurgeCacheConfirmationMessage
            )
        )
        {
            try
            {
                PurgeDirectory(PathDef.Default.CacheDirectory);
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(
                    ex,
                    Resources.MaintenanceStoragePage_PurgeCacheDangerNotificationTitle
                );
            }

            Calculate();
        }
    }

    [RelayCommand]
    private void GotoInstance(StorageInstanceModel? model)
    {
        if (model != null)
        {
            navigationService.Navigate<InstancePage>(
                new InstancePageModel.CompositeParameter(model.Key, typeof(InstanceStoragePage))
            );
        }
    }

    #endregion

    #region Reactive

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
