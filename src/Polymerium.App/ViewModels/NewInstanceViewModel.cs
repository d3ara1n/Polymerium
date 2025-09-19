using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel(
    OverlayService overlayService,
    ProfileManager profileManager,
    NavigationService navigationService,
    NotificationService notificationService,
    ImporterAgent importerAgent,
    DataService dataService,
    PersistenceService persistenceService) : ViewModelBase
{
    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        var game = await dataService.GetMinecraftVersionsAsync();
        var versions = game.Versions.Select(x => new GameVersionModel(x.Version, x.Type, x.ReleaseTime)).ToList();

        Versions = versions;
        var first = game.Versions.FirstOrDefault(x => x.Recommended);
        VersionName = first != null ? first.Version : string.Empty;
        IsVersionLoaded = true;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task PickVersion()
    {
        if (Versions != null)
        {
            var dialog = new GameVersionPickerDialog();
            dialog.SetItems(Versions);
            if (await overlayService.PopDialogAsync(dialog) && dialog.Result is GameVersionModel version)
            {
                Dispatcher.UIThread.Post(() => VersionName = version.Name);
            }
        }
    }

    [RelayCommand]
    private async Task OpenImportDialog()
    {
        var path = await overlayService.RequestFileAsync(Resources.NewInstanceView_RequestFilePrompt,
                                                         Resources.NewInstanceView_RequestFileTitle);
        if (path != null)
        {
            try
            {
                var fs = new FileStream(path, FileMode.Open);
                var ms = new MemoryStream();
                await fs.CopyToAsync(ms);
                fs.Close();
                ms.Position = 0;
                var pack = new CompressedProfilePack(ms);
                var container = await importerAgent.ImportAsync(pack);
                ImportedPack = new(path, pack, container);
                VersionName = container.Profile.Setup.Version;
                DisplayName = container.Profile.Name;
            }
            catch (Exception e)
            {
                notificationService.PopMessage(e, Resources.NewInstanceView_ImportDangerNotificationTitle);
            }
        }
    }

    [RelayCommand]
    private void GotoMarketplace() => navigationService.Navigate<MarketplacePortalView>();

    [RelayCommand]
    private void ClearImportedPack() => ImportedPack = null;

    [RelayCommand]
    private async Task CreateAsync()
    {
        var display = string.IsNullOrEmpty(DisplayName) ? VersionName : DisplayName;

        var key = profileManager.RequestKey(display);

        Profile profile;
        if (ImportedPack != null)
        {
            profile = ImportedPack.Container.Profile;
            await importerAgent.ExtractImportFilesAsync(key.Key, ImportedPack.Container, ImportedPack.Pack);
        }
        else
        {
            profile = new(display, new(null, VersionName, null, []), new Dictionary<string, object>());
        }

        if (Thumbnail != null)
        {
            try
            {
                using var stream = new MemoryStream();
                Thumbnail.Save(stream);
                stream.Position = 0;
                var extension = FileHelper.GuessBitmapExtension(stream);
                var iconPath = PathDef.Default.FileOfIcon(key.Key, extension);
                stream.Position = 0;
                var parent = Path.GetDirectoryName(iconPath);
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                var writer = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(writer).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => notificationService.PopMessage(ex,
                                                                              Resources
                                                                                 .NewInstanceView_IconSavingDangerNotificationTitle));
            }
        }

        profileManager.Add(key, profile);

        persistenceService.AppendAction(new(key.Key, PersistenceService.ActionKind.Install, null, ImportedPack?.Path));

        navigationService.Navigate<InstanceView>(key.Key);
    }

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

    [ObservableProperty]
    public partial FloatingImportedPackModel? ImportedPack { get; set; }

    #endregion
}
