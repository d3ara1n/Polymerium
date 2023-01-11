using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly IOverlayService _overlayService;
    private readonly AssetStorageService _storageService;

    private ContentControl overlay;

    public MainViewModel(ILogger<MainViewModel> logger, IOverlayService overlayService, AssetStorageService storageService)
    {
        _logger = logger;
        _overlayService = overlayService;
        _storageService = storageService;
        if (overlayService is WindowOverlayService windowOverlayService)
            windowOverlayService.Register(PushOverlay, PullOverlay);
        else
            throw new ApplicationException("Window overlay cannot get registered");

        NavigationPages = new ObservableCollection<NavigationItemModel>
        {
            new("\xEA8A", "Home", typeof(HomeView)), new("\xF8AA", "Add", typeof(NewInstanceView))
        };
        SelectedItem = NavigationPages[0];
        NavigationPinnedPages = new NavigationItemModel[] { new("\xE115", "Settings", typeof(HomeView)) };
    }

    public ObservableCollection<NavigationItemModel> NavigationPages { get; }
    public NavigationItemModel[] NavigationPinnedPages { get; }
    public NavigationItemModel SelectedItem { get; set; }

    public ContentControl Overlay
    {
        get => overlay;
        set => SetProperty(ref overlay, value);
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
}
