using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.PageModels;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Accounts;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Services;

public class InstanceService
{
    private readonly InstanceManager _instanceManager;
    private readonly ProfileManager _profileManager;
    private readonly ConfigurationService _configurationService;
    private readonly PersistenceService _persistenceService;
    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;
    private readonly ExporterAgent _exporterAgent;
    private readonly NavigationService _navigationService;
    private readonly SourceCache<string, string> _pinnedKeys = new(k => k);

    public IObservable<IChangeSet<string, string>> PinnedChangeStream => _pinnedKeys.Connect();

    public InstanceService(
        InstanceManager instanceManager,
        ProfileManager profileManager,
        ConfigurationService configurationService,
        PersistenceService persistenceService,
        OverlayService overlayService,
        NotificationService notificationService,
        ExporterAgent exporterAgent,
        NavigationService navigationService
    )
    {
        _instanceManager = instanceManager;
        _profileManager = profileManager;
        _configurationService = configurationService;
        _persistenceService = persistenceService;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _exporterAgent = exporterAgent;
        _navigationService = navigationService;
        instanceManager.AccountUpdated += OnAccountUpdated;

        foreach (var key in _persistenceService.GetPinnedInstanceKeys())
        {
            _pinnedKeys.AddOrUpdate(key);
        }
    }

    private void OnAccountUpdated(object? sender, IAccount account)
    {
        _persistenceService.UpdateAccount(account.Uuid, AccountHelper.ToRaw(account));
    }

    public bool IsPinned(string key) => _pinnedKeys.Lookup(key).HasValue;

    public void Pin(string key)
    {
        _pinnedKeys.AddOrUpdate(key);
        _persistenceService.SetPinnedInstance(key, true);
    }

    public void Unpin(string key)
    {
        _pinnedKeys.RemoveKey(key);
        _persistenceService.SetPinnedInstance(key, false);
    }

