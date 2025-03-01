using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public partial class ExhibitionWelcomeViewModel : ViewModelBase
{
    #region Reactive

    [ObservableProperty]
    private IReadOnlyList<MinecraftNewsModel>? _news;

    #endregion

    public ExhibitionWelcomeViewModel(
        MojangLauncherService mojangLauncherService,
        IHttpClientFactory httpClientFactory,
        NavigationService navigationService)
    {
        _mojangLauncherService = mojangLauncherService;
        _httpClientFactory = httpClientFactory;
        _navigationService = navigationService;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        var news = await _mojangLauncherService.GetMinecraftNewsAsync();
        var models = news
                    .Entries.Take(16)
                    .Select(async (x, i) =>
                     {
                         var url = _mojangLauncherService.GetAbsoluteImageUrl(x.PlayPageImage.Url);
                         Bitmap? cover = null;
                         if (!Debugger.IsAttached || true)
                         {
                             using var client = _httpClientFactory.CreateClient();
                             var data = await client.GetByteArrayAsync(url, token);
                             cover = new Bitmap(new MemoryStream(data));
                         }
                         else
                         {
                             cover = AssetUriIndex.DIRT_IMAGE_BITMAP;
                         }

                         return new MinecraftNewsModel(cover, x.Category, x.Title, x.Text, x.ReadMoreLink, x.NewsType)
                         {
                             IsVeryBig = i == 0
                         };
                         ;
                     })
                    .ToArray();
        await Task.WhenAll(models);
        News = models.Select(x => x.Result).ToList();
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
        _navigationService.Navigate<ExhibitionSearchView>(new ExhibitionSearchViewModel.SearchArguments(query, null));
    }

    #endregion
}