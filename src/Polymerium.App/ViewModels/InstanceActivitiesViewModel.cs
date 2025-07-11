using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceActivitiesViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    DataService dataService,
    PersistenceService persistenceService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<InstanceActionModel>? PagedActionCollection { get; set; }

    [ObservableProperty]
    public partial LazyObject? PagedActions { get; set; }

    public string TotalPlayTime { get; set; } = TimeSpan.Zero.Humanize();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPlayTime))]
    public partial TimeSpan TotalPlayTimeRaw { get; set; }

    [ObservableProperty]
    public partial int SinceDayIndex { get; set; } = -1;

    #endregion

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        TotalPlayTimeRaw = persistenceService.GetTotalPlayTime(Basic.Key);
        SinceDayIndex = 0;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SinceDayIndex))
        {
            var since = DateTimeOffset.Now.AddDays(SinceDayIndex switch
            {
                0 => -1,
                1 => -7,
                2 => -30,
                3 => -365,
                _ => -114514
            });
            LoadPage(since);
        }
    }

    private void LoadPage(DateTimeOffset since)
    {
        var lazy = new LazyObject(async _ =>
        {
            var actions = persistenceService.GetLatestActions(Basic.Key, since);
            var tasks = actions
                       .Where(x => !(x.Old == null && x.New == null))
                       .Select(async x =>
                        {
                            Package? oldPackage = null;
                            Package? newPackage = null;
                            if (x.Old != null && PackageHelper.TryParse(x.Old, out var old))
                                oldPackage = await dataService.ResolvePackageAsync(old.Label,
                                                 old.Namespace,
                                                 old.Pid,
                                                 old.Vid,
                                                 Filter.None);

                            if (x.New != null && PackageHelper.TryParse(x.New, out var @new))
                                newPackage = await dataService.ResolvePackageAsync(@new.Label,
                                                 @new.Namespace,
                                                 @new.Pid,
                                                 @new.Vid,
                                                 Filter.None);

                            var thumbnail = newPackage?.Thumbnail != null || oldPackage?.Thumbnail != null
                                                ? await dataService.GetBitmapAsync(newPackage?.Thumbnail
                                                   ?? oldPackage?.Thumbnail
                                                   ?? throw new NotImplementedException())
                                                : AssetUriIndex.DIRT_IMAGE_BITMAP;

                            return new InstanceActionModel(newPackage?.ProjectId
                                                        ?? oldPackage?.ProjectId ?? string.Empty,
                                                           newPackage?.ProjectName
                                                        ?? oldPackage?.ProjectName ?? string.Empty,
                                                           oldPackage?.VersionId,
                                                           oldPackage?.VersionName,
                                                           newPackage?.VersionId,
                                                           newPackage?.VersionName,
                                                           thumbnail,
                                                           x.At,
                                                           false);
                        })
                       .ToArray();

            await Task.WhenAll(tasks);
            var results = tasks.Where(x => x.IsCompletedSuccessfully).Select(x => x.Result).ToList();
            return new InstanceActionCollection(results);
        });
        PagedActions = lazy;
    }
}