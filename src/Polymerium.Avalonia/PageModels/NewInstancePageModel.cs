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
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Importers;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class NewInstancePageModel(
    IViewContext<string> context,
    OverlayService overlayService,
    ProfileManager profileManager,
    NavigationService navigationService,
    NotificationService notificationService,
    ImporterAgent importerAgent,
    InstanceModpackService modpacks,
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

        if (context.Parameter is { } path)
        {
            await TryImportFromFileAsync(path);
        }

        var game = await dataService.GetMinecraftVersionsAsync();
        var versions = game.Versions.Select(x => new GameVersionModel(x.Version, MinecraftReleaseTypeHelper.FromComponentType(x.Type), x.ReleaseTime)).ToList();

        Versions = versions;
        var first = game.Versions.FirstOrDefault(x => x.Recommended);
        VersionName = first != null ? first.Version : string.Empty;
        IsVersionLoaded = true;
    }

    #endregion

    #region Other

    private async Task TryImportFromFileAsync(string path)
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

            if (container.LaunchDiagnostics is { Count: > 0 } diagnostics)
            {
                notificationService.PopMessage(string.Join(Environment.NewLine, diagnostics.Select(x => x.Message)),
                                               LanguageManager
                                                  .Instance.NewInstancePage_ImportDiagnosticWarningNotificationTitle
                                                  .Current(),
                                               GrowlLevel.Warning);
            }
        }
        catch (Exception e)
        {
            notificationService.PopMessage(e, LanguageManager.Instance.NewInstancePage_ImportDangerNotificationTitle.Current());
        }
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
        var path = await overlayService.RequestFileAsync(LanguageManager.Instance.NewInstancePage_RequestFilePrompt.Current(),
                                                         LanguageManager.Instance.NewInstancePage_RequestFileTitle.Current());
        if (path != null)
        {
            await TryImportFromFileAsync(path);
        }
    }

    [RelayCommand]
    private void GotoMarketplace() => navigationService.Navigate<MarketplacePortalPage>();

    [RelayCommand]
    private void ClearImportedPack() => ImportedPack = null;

    [RelayCommand]
    private async Task CreateAsync()
    {
        using var key = profileManager.RequestKey(DisplayName);
        var imported = ImportedPack;
        var attachments = new Dictionary<string, byte[]>();
        if (Thumbnail != null)
        {
            try
            {
                using var stream = new MemoryStream();
                Thumbnail.Save(stream, new PngBitmapEncoderOptions());
                attachments.Add("icon.png", stream.ToArray());
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, LanguageManager.Instance.NewInstancePage_IconSavingDangerNotificationTitle.Current());
            }
        }

        if (imported != null)
        {
            imported.Container.Profile.Name = DisplayName;
            await Task.Run(() => modpacks.InstallAsync(key, imported.Pack, imported.Container, CancellationToken.None, attachments));
        }
        else
        {
            var profile = new Profile
            {
                Name = DisplayName,
                Setup = new() { Loader = null, Version = VersionName, Source = null }
            };
            foreach (var (name, bytes) in attachments)
            {
                try
                {
                    var home = PathDef.Default.DirectoryOfHome(key.Key);
                    Directory.CreateDirectory(home);
                    await File.WriteAllBytesAsync(Path.Combine(home, name), bytes);
                }
                catch (Exception ex)
                {
                    notificationService.PopMessage(ex, LanguageManager.Instance.NewInstancePage_IconSavingDangerNotificationTitle.Current());
                }
            }
            profileManager.Add(key, profile);
        }

        persistenceService.AppendAction(new()
        {
            Key = key.Key,
            Kind = PersistenceService.ActionKind.Install,
            New = imported?.Path
        });

        navigationService.Navigate<InstancePage>(key.Key);
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
