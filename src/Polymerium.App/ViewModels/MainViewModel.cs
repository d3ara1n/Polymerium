using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Abstractions.DownloadSources.Models;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels;

public class MainViewModel : ObservableRecipient, IRecipient<GameInstanceAddedMessage>, IRecipient<ApplicationAliveChangedMessage>
{
    private readonly ILogger _logger;
    private readonly IOverlayService _overlayService;
    private readonly NavigationService _navigationService;
    private readonly InstanceManager _instanceManager;
    private readonly DispatcherQueue _dispatcher;

    public MainViewModel(ILogger<MainViewModel> logger, IOverlayService overlayService, InstanceManager instanceManager, NavigationService navigationService)
    {
        _logger = logger;
        _overlayService = overlayService;
        _instanceManager = instanceManager;
        _navigationService = navigationService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        if (overlayService is WindowOverlayService windowOverlayService)
            windowOverlayService.Register(PushOverlay, PullOverlay);
        else
            throw new ArgumentNullException(nameof(overlayService));
        IsActive = true;
        NavigationPages = new ObservableCollection<NavigationItemModel>(instanceManager.GetView().Select(it => new NavigationItemModel("\xF158", it.Name, typeof(InstanceView), it, it.ThumbnailFile)));
        NavigationPages.Insert(0, new("\xEA8A", "Home", typeof(HomeView)));
        NavigationPages.Add(new("\xF8AA", "Add", typeof(NewInstanceView)));
        SelectedPage = NavigationPages[0];
        NavigationPinnedPages = new NavigationItemModel[] { new("\xE115", "Settings", typeof(SettingView)) };
        LogonAccounts = new ObservableCollection<AccountItemModel>()
        {
            new()
            {
                Inner = new OfflineAccount()
                {
                    PlayerName = "Dearain",
                    GeneratedId = "123456"
                },
                AvatarSource = "ms-appx:///Assets/Placeholders/default_avatar_alt_face.png"
            },
            new()
            {
                Inner = new OfflineAccount()
                {
                    PlayerName = "Herobrine",
                    GeneratedId = "654321"
                },
                AvatarSource = "ms-appx:///Assets/Placeholders/default_avatar_face.png"
            }
        };
        LogonAccount = LogonAccounts[0];
        AccountShowcase = LogonAccount;
    }

    public ObservableCollection<NavigationItemModel> NavigationPages { get; }
    public NavigationItemModel[] NavigationPinnedPages { get; }
    private NavigationItemModel selectedPage;
    public NavigationItemModel SelectedPage { get => selectedPage; set => SetProperty(ref selectedPage, value); }
    public ObservableCollection<AccountItemModel> LogonAccounts { get; set; }
    private AccountItemModel logonAccount;
    public AccountItemModel LogonAccount { get => logonAccount; set => SetProperty(ref logonAccount, value); }
    public AccountItemModel AccountShowcase { get; set; }


    private ContentControl overlay;
    public ContentControl Overlay
    {
        get => overlay;
        set => SetProperty(ref overlay, value);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        StrongReferenceMessenger.Default.RegisterAll(this);
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        StrongReferenceMessenger.Default.UnregisterAll(this);
    }

    private void PushOverlay(ContentControl content)
    {
        Overlay = content;
    }

    private ContentControl PullOverlay()
    {
        // 安全的把所有权转移出去
        (var res, Overlay) = (Overlay, null);
        return res;
    }

    public void SwitchAccountTo(AccountItemModel model)
    {
        if (SelectedPage.GameInstance != null)
        {
            SelectedPage.GameInstance.BoundAccountId = model.Inner.Id;
        }
        else
        {
            AccountShowcase = model;
        }
        LogonAccount = model;
    }

    public void SetNavigateHandler(NavigateHandler handler)
    {
        _navigationService.Register(handler);
    }

    public void OnNavigatedTo(NavigationItemModel page)
    {
        if (page.GameInstance != null)
        {
            var account = LogonAccounts.FirstOrDefault(x => x.Inner.Id == page.GameInstance.BoundAccountId);
            if (account != null)
                LogonAccount = account;
        }
        else
        {
            LogonAccount = AccountShowcase;
        }
    }

    public void Receive(GameInstanceAddedMessage message)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () => NavigationPages.Insert(NavigationPages.Count - 1, new NavigationItemModel("\xF158", message.AddedInstance.Name, typeof(InstanceView), message.AddedInstance, message.AddedInstance.ThumbnailFile)));
    }

    public void Receive(ApplicationAliveChangedMessage message)
    {
        IsActive = false;
    }
}
