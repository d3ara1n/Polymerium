using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels;

public partial class InstancePropertyViewModel : InstanceViewModelBase
{
    private ProfileGuard? _owned;

    public InstancePropertyViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        InstanceManager instanceManager,
        OverlayService overlayService,
        NotificationService notificationService) : base(bag, instanceManager, profileManager)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;

        SafeCode = Random.Shared.Next(1000, 9999).ToString();
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        base.OnUpdateModel(key, profile);

        NameOverwrite = profile.Name;
        ThumbnailOverwrite = Basic.Thumbnail;
    }

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
            _owned = guard;
        return base.OnInitializedAsync(token);
    }

    protected override async Task OnDeinitializeAsync(CancellationToken token)
    {
        if (_owned != null)
            await _owned.DisposeAsync();
        await base.OnDeinitializeAsync(token);
    }

    private void WriteIcon()
    {
        // NOTE: 如果监听 ThumbnailOverwrite 改变去写会导致死循环
        var path = ProfileHelper.PickIcon(Basic.Key);
        if (path != null)
        {
            ThumbnailOverwrite.Save(path);
        }
    }

    #region Injected

    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;

    #endregion

    #region Commands

    [RelayCommand]
    private void ResetInstance() { }

    [RelayCommand]
    private void DeleteInstance()
    {
        // TODO: 创建 @/_bomb_has_been_planted_ 文件，ProfileManager 在开局扫实例时检测到该文件就顺便删除
        ProfileManager.Remove(Basic.Key);
    }

    [RelayCommand]
    private void RemoveThumbnail()
    {
        ThumbnailOverwrite = AssetUriIndex.DIRT_IMAGE_BITMAP;
        WriteIcon();
    }

    [RelayCommand]
    private async Task SelectThumbnail()
    {
        var path = await _overlayService.RequestFileAsync("Select a image file", "Select thumbnail");
        if (path != null && FileHelper.IsBitmapFile(path))
        {
            ThumbnailOverwrite = new Bitmap(path);
            WriteIcon();
        }
        else
        {
            _notificationService.PopMessage("Selected file is not a valid image or no file selected.");
        }
    }

    [RelayCommand]
    private async Task RenameInstance()
    {
        var name = await _overlayService.RequestInputAsync("Get the instance a new name",
                                                           "Rename instance",
                                                           Basic.Name);
        if (name != null && _owned != null)
        {
            NameOverwrite = name;
            _owned.Value.Name = name;
        }
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public required partial Bitmap ThumbnailOverwrite { get; set; }

    [ObservableProperty]
    public required partial string NameOverwrite { get; set; }

    [ObservableProperty]
    public partial string SafeCode { get; set; }

    #endregion
}