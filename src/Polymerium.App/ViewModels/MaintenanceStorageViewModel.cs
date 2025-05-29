using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using Polymerium.App.Facilities;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels;

public partial class MaintenanceStorageViewModel : ViewModelBase
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

        CacheSize = PackageSize + LibrarySize + AssetSize;
        TotalSize = PackageSize + LibrarySize + AssetSize;
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

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<ISeries> TotalSeries { get; set; }

    [ObservableProperty]
    public partial ulong TotalSize { get; set; }

    [ObservableProperty]
    public partial ulong CacheSize { get; set; }

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