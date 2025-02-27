using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public partial class ExhibitionWelcomeViewModel : ViewModelBase
{
    #region Injected

    private readonly MojangLauncherService _mojangLauncherService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExhibitionWelcomeViewModel(MojangLauncherService mojangLauncherService, IHttpClientFactory httpClientFactory)
    {
        _mojangLauncherService = mojangLauncherService;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        var news = await _mojangLauncherService.GetMinecraftNewsAsync();
        var models = news
                    .Entries.Take(16)
                    .Select(async x =>
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

                         return new MinecraftNewsModel(cover, x.Category, x.Title, x.Text, x.ReadMoreLink, x.NewsType);
                     })
                    .ToArray();
        await Task.WhenAll(models);
        News = models.Select(x => x.Result).ToList();
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    private IReadOnlyList<MinecraftNewsModel>? _news;

    #endregion
}