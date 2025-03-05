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
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel : ViewModelBase
{
    private CancellationToken? _token;

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
        _token = token;
        if (token.IsCancellationRequested)
            return;

        var game = await _prismLauncherService.GetGameVersionsAsync(token);
        var versions = game.Versions.Select(x => new GameVersionModel(x.Version, x.Type, x.ReleaseTime)).ToList();

        Versions = versions;
        VersionName = versions.MaxBy(x => x.ReleaseTimeRaw)?.Name ?? string.Empty;
        IsVersionLoaded = true;
    }

    private async Task LoadLoadersAsync(CancellationToken token)
    {
        // x.Requires.Any(y =>
        //                    y.Uid == PrismLauncherHelper.UID_INTERMEDIARY || (y.Uid == PrismLauncherHelper.UID_MINECRAFT &&
        //                                                                      (y.Equal == model.Inner.Metadata.Version ||
        //                                                                       y.Suggest == model.Inner.Metadata.Version)))
        // TODO: 从本地化资源中加载名字
        var loaders = ((string Identity, string Uid, string Display)[])
        [
            (LoaderHelper.LOADERID_FORGE, PrismLauncherService.UID_FORGE, "Forge"),
            (LoaderHelper.LOADERID_NEOFORGE, PrismLauncherService.UID_NEOFORGE, "NeoForge"),
            (LoaderHelper.LOADERID_FABRIC, PrismLauncherService.UID_FABRIC, "Fabric"),
            (LoaderHelper.LOADERID_QUILT, PrismLauncherService.UID_QUILT, "Quilt")
        ];
        var compounds = new List<LoaderCompoundModel>();
        foreach (var loader in loaders)
        {
            var component = await _prismLauncherService.GetVersionsAsync(loader.Uid, token);
            compounds.Add(new LoaderCompoundModel(loader.Identity,
                                                  loader.Display,
                                                  component
                                                     .Versions.OrderByDescending(x => x.ReleaseTime)
                                                     .Select(x => x.Version)
                                                     .ToList(),
                                                  component.Versions.FirstOrDefault(x => x.Recommended)?.Version));
        }
    }

    #region Commands

    [RelayCommand]
    private async Task PickVersion()
    {
        if (Versions != null)
        {
            var dialog = new VersionPickerDialog();
            dialog.SetItems(Versions);
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

        string? loader = null;
        // if (SelectedLoader != null)
        // {
        //     if (SelectedLoaderVersion != null)
        //     {
        //         loader = LoaderHelper.ToLurl(SelectedLoader.LoaderId, SelectedLoaderVersion);
        //     }
        //     else
        //     {
        //         // TODO: 使用 validation 解决
        //         _notificationService.PopMessage("Loader fallbacks to none due to no loader version selected",
        //                                         "Created with warnings",
        //                                         level: NotificationLevel.Warning);
        //     }
        // }

        _profileManager.Add(key,
                            new Profile(display,
                                        new Profile.Rice(null,
                                                         version,
                                                         loader,
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
    public partial IReadOnlyList<GameVersionModel>? Versions { get; set; }

    [ObservableProperty]
    public partial string VersionName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsVersionLoaded { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    #endregion
}