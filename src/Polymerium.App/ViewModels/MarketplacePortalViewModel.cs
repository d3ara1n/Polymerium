using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
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
        ConfigurationService configurationService,
        NavigationService navigationService,
        DataService dataService)
    {
        _mojangLauncherService = mojangLauncherService;
        _configurationService = configurationService;
        _navigationService = navigationService;
        _dataService = dataService;
    }

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<MinecraftNewsModel>? News { get; set; }

    #endregion

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        var news = await _dataService.GetMinecraftNewsAsync();
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
    private readonly ConfigurationService _configurationService;
    private readonly NavigationService _navigationService;
    private readonly DataService _dataService;

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenReadMoreLink(MinecraftNewsModel? news)
    {
        if (news != null)
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(news.ReadMoreLink);
        // Process.Start(new ProcessStartInfo(news.ReadMoreLink.AbsoluteUri) { UseShellExecute = true });
    }

    [RelayCommand]
    private void GotoSearchView(string? query)
    {
        if (_configurationService.Value.ApplicationSuperPowerActivated)
            if (query == "/gamemode 1")
            {
                _navigationService.Navigate<UnknownView>();
                return;
            }

        _navigationService.Navigate<MarketplaceSearchView>(new MarketplaceSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}