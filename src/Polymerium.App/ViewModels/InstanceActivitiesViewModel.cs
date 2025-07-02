using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
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

        await Task.Run(() =>
                       {
                           var actions = persistenceService.GetLatestActions(Basic.Key, 20);
                           var tasks = actions.Select(async x =>
                           {
                               if (x.Old != null && PackageHelper.TryParse(x.Old, out var old)) { }

                               if (x.New != null && PackageHelper.TryParse(x.New, out var @new)) { }
                           });
                       },
                       token);
    }

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<InstanceActionModel>? PagedCollection { get; set; }

    #endregion
}