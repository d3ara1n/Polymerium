using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel : ViewModelBase
{
    private IReadOnlyList<GameVersionModel>? _versions;

    public NewInstanceViewModel(
        OverlayService overlayService,
        PrismLauncherService prismLauncherService,
        ProfileManager profileManager,
        NavigationService navigationService,
        NotificationService notificationService)
    {
        _overlayService = overlayService;
        _prismLauncherService = prismLauncherService;
        _profileManager = profileManager;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        var index = await _prismLauncherService.GetGameVersionsAsync();
        var versions = index.Versions.Select(x => new GameVersionModel(x.Version, x.Type, x.ReleaseTime)).ToList();
        _versions = versions;
        Dispatcher.UIThread.Post(() =>
        {
            VersionName = versions.MaxBy(x => x.ReleaseTimeRaw)?.Name ?? string.Empty;
            IsVersionLoaded = true;
        });
    }

    #region Commands

    [RelayCommand]
    private async Task PickVersion()
    {
        if (_versions != null)
        {
            var dialog = new VersionPickerDialog();
            dialog.SetItems(_versions);
            if (await _overlayService.PopDialogAsync(dialog) && dialog.Result is GameVersionModel version)
                Dispatcher.UIThread.Post(() => VersionName = version.Name);
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        var version = VersionName;
        var display = string.IsNullOrEmpty(DisplayName) ? VersionName : DisplayName;

        var key = _profileManager.RequestKey(display);

        if (Thumbnail != null)
        {
            var stream = new MemoryStream();
            Thumbnail.Save(stream);
            stream.Position = 0;
            var extension = FileHelper.GuessBitmapExtension(stream);
            var iconPath = PathDef.Default.FileOfIcon(key.Key, extension);
            stream.Position = 0;
            if (!await FileHelper.TryWriteToFileAsync(iconPath, stream))
                _notificationService.PopMessage("Write icon file to the instance dir failed",
                                                level: NotificationLevel.Danger);
        }

        _profileManager.Add(key,
                            new Profile(display,
                                        new Profile.Rice(null,
                                                         version,
                                                         null,
                                                         new List<string>(),
                                                         new List<string>(),
                                                         new List<string>()),
                                        new Dictionary<string, object>()));


        _navigationService.Navigate<InstanceView>(key.Key);
    }

    #endregion

    #region Injected

    private readonly OverlayService _overlayService;
    private readonly PrismLauncherService _prismLauncherService;
    private readonly ProfileManager _profileManager;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    private string _versionName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isVersionLoaded;

    [ObservableProperty]
    private Bitmap? _thumbnail;

    #endregion
}