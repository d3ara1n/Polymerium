using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.PageModels;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Accounts;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Services;

public class InstanceService
{
    private readonly ConfigurationService _configurationService;
    private readonly ExporterAgent _exporterAgent;
    private readonly InstanceManager _instanceManager;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;
    private readonly PersistenceService _persistenceService;
    private readonly SourceCache<string, string> _pinnedKeys = new(k => k);
    private readonly ProfileManager _profileManager;

    public InstanceService(
        InstanceManager instanceManager,
        ProfileManager profileManager,
        ConfigurationService configurationService,
        PersistenceService persistenceService,
        OverlayService overlayService,
        NotificationService notificationService,
        ExporterAgent exporterAgent,
        NavigationService navigationService)
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

    public IObservable<IChangeSet<string, string>> PinnedChangeStream => _pinnedKeys.Connect();

    private void OnAccountUpdated(object? sender, IAccount account) =>
        _persistenceService.UpdateAccount(account.Uuid, AccountHelper.ToRaw(account));

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
        var account = selector is null ? null : _persistenceService.GetAccount(selector.Uuid);
        if (account is null)
        {
            throw new AccountNotFoundException("Account is not provided or removed after set");
        }

        var cooked = AccountHelper.ToCooked(account);
        _persistenceService.UseAccount(account.Uuid);
        var profile = _profileManager.GetImmutable(key);
        var vault = CreateJavaVault(profile, _configurationService.Value);
        var deploy = new DeployOptions(false);
        var launch =
            new LaunchOptions(additionalArguments:
                              profile.GetOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                                                  _configurationService.Value.GameJavaAdditionalArguments),
                              maxMemory:
                              profile.GetOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                                                  _configurationService.Value.GameJavaMaxMemory),
                              windowSize:
                              (profile.GetOverride(Profile.OVERRIDE_WINDOW_WIDTH,
                                                   _configurationService.Value.GameWindowInitialWidth),
                               profile.GetOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                                                   _configurationService.Value.GameWindowInitialHeight)),
                              quickConnectAddress:
                              profile.GetOverride<string>(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER),
                              commandWrapperTemplate:
                              profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_COMMAND_WRAPPER,
                                                  _configurationService.Value.GameCommandWrapper),
                              launchMode: mode,
                              account: cooked,
                              brand: Program.Brand);
        _instanceManager.DeployAndLaunch(key, deploy, launch, vault);
    }

    public void Deploy(string key, bool? fullCheckMode = null)
    {
        var profile = _profileManager.GetImmutable(key);
        var vault = CreateJavaVault(profile, _configurationService.Value);
        _instanceManager.Deploy(key, new(fullCheckMode), vault);
    }

    // vault 是“用户实际提供了哪些 major”：实例 override 是通配项（任何 major 都落它），否则按全局
    //  预设逐 major 列出。空值不构成提供项，仲裁时才不会被当成“有”。
    private static IReadOnlyList<(uint? Major, string Home)> CreateJavaVault(Profile profile, Configuration configuration)
    {
        if (profile.GetOverride<string>(Profile.OVERRIDE_JAVA_HOME) is { Length: > 0 } home)
        {
            return JavaHelper.WildcardVault(home);
        }

        List<(uint? Major, string Home)> vault = [];
        Add(8, configuration.RuntimeJavaHome8);
        Add(11, configuration.RuntimeJavaHome11);
        Add(16, configuration.RuntimeJavaHome17);
        Add(17, configuration.RuntimeJavaHome17);
        Add(21, configuration.RuntimeJavaHome21);
        Add(24, configuration.RuntimeJavaHome25);
        Add(25, configuration.RuntimeJavaHome25);
        return vault;

        void Add(uint major, string home)
        {
            if (!string.IsNullOrEmpty(home))
            {
                vault.Add((major, home));
            }
        }
    }

    // 两类失败都要用户回到 Java 设置里指定运行时：需求集为空（patch 自相矛盾）与两个提供方都覆盖不到
    //  需求。提示直接把用户带到设置页，而不是让他自己找。
    private void PopJavaUnavailable(Exception ex, string key) =>
        _notificationService.PopMessage(ex,
                                        LanguageManager.Instance.InstanceService_JavaUnavailableNotificationTitle.Current()
                                                 .Replace("{0}", key),
                                        GrowlLevel.Danger,
                                        ThumbnailHelper.ForInstance(key),
                                        new GrowlAction(LanguageManager.Instance.InstanceService_ConfigureJavaActionText.Current(),
                                                        new RelayCommand(() => _navigationService.Navigate<SettingsPage>())));

    public void Play(string key)
    {
        try
        {
            DeployAndLaunch(key, LaunchMode.Managed);
        }
        catch (NoCompatibleJavaException ex)
        {
            PopJavaUnavailable(ex, key);
        }
        catch (JavaNotFoundException ex)
        {
            PopJavaUnavailable(ex, key);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            LanguageManager.Instance.MainWindow_InstanceLaunchingDangerNotificationTitle.Current()
                                                     .Replace("{0}", key),
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    // WARNING: explicit nulls bind to the configurable Deploy overload instead of recursing into this one-arg wrapper
    public void Deploy(string key)
    {
        try
        {
            Deploy(key, null);
        }
        catch (NoCompatibleJavaException ex)
        {
            PopJavaUnavailable(ex, key);
        }
        catch (JavaNotFoundException ex)
        {
            PopJavaUnavailable(ex, key);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            LanguageManager.Instance.MainWindow_InstanceDeployingDangerNotificationTitle.Current()
                                                     .Replace("{0}", key),
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    public async Task ResetAsync(string key)
    {
        if (!await _overlayService.RequestStrongConfirmationAsync(LanguageManager.Instance.InstancePropertiesPage_ResetConfirmationMessage.Current(),
                                                                  LanguageManager.Instance.InstancePropertiesPage_ResetConfirmationTitle.Current()))
        {
            return;
        }

        if (_instanceManager.IsInUse(key))
        {
            _notificationService.PopMessage(LanguageManager.Instance.InstancePropertiesPage_ResetInUseWarningNotificationMessage.Current(),
                                            LanguageManager.Instance.InstancePropertiesPage_ResetInUseWarningNotificationTitle.Current()
                                                     .Replace("{0}", key),
                                            GrowlLevel.Warning,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
            return;
        }

        var build = PathDef.Default.DirectoryOfBuild(key);
        var file = PathDef.Default.FileOfLockData(key);
        try
        {
            if (Directory.Exists(build))
            {
                Directory.Delete(build, true);
            }

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            _persistenceService.AppendAction(new() { Key = key, Kind = PersistenceService.ActionKind.Reset });
            _notificationService.PopMessage(LanguageManager.Instance.InstancePropertiesPage_ResetSuccessNotificationMessage.Current(),
                                            key,
                                            GrowlLevel.Success,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    public Task OpenFolder(string? key)
    {
        if (key != null)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(dir),
                                                           LanguageManager.Instance.Shared_FailedToOpenFolderDangerNotificationTitle.Current(),
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
                                                LanguageManager.Instance.MainWindow_ReadPackConfigDangerNotificationTitle.Current(),
                                                GrowlLevel.Warning,
                                                ThumbnailHelper.ForInstance(key));
            }

            pack ??= new();

            var availableTags = profile.Setup.Packages.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();

            var patchIndex = await PatchStorageHelper.ReadIndexAtAsync(PathDef.Default.DirectoryOfPatches(key));
            var dialog = new ModpackExporterDialog
            {
                HasPatches = patchIndex.Import.Count > 0,
                Pack = pack,
                AvailableTags = availableTags,
                OverlayService = _overlayService,
                NameOriginal = !string.IsNullOrEmpty(overrideName) ? overrideName : profile.Name,
                LoaderLabel = loaderLabel,
                PackageCount = profile.Setup.Packages.Count,
                AuthorOriginal = !string.IsNullOrEmpty(overrideAuthor) ? overrideAuthor : user,
                VersionOriginal = !string.IsNullOrEmpty(overrideVersion) ? overrideVersion : "1.0.0",
                Result = new ModpackExporterModel(key)
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
                        SuggestedFileName = $"{name}.{version}",
                        DefaultExtension = "zip",
                        FileTypeChoices =
                        [
                            new(LanguageManager.Instance.Shared_ZipArchiveFileTypeText.Current())
                            {
                                Patterns = ["*.zip"]
                            }
                        ]
                    });
                    if (storageItem is not null)
                    {
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_NAME, name);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_AUTHOR, author);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_VERSION, version);
                        var notification = _notificationService.PopProgress(name,
                                                                            LanguageManager.Instance.MainWindow_ExportModpackProgressingNotificationMessage.Current(),
                                                                            thumbnail: ThumbnailHelper
                                                                               .ForInstance(key));
                        try
                        {
                            using var container = await Task.Run(async () => await _exporterAgent.ExportAsync(pack,
                                                                                 model.SelectedExporterLabel,
                                                                                 key,
                                                                                 name,
                                                                                 author,
                                                                                 version,
                                                                                 PurifyRecipeGroups));
                            notification.Report(50);
                            await using var stream = await storageItem.OpenWriteAsync();
                            await Task.Run(async () =>
                            {
                                await _exporterAgent.PackCompressedAsync(stream, container);
                            });
                            notification.Report(100);
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            var path = storageItem.TryGetLocalPath();
                            _notificationService.PopMessage(path ?? LanguageManager.Instance.Enum_Unknown.Current(),
                                                            LanguageManager.Instance.MainWindow_ExportModpackSuccessNotificationTitle.Current(),
                                                            thumbnail: ThumbnailHelper.ForInstance(key));
                        }
                        catch (Exception ex)
                        {
                            _notificationService.PopMessage(ex,
                                                            LanguageManager.Instance.MainWindow_ExportModpackDangerNotificationTitle.Current(),
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
                                                LanguageManager.Instance.MainWindow_SavePackConfigDangerNotificationTitle.Current(),
                                                thumbnail: ThumbnailHelper.ForInstance(key));
            }
        }
    }

    // WARNING: recipe 是宿主本地概念（配方数据不随包分发），导出前统一净化为 collection 分组，
    //  消费者侧以同名集合呈现而非悬空的 recipe 引用。
    private void PurifyRecipeGroups(Profile profile)
    {
        string Map(string? source)
        {
            if (source is null || !RecipeHelper.TryGetId(source, out var id))
            {
                return source!;
            }

            return CollectionHelper.ToUri(_persistenceService.GetRecipe(id)?.Name ?? id);
        }

        foreach (var entry in profile.Setup.Packages)
        {
            entry.Source = Map(entry.Source);
        }

        var orders = profile.Setup.SourceOrders;
        for (var i = 0; i < orders.Count; i++)
        {
            orders[i] = Map(orders[i]);
        }
    }

    public IReadOnlyList<RecipeReference> GetRecipeReferences(string recipeId)
    {
        var uri = RecipeHelper.ToUri(recipeId);
        return _profileManager
              .Profiles.Where(p => p.Item2.Setup.Packages.Any(e => e.Source == uri))
              .Select(p => new RecipeReference(p.Item1, p.Item2.Name))
              .ToList();
    }
}
