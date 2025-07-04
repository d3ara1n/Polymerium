using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        // 修改列表就应该改成分页，每次切换页的时候批量加载 [Project(OldVersion, NewVersion, Kind)]
        // PaginationPager 操作 Index，在 Index 改变时加载数据并切换 ItemsControl.ItemsSource，加个加载中动画
        // TODO: 使用分页控件来控制页数，在做出分页控件之前就只显示第一页吧
        await Task.Run(() => LoadPageAsync(0, 20), token);
    }

    private async Task LoadPageAsync(int skip, int take)
    {
        var actions = persistenceService.GetLatestActions(Basic.Key, skip, take);
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

                        return new InstanceActionModel(newPackage?.ProjectId ?? oldPackage?.ProjectId ?? string.Empty,
                                                       newPackage?.ProjectName
                                                    ?? oldPackage?.ProjectName ?? string.Empty,
                                                       oldPackage?.VersionId,
                                                       oldPackage?.VersionName,
                                                       newPackage?.VersionId,
                                                       newPackage?.VersionName,
                                                       thumbnail,
                                                       x.At,
                                                       (newPackage?.Kind ?? oldPackage?.Kind) is not ResourceKind
                                                          .Modpack
                                                    && false);
                    })
                   .ToArray();

        await Task.WhenAll(tasks);
        var results = tasks.Where(x => x.IsCompletedSuccessfully).Select(x => x.Result).ToList();
        PagedActionCollection = results;
    }

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<InstanceActionModel>? PagedActionCollection { get; set; }

    #endregion
}