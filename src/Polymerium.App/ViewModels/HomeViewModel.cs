using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly MemoryStorage _memoryStorage;
    private readonly NavigationService navigationService;

    public HomeViewModel(MemoryStorage memoryStorage, ViewModelContext context, NavigationService navigationService)
    {
        _memoryStorage = memoryStorage;
        Context = context;
        GotoRecentItemInstanceViewCommand = new RelayCommand<string>(GotoRecentItemInstanceView);
        RecentPlays = new ObservableCollection<RecentPlayedItemModel>(memoryStorage.Instances
            .Where(x => x.LastPlay.HasValue).OrderBy(x => DateTimeOffset.Now - x.LastPlay!.Value).Take(10).Select(x =>
                new RecentPlayedItemModel(x.Id, x.ThumbnailFile, x.Name,
                    x.LastPlay.HasValue ? x.LastPlay.Value.Humanize() : "从未",
                    GotoRecentItemInstanceViewCommand)));
        Tips = new[]
        {
            "比较抽象，没有提示"
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
}