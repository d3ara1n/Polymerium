﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels;

public partial class InstancePropertiesViewModel : InstanceViewModelBase
{
    private ProfileGuard? _owned;

    public InstancePropertiesViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        InstanceManager instanceManager,
        OverlayService overlayService,
        NotificationService notificationService,
        NavigationService navigationService,
        ConfigurationService configurationService) : base(bag, instanceManager, profileManager)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        _navigationService = navigationService;
        _configurationService = configurationService;

        SafeCode = Random.Shared.Next(1000, 9999).ToString();
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        base.OnUpdateModel(key, profile);

        NameOverwrite = profile.Name;
        ThumbnailOverwrite = Basic.Thumbnail;
    }

    private string AccessOverride<T>(string key)
    {
        if (_owned != null && _owned.Value.Overrides.TryGetValue(key, out var result) && result is T rv)
            return rv.ToString() ?? string.Empty;

        return string.Empty;
    }

    private void WriteOverride(string key, object? value)
    {
        if (_owned == null)
            return;
        if (value is not null and not "")
            _owned.Value.Overrides[key] = value;
        else
            _owned.Value.Overrides.Remove(key);
    }

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
            _owned = guard;

        #region Update Overrides & Perferences

        JavaHomeOverride = AccessOverride<string>(Profile.OVERRIDE_JAVA_HOME);
        JavaHomeWatermark = "Auto decide if unset";
        JavaMaxMemoryOverride = AccessOverride<uint>(Profile.OVERRIDE_JAVA_MAX_MEMORY);
        JavaMaxMemoryWatermark = _configurationService.Value.GameJavaMaxMemory.ToString();
        JavaAdditionalArgumentsOverride = AccessOverride<string>(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS);
        JavaAdditionalArgumentsWatermark = _configurationService.Value.GameJavaAdditionalArguments;
        WindowInitialHeightOverride = AccessOverride<uint>(Profile.OVERRIDE_WINDOW_HEIGHT);
        WindowInitialHeightWatermark = _configurationService.Value.GameWindowInitialHeight.ToString();
        WindowInitialWidthOverride = AccessOverride<uint>(Profile.OVERRIDE_WINDOW_WIDTH);
        WindowInitialWidthWatermark = _configurationService.Value.GameWindowInitialWidth.ToString();

        #endregion

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
            ThumbnailOverwrite.Save(path);
    }

    #region Injected

    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;
    private readonly NavigationService _navigationService;
    private readonly ConfigurationService _configurationService;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task PickFile(TextBox? box)
    {
        if (box != null)
        {
            var path = await _overlayService.RequestFileAsync("Pick a file like /bin/java.exe or /bin/javaw.exe",
                                                              "Select a Java executable");
            if (path != null && File.Exists(path))
            {
                var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                if (dir != null)
                    box.Text = dir;
            }
        }
    }

    [RelayCommand]
    private void ResetInstance() { }

    [RelayCommand]
    private void DeleteInstance()
    {
        var path = PathDef.Default.FileOfBomb(Basic.Key);
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, Basic.Key);
        ProfileManager.Remove(Basic.Key);

        if (_navigationService.CanGoBack)
            _navigationService.GoBack();
        else
            _navigationService.Navigate<LandingView>();
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
        if (path != null)
        {
            if (FileHelper.IsBitmapFile(path))
            {
                ThumbnailOverwrite = new Bitmap(path);
                WriteIcon();
            }
            else
            {
                _notificationService.PopMessage("Selected file is not a valid image or no file selected.");
            }
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

    [RelayCommand]
    private void GotoSettings() => _navigationService.Navigate<SettingsView>();

    #endregion

    #region Reactive

    [ObservableProperty]
    public required partial Bitmap ThumbnailOverwrite { get; set; }

    [ObservableProperty]
    public required partial string NameOverwrite { get; set; }

    [ObservableProperty]
    public partial string SafeCode { get; set; }

    #endregion

    #region Overrides

    [ObservableProperty]
    public partial string JavaHomeOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JavaHomeWatermark { get; set; } = string.Empty;

    partial void OnJavaHomeOverrideChanged(string? value)
    {
        WriteOverride(Profile.OVERRIDE_JAVA_HOME, value);
    }

    [ObservableProperty]
    public partial string JavaMaxMemoryOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JavaMaxMemoryWatermark { get; set; } = string.Empty;

    partial void OnJavaMaxMemoryOverrideChanged(string? value)
    {
        WriteOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                      value is not null && uint.TryParse(value, out var ui) ? ui : null);
    }

    [ObservableProperty]
    public partial string JavaAdditionalArgumentsOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JavaAdditionalArgumentsWatermark { get; set; } = string.Empty;

    partial void OnJavaAdditionalArgumentsOverrideChanged(string? value)
    {
        WriteOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS, value);
    }

    [ObservableProperty]
    public partial string WindowInitialHeightOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WindowInitialHeightWatermark { get; set; } = string.Empty;

    partial void OnWindowInitialHeightOverrideChanged(string? value)
    {
        WriteOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                      value is not null && uint.TryParse(value, out var ui) ? ui : null);
    }

    [ObservableProperty]
    public partial string WindowInitialWidthOverride { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WindowInitialWidthWatermark { get; set; } = string.Empty;

    partial void OnWindowInitialWidthOverrideChanged(string? value)
    {
        WriteOverride(Profile.OVERRIDE_WINDOW_WIDTH, value is not null && uint.TryParse(value, out var ui) ? ui : null);
    }

    #endregion
}