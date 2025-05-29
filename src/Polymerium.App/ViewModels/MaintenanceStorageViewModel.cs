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

namespace Polymerium.App.ViewModels;

public partial class MaintenanceStorageViewModel(
    ProfileManager profileManager,
    NavigationService navigationService) : ViewModelBase
{
    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        await Task.Run(Calculate, token);

        await base.OnInitializedAsync(token);
    }

    private void Calculate()
    {
        (PackageSize, PackageCount) = CalculateDirectorySize(PathDef.Default.CachePackageDirectory);
        (LibrarySize, _) = CalculateDirectorySize(PathDef.Default.CacheLibraryDirectory);
        (AssetSize, _) = CalculateDirectorySize(PathDef.Default.CacheAssetDirectory);

        foreach (var (key, profile) in profileManager.Profiles)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            var (size, _) = CalculateDirectorySize(dir);
            Instances.Add(new StorageInstanceModel(key, profile.Name, size));
            InstanceSize += size;
        }

        CacheSize = PackageSize + LibrarySize + AssetSize;
        TotalSize = CacheSize + InstanceSize;
    }

    private static (ulong, ulong) CalculateDirectorySize(string path)
    {
        if (!Directory.Exists(path))
            return (0ul, 0ul);
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

    #region Commands

    [RelayCommand]
    private void PurgeCache() { }

    [RelayCommand]
    private void GotoInstance(StorageInstanceModel? model)
    {
        if (model != null)
            navigationService.Navigate<InstanceView>(new InstanceViewModel.CompositeParameter(model.Key,
                                                                            typeof(InstanceStorageView)));
    }

    #endregion

    #region Direct

    public ObservableCollection<StorageInstanceModel> Instances { get; } = [];

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

    #endregion
}