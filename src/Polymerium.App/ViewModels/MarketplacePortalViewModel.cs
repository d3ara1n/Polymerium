using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public partial class MarketplacePortalViewModel(
    MojangLauncherService mojangLauncherService,
    ConfigurationService configurationService,
    NavigationService navigationService,
    DataService dataService) : ViewModelBase
{
    #region Overrides

    protected override async Task OnInitializeAsync()
    {
        if (PageToken.IsCancellationRequested)
        {
            return;
        }

        var news = await dataService.GetMinecraftNewsAsync();
        var models = news
                    .Entries.Take(25)
                    .Select((x, i) =>
                     {
                         var url = mojangLauncherService.GetAbsoluteImageUrl(x.PlayPageImage.Url);

                         return new MinecraftNewsModel(url, x.Category, x.Title, x.Text, x.ReadMoreLink, x.NewsType)
                         {
                             IsVeryBig = i == 0
                         };
                     })
                    .ToArray();
        HeadNews = models.FirstOrDefault();
        TailNews = [.. models.Skip(1)];
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<MinecraftNewsModel>? TailNews { get; set; }

    [ObservableProperty]
    public partial MinecraftNewsModel? HeadNews { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenReadMoreLink(MinecraftNewsModel? news)
    {
        if (news != null)
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(news.ReadMoreLink);
        }
        // Process.Start(new ProcessStartInfo(news.ReadMoreLink.AbsoluteUri) { UseShellExecute = true });
    }

    [RelayCommand]
    private void GotoSearchView(string? query)
    {
        if (configurationService.Value.ApplicationSuperPowerActivated)
        {
            if (query == "/gamemode 1")
            {
                navigationService.Navigate<UnknownView>();
                return;
            }
        }

        navigationService.Navigate<MarketplaceSearchView>(new MarketplaceSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}
