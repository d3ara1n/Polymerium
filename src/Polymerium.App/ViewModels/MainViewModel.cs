using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly AccountManager _accountManager;
    private readonly ComponentManager _componentManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly DispatcherQueue _dispatcher;
    private readonly InstanceManager _instanceManager;
    private readonly ILogger _logger;
    private readonly MemoryStorage _memoryStorage;
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    private ContentControl? overlay;
    private NavigationItemModel? selectedPage;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IOverlayService overlayService,
        InstanceManager instanceManager,
        AccountManager accountManager,
        ConfigurationManager configurationManager,
        ComponentManager componentManager,
        NavigationService navigationService,
        MemoryStorage memoryStorage,
        ViewModelContext context
    )
    {
        _logger = logger;
        _overlayService = overlayService;
        _instanceManager = instanceManager;
        _componentManager = componentManager;
        _accountManager = accountManager;
        _navigationService = navigationService;
        _memoryStorage = memoryStorage;
        _configurationManager = configurationManager;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        OpenAddAccountWizardCommand = new RelayCommand(OpenAddAccountWizard);
        RemoveAccountCommand = new RelayCommand(RemoveAccount);
        Context = context;
        if (overlayService is WindowOverlayService windowOverlayService)
            windowOverlayService.Register(PushOverlay, PullOverlay);
        else
            throw new ArgumentNullException(nameof(overlayService));
        _memoryStorage.Instances.CollectionChanged += Instances_CollectionChanged;
        NavigationPages = new ObservableCollection<NavigationItemModel>(
            instanceManager
                .GetView()
                .Select(
                    it =>
                        new NavigationItemModel(
                            "\xF158",
                            it.Name,
                            typeof(InstanceView),
                            it,
                            it.ThumbnailFile
                        )
                )
        );
        NavigationPages.Insert(
            0,
            new NavigationItemModel(
                "\xEA8A",
                "Home",
                typeof(HomeView),
                thumbnailSource: "ms-appx:///Assets/Icons/icons8-home-page-48.png"
            )
        );
        NavigationPages.Add(
            new NavigationItemModel(
                "\xF8AA",
                "Add",
                typeof(NewInstanceView),
                thumbnailSource: "ms-appx:///Assets/Icons/icons8-add-new-48.png"
            )
        );
        SelectedPage = NavigationPages[0];
        NavigationPinnedPages = new NavigationItemModel[]
        {
            new(
                "\xE115",
                "Settings",
                typeof(SettingView),
                thumbnailSource: "ms-appx:///Assets/Icons/icons8-settings-48.png"
            )
        };
        SearchBarItems = new ObservableCollection<NavigationSearchBarItemModel>();
    }

    public ViewModelContext Context { get; }

    public ObservableCollection<NavigationItemModel> NavigationPages { get; }
    public NavigationItemModel[] NavigationPinnedPages { get; }

    public ObservableCollection<IGameAccount> LogonAccounts => _memoryStorage.Accounts;

    public NavigationItemModel? SelectedPage
    {
        get => selectedPage;
        set => SetProperty(ref selectedPage, value);
    }

    public ObservableCollection<NavigationSearchBarItemModel> SearchBarItems { get; }

    public ContentControl? Overlay
    {
        get => overlay;
        set => SetProperty(ref overlay, value);
    }

    public ICommand OpenAddAccountWizardCommand { get; }

    public ICommand RemoveAccountCommand { get; }

    private void PushOverlay(ContentControl content)
    {
        Overlay = content;
    }

    private ContentControl? PullOverlay()
    {
        // 安全的把所有权转移出去
        (var res, Overlay) = (Overlay, null);
        return res;
    }

    public void SetNavigateHandler(NavigateHandler handler)
    {
        _navigationService.Register(handler);
    }

    public void OnNavigatingTo(NavigationItemModel page)
    {
        Context.AssociatedInstance =
            page.GameInstance != null ? new GameInstanceModel(page.GameInstance) : null;
    }

    private void Instances_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dispatcher.TryEnqueue(
            DispatcherQueuePriority.Normal,
            () =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:

                    {
                        if (e.NewItems != null)
                            foreach (GameInstance instance in e.NewItems)
                                NavigationPages.Insert(
                                    NavigationPages.Count - 1,
                                    new NavigationItemModel(
                                        "\xF158",
                                        instance.Name,
                                        typeof(InstanceView),
                                        instance,
                                        instance.ThumbnailFile
                                    )
                                );
                    }
                        break;

                    case NotifyCollectionChangedAction.Remove:

                    {
                        if (e.OldItems != null)
                            foreach (GameInstance instance in e.OldItems)
                            {
                                var item = NavigationPages.FirstOrDefault(
                                    x => x.GameInstance?.Id == instance.Id
                                );
                                if (item != null)
                                    NavigationPages.Remove(item);
                                // TODO: 当前页面和该实例有关就关闭该页面
                            }
                    }
                        break;
                }
            }
        );
    }

    private void OpenAddAccountWizard()
    {
        _overlayService.Show(new AddAccountWizardDialog(_overlayService));
    }

    private void RemoveAccount()
    {
        if (Context.SelectedAccount != null)
        {
            if (Context.AssociatedInstance != null)
            {
                Context.AssociatedInstance.BoundAccountId = null;
            }
            else
            {
                Context.AccountShowcase = null;
                _configurationManager.Current.AccountShowcaseId = null;
            }

            _accountManager.RemoveAccount(Context.SelectedAccount);
            Context.SelectedAccount = null;
        }
    }

    public IEnumerable<GameInstance> GetViewOfInstance()
    {
        return _instanceManager.GetView();
    }

    public void GotoInstanceView(string id)
    {
        _navigationService.Navigate<InstanceView>(id);
    }
}