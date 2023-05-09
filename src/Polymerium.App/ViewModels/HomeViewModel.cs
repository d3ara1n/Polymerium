using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json.Linq;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Wupoo;

namespace Polymerium.App.ViewModels;

public class HomeViewModel : ObservableObject
{
#pragma warning disable S1075 // URIs should not be hardcoded
    private const string LAUNCH_CONTENT_URL_BASE = "https://launchercontent.mojang.com";
#pragma warning restore S1075 // URIs should not be hardcoded
    private readonly NavigationService navigationService;
    private readonly IMemoryCache _cache;

    public HomeViewModel(
        MemoryStorage memoryStorage,
        ViewModelContext context,
        NavigationService navigationService,
        IMemoryCache cache
    )
    {
        _cache = cache;
        Context = context;
        GotoRecentItemInstanceViewCommand = new RelayCommand<string>(GotoRecentItemInstanceView);
        RecentPlays = new ObservableCollection<RecentPlayedItemModel>(
            memoryStorage.Instances
                .Where(x => x.LastPlay.HasValue)
                .OrderBy(x => DateTimeOffset.Now - x.LastPlay!.Value)
                .Take(10)
                .Select(
                    x =>
                        new RecentPlayedItemModel(
                            x.Id,
                            x.ThumbnailFile?.AbsoluteUri,
                            x.Name,
                            x.LastPlay,
                            GotoRecentItemInstanceViewCommand
                        )
                )
        );
        Tips = new[]
        {
            "比较抽象，没有提示",
            "锁定元数据的实例只能在解锁之前只能通过资产文件管理中手动添加模组、资源包文件",
            "离线账号选项会在添加微软账号之后出现",
            "直接或通过搜索条进入下载中心下载资源会在后续操作中要求选择实例",
            "下载中心刷不出来内容？试试开代理。\n没开？那开一个试试"
        };
        if (Context.SelectedAccount == null)
            Tip = "你还没有设置账号，\n点击左侧导航栏头像添加";
        else
            Tip = Tips[Random.Shared.Next(Tips.Length)];
        this.navigationService = navigationService;
    }

    public ViewModelContext Context { get; }

    public string[] Tips { get; }
    public string Tip { get; }

    public ICommand GotoRecentItemInstanceViewCommand { get; }

    public ObservableCollection<RecentPlayedItemModel> RecentPlays { get; }

    public void GotoRecentItemInstanceView(string? instanceId)
    {
        navigationService.Navigate<InstanceView>(instanceId);
    }

    public async Task LoadNewsAsync(Action<HomeNewsItemModel?> callback)
    {
        var urlBase = new Uri(LAUNCH_CONTENT_URL_BASE);
        var newsUrl = new Uri(urlBase, "news.json");
        var results = await _cache.GetOrCreateAsync(
            "HomeNews",
            async entry =>
            {
                var list = new List<HomeNewsItemModel>();
                await Wapoo
                    .Wohoo(newsUrl.AbsoluteUri)
                    .ForJsonResult<JObject>(x =>
                    {
                        if (x.ContainsKey("entries"))
                        {
                            var entries = x.Value<JArray>("entries");
                            foreach (var entry in entries!.Values<JObject>())
                            {
                                var category = string.Empty;
                                if (entry!.ContainsKey("category"))
                                    category = entry.Value<string>("category");
                                if (category != "Minecraft: Java Edition")
                                    continue;
                                var title = string.Empty;
                                if (entry.ContainsKey("title"))
                                    title = entry.Value<string>("title");
                                var text = string.Empty;
                                if (entry.ContainsKey("text"))
                                    text = entry.Value<string>("text");
                                var readMore = string.Empty;
                                if (entry.ContainsKey("readMoreLink"))
                                    readMore = entry.Value<string>("readMoreLink");
                                var imageSource = string.Empty;
                                if (entry.ContainsKey("playPageImage"))
                                {
                                    var playPageImage = entry.Value<JObject>("playPageImage");
                                    if (playPageImage!.ContainsKey("url"))
                                        imageSource = playPageImage.Value<string>("url");
                                }

                                var date = string.Empty;
                                if (entry.ContainsKey("date"))
                                    date = entry.Value<string>("date");

                                var model = new HomeNewsItemModel(
                                    title!,
                                    text!,
                                    DateTimeOffset.Parse(date!),
                                    !string.IsNullOrEmpty(readMore) ? new Uri(readMore) : null,
                                    new Uri(urlBase, imageSource).AbsoluteUri
                                );
                                list.Add(model);
                            }
                        }
                    })
                    .FetchAsync();
                if (list.Count > 0)
                {
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                }
                else
                {
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(1));
                }
                return list;
            }
        );
        foreach (var item in results!)
            callback(item);
        callback(null);
    }
}
