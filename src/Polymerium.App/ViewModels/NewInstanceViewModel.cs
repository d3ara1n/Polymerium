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
using Huskui.Avalonia.Models;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;

namespace Polymerium.App.ViewModels;

public partial class NewInstanceViewModel(
    OverlayService overlayService,
    ProfileManager profileManager,
    NavigationService navigationService,
    NotificationService notificationService,
    ImporterAgent importerAgent,
    DataService dataService) : ViewModelBase
{
    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        var game = await dataService.GetMinecraftVersionsAsync();
        var versions = game.Versions.Select(x => new GameVersionModel(x.Version, x.Type, x.ReleaseTime)).ToList();

        Versions = versions;
        VersionName = game.Versions.FirstOrDefault(x => x.Recommended)?.Version ?? string.Empty;
        IsVersionLoaded = true;
    }

    #region Commands

    [RelayCommand]
    private async Task PickVersion()
    {
        if (Versions != null)
        {
            var dialog = new VersionPickerDialog();
            dialog.SetItems(Versions);
            if (await overlayService.PopDialogAsync(dialog) && dialog.Result is GameVersionModel version)
                Dispatcher.UIThread.Post(() => VersionName = version.Name);
        }
    }

    [RelayCommand]
    private async Task OpenImportDialog()
    {
        var dialog = new FilePickerDialog { Message = "Select a file to import" };
        var path = await overlayService.RequestFileAsync("Select a file to import");
        if (path != null)
            try
            {
                var fs = new FileStream(path, FileMode.Open);
                var ms = new MemoryStream();
                await fs.CopyToAsync(ms);
                fs.Close();
                ms.Position = 0;
                var pack = new CompressedProfilePack(ms);
                var container = await importerAgent.ImportAsync(pack);
                ImportedPack = new FloatingImportedPackModel(pack, container);
                VersionName = container.Profile.Setup.Version;
                DisplayName = container.Profile.Name;
            }
            catch (Exception e)
            {
                notificationService.PopMessage(e, "Import failed");
            }
    }

    [RelayCommand]
    private void GotoMarketplace()
    {
        navigationService.Navigate<MarketplacePortalView>();
    }

    [RelayCommand]
    private void ClearImportedPack()
    {
        ImportedPack = null;
    }

    [RelayCommand]
    private async Task Create()
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
            profile = new Profile(display,
                                  new Profile.Rice(null, VersionName, null, []),
                                  new Dictionary<string, object>());
        }

        if (Thumbnail != null)
        {
            var stream = new MemoryStream();
            Thumbnail.Save(stream);
            stream.Position = 0;
            var extension = FileHelper.GuessBitmapExtension(stream);
            var iconPath = PathDef.Default.FileOfIcon(key.Key, extension);
            stream.Position = 0;
            if (!await FileHelper.TryWriteToFileAsync(iconPath, stream))
                notificationService.PopMessage("Write icon file to the instance dir failed",
                                               level: NotificationLevel.Danger);
        }

        profileManager.Add(key, profile);

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