    public void DeployAndLaunch(string key, LaunchMode mode)
    {
        var selector = _persistenceService.GetAccountSelector(key);
        if (selector != null)
        {
            var account = _persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);
                _persistenceService.UseAccount(account.Uuid);
                var profile = _profileManager.GetImmutable(key);
                var locator = CreateJavaLocator(profile, _configurationService.Value);
                var deploy = new DeployOptions(
                    profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false),
                    profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, false),
                    false
                );
                var launch = new LaunchOptions(
                    additionalArguments: profile.GetOverride(
                        Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                        _configurationService.Value.GameJavaAdditionalArguments
                    ),
                    maxMemory: profile.GetOverride(
                        Profile.OVERRIDE_JAVA_MAX_MEMORY,
                        _configurationService.Value.GameJavaMaxMemory
                    ),
                    windowSize: (
                        profile.GetOverride(
                            Profile.OVERRIDE_WINDOW_WIDTH,
                            _configurationService.Value.GameWindowInitialWidth
                        ),
                        profile.GetOverride(
                            Profile.OVERRIDE_WINDOW_HEIGHT,
                            _configurationService.Value.GameWindowInitialHeight
                        )
                    ),
                    quickConnectAddress: profile.GetOverride<string>(
                        Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER
                    ),
                    commandWrapperTemplate: profile.GetOverride(
                        Profile.OVERRIDE_BEHAVIOR_COMMAND_WRAPPER,
                        _configurationService.Value.GameCommandWrapper
                    ),
                    launchMode: mode,
                    account: cooked,
                    brand: Program.Brand
                );
                _instanceManager.DeployAndLaunch(key, deploy, launch, locator);
            }
        }
        else
        {
            throw new AccountNotFoundException("Account is not provided or removed after set");
        }
    }

    public void Deploy(
        string key,
        bool? fastMode = null,
        bool? resolveDependency = null,
        bool? fullCheckMode = null
    )
    {
        var profile = _profileManager.GetImmutable(key);
        fastMode ??= profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false);
        resolveDependency ??= profile.GetOverride(
            Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY,
            false
        );
        fullCheckMode ??= false;
        var locator = CreateJavaLocator(profile, _configurationService.Value);
        _instanceManager.Deploy(key, new(fastMode, resolveDependency, fullCheckMode), locator);
    }

    private static JavaHomeLocatorDelegate CreateJavaLocator(
        Profile profile,
        Configuration configuration
    ) =>
        JavaHelper.MakeLocator(major =>
            profile.GetOverride(
                Profile.OVERRIDE_JAVA_HOME,
                major switch
                {
                    8 => configuration.RuntimeJavaHome8,
                    11 => configuration.RuntimeJavaHome11,
                    16 or 17 => configuration.RuntimeJavaHome17,
                    21 => configuration.RuntimeJavaHome21,
                    24 or 25 => configuration.RuntimeJavaHome25,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(major),
                        major,
                        $"Unsupported java version: {major}"
                    ),
                }
            )
        );

    public void Play(string key)
    {
        try
        {
            DeployAndLaunch(key, LaunchMode.Managed);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            Resources.Shared_FailedToLaunchInstanceDangerNotificationTitle,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    // NOTE: explicit nulls bind to the configurable Deploy overload instead of recursing into this one-arg wrapper
    public void Deploy(string key)
    {
        try
        {
            Deploy(key, null, null, null);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            Resources.Shared_FailedToDeployInstanceDangerNotificationTitle,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    public Task OpenFolder(string? key)
    {
        if (key != null)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(dir),
                                                           Resources
                                                              .Shared_FailedToOpenInstanceFolderDangerNotificationTitle,
                                                           _notificationService,
                                                           thumbnail: ThumbnailHelper.ForInstance(key));
        }

        return Task.CompletedTask;
    }

    public void GotoProperties(string? key)
    {
        if (key != null)
        {
            _navigationService.Navigate<InstancePage>(new InstancePageModel.CompositeParameter(key,
                                                          typeof(InstancePropertiesPage)));
        }
    }

    public void GotoSetup(string? key)
    {
        if (key != null)
        {
            _navigationService.Navigate<InstancePage>(new InstancePageModel.CompositeParameter(key,
                                                          typeof(InstanceSetupPage)));
        }
    }

    public async Task ExportInstanceAsync(string? key)
    {
        if (key is not null && _profileManager.TryGetImmutable(key, out var profile))
        {
            var loaderLabel = "None";
            if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var loader))
            {
                loaderLabel = LoaderHelper.ToDisplayLabel(loader.Identity, loader.Version);
            }

            var overrideName = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_NAME);
            var overrideAuthor = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_AUTHOR);
            var overrideVersion = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_VERSION);

            var user = string.Empty;
            var account = _persistenceService.GetAccounts().FirstOrDefault(x => x.IsDefault);
            if (account != null)
            {
                user = AccountHelper.ToCooked(account).Username;
            }

            var dataPath = PathDef.Default.FileOfPackData(key);
            PackData? pack = null;
            try
            {
                if (File.Exists(dataPath))
                {
                    pack = JsonSerializer.Deserialize<PackData>(await File.ReadAllTextAsync(dataPath),
                                                                FileHelper.SerializerOptions);
                }
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MainWindow_ReadPackConfigDangerNotificationTitle,
                                                GrowlLevel.Warning,
                                                thumbnail: ThumbnailHelper.ForInstance(key));
            }

            pack ??= new();

            var availableTags = profile.Setup.Packages.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();

            var dialog = new ModpackExporterDialog
            {
                Pack = pack,
                AvailableTags = availableTags,
                OverlayService = _overlayService,
                NameOriginal = !string.IsNullOrEmpty(overrideName) ? overrideName : profile.Name,
                LoaderLabel = loaderLabel,
                PackageCount = profile.Setup.Packages.Count,
                AuthorOriginal = !string.IsNullOrEmpty(overrideAuthor) ? overrideAuthor : user,
                VersionOriginal = !string.IsNullOrEmpty(overrideVersion) ? overrideVersion : "1.0.0",
                Result = new ModpackExporterModel(key),
            };

            if (await _overlayService.PopDialogAsync(dialog) && dialog.Result is ModpackExporterModel model)
            {
                var top = TopLevelHelper.GetTopLevel();
                var storage = top.StorageProvider;
                if (storage.CanOpen)
                {
                    var name = !string.IsNullOrEmpty(model.NameOverride) ? model.NameOverride : dialog.NameOriginal;
                    var author = !string.IsNullOrEmpty(model.AuthorOverride)
                                     ? model.AuthorOverride
                                     : dialog.AuthorOriginal;
                    var version = !string.IsNullOrEmpty(model.VersionOverride)
                                      ? model.VersionOverride
                                      : dialog.VersionOriginal;
                    var storageItem = await storage.SaveFilePickerAsync(new()
                    {
                        SuggestedStartLocation =
                            await storage
                               .TryGetWellKnownFolderAsync(WellKnownFolder
                                                              .Downloads),
                        SuggestedFileName =
                            $"{name}.{version}",
                        DefaultExtension = "zip",
                        FileTypeChoices =
                        [
                            new(Resources
                                       .Shared_ZipArchiveFileTypeText)
                                {
                                    Patterns = ["*.zip"],
                                },
                            ],
                    });
                    if (storageItem is not null)
                    {
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_NAME, name);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_AUTHOR, author);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_VERSION, version);
                        var notification = _notificationService.PopProgress(name,
                                                                                Resources
                                                                                   .MainWindow_ExportModpackProgressingNotificationMessage,
                                                                                thumbnail: ThumbnailHelper
                                                                                   .ForInstance(key));
                        try
                        {
                            using var container = await Task.Run(async () => await _exporterAgent.ExportAsync(pack,
                                                                     model.SelectedExporterLabel,
                                                                     key,
                                                                     name,
                                                                     author,
                                                                     version));
                            notification.Report(50);
                            await using var stream = await storageItem.OpenWriteAsync();
                            await Task.Run(async () =>
                            {
                                await _exporterAgent.PackCompressedAsync(stream, container);
                            });
                            notification.Report(100);
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            var path = storageItem.TryGetLocalPath();
                            _notificationService.PopMessage(path ?? Resources.Enum_Unknown,
                                                            Resources
                                                               .MainWindow_ExportModpackSuccessNotificationTitle,
                                                            thumbnail: ThumbnailHelper.ForInstance(key));
                        }
                        catch (Exception ex)
                        {
                            _notificationService.PopMessage(ex,
                                                            Resources
                                                               .MainWindow_ExportModpackDangerNotificationTitle,
                                                            thumbnail: ThumbnailHelper.ForInstance(key));
                        }
                        finally
                        {
                            notification.Dispose();
                        }
                    }

                }
            }

            var dir = Path.GetDirectoryName(dataPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(pack, FileHelper.SerializerOptions));
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MainWindow_SavePackConfigDangerNotificationTitle,
                                                thumbnail: ThumbnailHelper.ForInstance(key));
            }
        }
    }
}
