using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public partial class LandingViewModel(
    NavigationService navigationService,
    DataService dataService,
    MojangLauncherService mojangLauncherService,
    PersistenceService persistenceService) : ViewModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial LazyObject? MinecraftNews { get; set; }

    [ObservableProperty]
    public partial double TotalPlayHours { get; set; }

    [ObservableProperty]
    public partial int ActiveDays { get; set; }

    [ObservableProperty]
    public partial int TotalSessions { get; set; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync()
    {
        // This page is always the root page
        navigationService.ClearHistory();


        MinecraftNews = new(async _ =>
        {
            var news = await dataService.GetMinecraftReleasePatchesAsync();
            var models = news
                        .Entries.Take(24)
                        .Select((x, i) =>
                         {
                             var url = mojangLauncherService.GetAbsoluteImageUrl(x.Image.Url);

                             return new MinecraftReleasePatchModel(url, x.Type, x.Title, x.ShortText, x.Date);
                         })
                        .ToList();
            return models;
        });

        TotalPlayHours = persistenceService.GetTotalPlayTime().TotalHours;
        ActiveDays = persistenceService.GetActiveDays();
        TotalSessions = persistenceService.GetSessionCount();
    }

    #endregion
}
