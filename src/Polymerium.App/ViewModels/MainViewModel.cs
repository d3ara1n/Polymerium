using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Extensions;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Polymerium.App.ViewModels;

public sealed partial class MainViewModel : ObservableRecipient, IDisposable
{
    private readonly ILogger _logger;
    private readonly IOverlayService _overlayService;
    private readonly NavigationService _navigationService;
    private readonly InstanceManager _instanceManager;
    private readonly AccountManager _accountManager;
    private readonly DispatcherQueue _dispatcher;
    private readonly MemoryStorage _memoryStorage;
    private readonly ConfigurationManager _configurationManager;

    public MainViewModel(ILogger<MainViewModel> logger, IOverlayService overlayService, InstanceManager instanceManager, AccountManager accountManager, ConfigurationManager configurationManager, NavigationService navigationService, MemoryStorage memoryStorage)
    {
        _logger = logger;
        _overlayService = overlayService;
        _instanceManager = instanceManager;
        _accountManager = accountManager;
        _navigationService = navigationService;
        _memoryStorage = memoryStorage;
        _configurationManager = configurationManager;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        if (overlayService is WindowOverlayService windowOverlayService)
            windowOverlayService.Register(PushOverlay, PullOverlay);
        else
            throw new ArgumentNullException(nameof(overlayService));
        IsActive = true;
        _memoryStorage.Instances.CollectionChanged += Instances_CollectionChanged;
        _memoryStorage.Accounts.CollectionChanged += Accounts_CollectionChanged;
        NavigationPages = new ObservableCollection<NavigationItemModel>(instanceManager.GetView().Select(it => new NavigationItemModel("\xF158", it.Name, typeof(InstanceView), it, it.ThumbnailFile)));
        NavigationPages.Insert(0, new("\xEA8A", "Home", typeof(HomeView), thumbnailSource: "ms-appx:///Assets/Icons/icons8-home-page-48.png"));
        NavigationPages.Add(new("\xF8AA", "Add", typeof(NewInstanceView), thumbnailSource: "ms-appx:///Assets/Icons/icons8-add-new-48.png"));
        SelectedPage = NavigationPages[0];
        NavigationPinnedPages = new NavigationItemModel[] { new("\xE115", "Settings", typeof(SettingView), thumbnailSource: "ms-appx:///Assets/Icons/icons8-settings-48.png") };
        LogonAccounts = new ObservableCollection<AccountItemModel>();
        if (_accountManager.TryFindById(_configurationManager.Current.AccountShowcaseId, out var account))
        {
            AccountShowcase = account.ToModel();
        }
    }

    public ObservableCollection<NavigationItemModel> NavigationPages { get; }
    public NavigationItemModel[] NavigationPinnedPages { get; }
    private NavigationItemModel selectedPage;
    public NavigationItemModel SelectedPage { get => selectedPage; set => SetProperty(ref selectedPage, value); }
    public ObservableCollection<AccountItemModel> LogonAccounts { get; set; }
    public AccountItemModel AccountShowcase { get; set; }
    public MemoryStorage MemoryStorage => _memoryStorage;

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
            _configurationManager.Current.AccountShowcaseId = model.Inner.Id;
            AccountShowcase = model;
        }
        _memoryStorage.SelectedAccount = model;
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
                _memoryStorage.SelectedAccount = account;
        }
        else
        {
            _memoryStorage.SelectedAccount = AccountShowcase;
        }
    }

    private void Instances_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (GameInstance instance in e.NewItems)
                        {
                            NavigationPages.Insert(NavigationPages.Count - 1, new NavigationItemModel("\xF158", instance.Name, typeof(InstanceView), instance, instance.ThumbnailFile));
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (GameInstance instance in e.OldItems)
                        {
                            var item = NavigationPages.FirstOrDefault(x => x.GameInstance.Id == instance.Id);
                            if (item != null)
                            {
                                NavigationPages.Remove(item);
                                // TODO: 当前页面和该实例有关就关闭该页面
                            }
                        }
                    }
                    break;
            }
        });
    }

    private void Accounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    foreach (IGameAccount a in e.NewItems)
                    {
                        var model = new AccountItemModel()
                        {
                            Inner = a,
                            AvatarFaceSource = "ms-appx:///Assets/Placeholders/default_avatar_face.png"
                        };
                        LogonAccounts.Add(model);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                {
                    foreach (IGameAccount r in e.OldItems)
                    {
                        var model = LogonAccounts.FirstOrDefault(x => x.Inner.Id == r.Id);
                        if (model != null)
                        {
                            LogonAccounts.Remove(model);
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                LogonAccounts.Clear();
                break;
        }
    }

    [RelayCommand]
    public void OpenAddAccountWizard()
    {
        _overlayService.Show(new AddAccountWizardDialog(_overlayService));
    }

    public void FillMenuItems()
    {
        // FlyoutSubMenu 不能绑定 ItemSource 就真的**
        foreach (var model in _memoryStorage.Accounts)
        {
            LogonAccounts.Add(model.ToModel());
        }
    }

    public void Dispose()
    {
        IsActive = false;
    }
}