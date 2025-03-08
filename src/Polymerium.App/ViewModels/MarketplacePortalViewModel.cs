using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public partial class MarketplacePortalViewModel : ViewModelBase
{
    public MarketplacePortalViewModel(
        MojangLauncherService mojangLauncherService,
        IHttpClientFactory httpClientFactory,
        NavigationService navigationService)
    {
        _mojangLauncherService = mojangLauncherService;
        _httpClientFactory = httpClientFactory;
        _navigationService = navigationService;
    }

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<MinecraftNewsModel>? News { get; set; }

    #endregion

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        var news = await _mojangLauncherService.GetMinecraftNewsAsync();
        var models = news
                    .Entries.Take(16)
                    .Select((x, i) =>
                     {
                         var url = _mojangLauncherService.GetAbsoluteImageUrl(x.PlayPageImage.Url);

                         return new MinecraftNewsModel(url, x.Category, x.Title, x.Text, x.ReadMoreLink, x.NewsType)
                         {
                             IsVeryBig = i == 0
                         };
                     });
        News = models.ToList();
    }

    #region Injected

    private readonly MojangLauncherService _mojangLauncherService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NavigationService _navigationService;

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenReadMoreLink(MinecraftNewsModel? news)
    {
        if (news != null)
            Process.Start(new ProcessStartInfo(news.ReadMoreLink.AbsoluteUri) { UseShellExecute = true });
    }

    [RelayCommand]
    private void GotoSearchView(string? query)
    {
        _navigationService.Navigate<MarketplaceSearchView>(new MarketplaceSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}