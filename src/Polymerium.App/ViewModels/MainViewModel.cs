using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.DownloadSources.Models;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public class MainViewModel : ObservableRecipient, IRecipient<GameInstanceAddedMessage>, IRecipient<ApplicationAliveChangedMessage>
{
    private readonly ILogger _logger;
    private readonly IOverlayService _overlayService;
    private readonly AssetStorageService _storageService;
    private readonly DispatcherQueue _dispatcher;

    public MainViewModel(ILogger<MainViewModel> logger, IOverlayService overlayService, AssetStorageService storageService)
    {
        _logger = logger;
        _overlayService = overlayService;
        _storageService = storageService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        if (overlayService is WindowOverlayService windowOverlayService)
            windowOverlayService.Register(PushOverlay, PullOverlay);
        else
            throw new ApplicationException("Window overlay cannot get registered");

        NavigationPages = new ObservableCollection<NavigationItemModel>(storageService.GetViewOfInstances().Select(it => new NavigationItemModel("\xF158", it.Name, typeof(InstanceView), it, it.ThumbnailFile)));
        NavigationPages.Insert(0, new("\xEA8A", "Home", typeof(HomeView)));
        NavigationPages.Add(new("\xF8AA", "Add", typeof(NewInstanceView)));
        SelectedItem = NavigationPages[0];
        NavigationPinnedPages = new NavigationItemModel[] { new("\xE115", "Settings", typeof(HomeView)) };

        IsActive = true;
    }

    public ObservableCollection<NavigationItemModel> NavigationPages { get; }
    public NavigationItemModel[] NavigationPinnedPages { get; }
    public NavigationItemModel SelectedItem { get; set; }


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

    public void Receive(GameInstanceAddedMessage message)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () => NavigationPages.Insert(NavigationPages.Count - 1, new NavigationItemModel("\xF158", message.AddedInstance.Name, typeof(InstanceView), message.AddedInstance, message.AddedInstance.ThumbnailFile)));
    }

    public void Receive(ApplicationAliveChangedMessage message)
    {
        IsActive = false;
    }
}
