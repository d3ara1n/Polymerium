using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Humanizer;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views.Instances;
using Polymerium.Core;
using Polymerium.Core.Components;
using Polymerium.Core.Engines;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Extensions;
using Polymerium.Core.GameAssets;
using Polymerium.Core.LaunchConfigurations;
using Polymerium.Core.Managers;
using Polymerium.Core.Managers.GameModels;
using Polymerium.Core.Stars;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly AssetManager _assetManager;
    private readonly ComponentManager _componentManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly IFileBaseService _fileBase;
    private readonly LocalizationService _localizationService;
    private readonly JavaManager _javaManager;
    private readonly INotificationService _notificationService;
    private readonly NavigationService _navigationService;
    private readonly ResolveEngine _resolver;
    private readonly GameManager _gameManager;
    private readonly AccountManager _accountManager;
    private readonly DispatcherQueue _dispatcher;
    private string coreVersion = string.Empty;

    private bool isModSupported;

    private bool isShaderSupported;
    private uint modCount;
    private Uri? referenceUrl;
    private uint resourcePackCount;
    private uint shaderPackCount;

    private Action<InstanceState>? stateChangeHandler;

    public InstanceViewModel(
        ViewModelContext context,
        ResolveEngine resolver,
        ConfigurationManager configurationManager,
        IOverlayService overlayService,
        IFileBaseService fileBase,
        JavaManager javaManager,
        INotificationService notificationService,
        ComponentManager componentManager,
        NavigationService navigationService,
        AssetManager assetManager,
        LocalizationService localizationService,
        GameManager gameManager,
        AccountManager accountManager
    )
    {
        _componentManager = componentManager;
        _resolver = resolver;
        _navigationService = navigationService;
        _javaManager = javaManager;
        _configurationManager = configurationManager;
        _assetManager = assetManager;
        _fileBase = fileBase;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _gameManager = gameManager;
        _accountManager = accountManager;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Instance = context.AssociatedInstance!;
        OverlayService = overlayService;
        CoreVersion = Instance.Inner.GetCoreVersion() ?? "N/A";
        GotoConfigurationViewCommand = new RelayCommand(GotoConfigurationView);
        Components = new ObservableCollection<ComponentTagItemModel>(
            BuildComponentModels(Instance.Components)
        );
        InformationItems = new ObservableCollection<InstanceInformationItemModel>
        {
            new(
                "\uF427",
                _localizationService.GetString("InstanceView_Other_Identity_Label"),
                Instance.Id
            ),
            new(
                "\uE125",
                _localizationService.GetString("InstanceView_Other_Author_Label"),
                string.IsNullOrEmpty(Instance.Author)
                    ? _localizationService.GetString("InstanceView_Other_Author_Unknown")
                    : Instance.Author
            ),
            new(
                "\uE121",
                _localizationService.GetString("InstanceView_Other_PlayTime_Label"),
                Instance.PlayTime.Humanize()
            ),
            new(
                "\uEC92",
                _localizationService.GetString("InstanceView_Other_LastPlay_Label"),
                Instance.LastPlay == null
                    ? _localizationService.GetString("InstanceView_Other_LastPlay_Unknown")
                    : Instance.LastPlay.Humanize()
            ),
            new(
                "\uEB50",
                _localizationService.GetString("InstanceView_Other_PlayCount_Label"),
                $"{Instance.PlayCount}"
            ),
            new(
                "\uEB05",
                _localizationService.GetString("InstanceView_Other_SuccessRate_Label"),
                Instance.PlayCount == 0
                    ? _localizationService.GetString("InstanceView_Other_SuccessRate_Unknown")
                    : $"{(Instance.PlayCount - Instance.ExceptionCount) / (float)Instance.PlayCount * 100}%"
            ),
            new(
                "\uEC92",
                _localizationService.GetString("InstanceView_Other_CreateDate_Label"),
                Instance.CreatedAt.Humanize()
            ),
            new(
                "\uEC92",
                _localizationService.GetString("InstanceView_Other_LastRestore_Label"),
                Instance.LastRestore == null
                    ? _localizationService.GetString("InstanceView_Other_LastRestore_Unknown")
                    : Instance.LastRestore.Humanize()
            )
        };
        OpenInExplorerCommand = new RelayCommand<string>(OpenInExplorer);
        Saves = new ObservableCollection<InstanceWorldSaveModel>();
        Screenshots = new ObservableCollection<InstanceScreenshotModel>();
        RawAssetSource = new ObservableCollection<AssetRaw>();
        RawShaderPacks = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.ShaderPack
        };
        RawMods = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.Mod
        };
        RawResourcePacks = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.ResourcePack
        };
        IsModSupported = Instance.Components.Any(x => ComponentMeta.MINECRAFT != x.Identity);
        IsShaderSupported = true;
    }

    public string CoreVersion
    {
        get => coreVersion;
        set => SetProperty(ref coreVersion, value);
    }

    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }
    public ObservableCollection<InstanceWorldSaveModel> Saves { get; }
    public ObservableCollection<InstanceScreenshotModel> Screenshots { get; }
    public ObservableCollection<AssetRaw> RawAssetSource { get; }
    public IAdvancedCollectionView RawShaderPacks { get; }
    public IAdvancedCollectionView RawMods { get; }
    public IAdvancedCollectionView RawResourcePacks { get; }
    public IRelayCommand<string> OpenInExplorerCommand { get; }

    public bool IsModSupported
    {
        get => isModSupported;
        set => SetProperty(ref isModSupported, value);
    }

    public bool IsShaderSupported
    {
        get => isShaderSupported;
        set => SetProperty(ref isShaderSupported, value);
    }

    public uint ModCount
    {
        get => modCount;
        set => SetProperty(ref modCount, value);
    }

    public uint ResourcePackCount
    {
        get => resourcePackCount;
        set => SetProperty(ref resourcePackCount, value);
    }

    public uint ShaderPackCount
    {
        get => shaderPackCount;
        set => SetProperty(ref shaderPackCount, value);
    }

    public Uri? ReferenceUrl
    {
        get => referenceUrl;
        set => SetProperty(ref referenceUrl, value);
    }

    public GameInstanceModel Instance { get; }
    public IOverlayService OverlayService { get; }
    public ICommand GotoConfigurationViewCommand { get; }

    private void OpenInExplorer(string? dir)
    {
        var path = Path.Combine(
            _fileBase.Locate(new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", Instance.Id))),
            dir!
        );
        Process.Start(
            new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = true,
                Arguments = Directory.Exists(path) ? path : $"/select, {path}"
            }
        );
    }

    private IEnumerable<ComponentTagItemModel> BuildComponentModels(
        IEnumerable<Component> components
    )
    {
        return components.Select(x =>
        {
            _componentManager.TryFindByIdentity(x.Identity, out var meta);
            return new ComponentTagItemModel(
                meta?.FriendlyName ?? x.Identity,
                x.Version,
                x.Identity,
                $"{x.Identity}:{x.Version}"
            );
        });
    }

    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>(
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight }
        );
    }

    public void LoadAssets()
    {
        var assets = _assetManager.ScanAssets(Instance.Inner);
        RawAssetSource.Clear();
        foreach (var asset in assets)
            RawAssetSource.Add(asset);
        RawMods.Refresh();
        ModCount = (uint)RawMods.Count;
        RawResourcePacks.Refresh();
        ResourcePackCount = (uint)RawResourcePacks.Count;
        RawShaderPacks.Refresh();
        ShaderPackCount = (uint)RawShaderPacks.Count;
    }

    public void LoadSaves()
    {
        var saves = _assetManager.ScanSaves(Instance.Inner);
        Saves.Clear();
        foreach (var save in saves)
        {
            var model = new InstanceWorldSaveModel(
                _fileBase.Locate(
                    new Uri(
                        new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", Instance.Id)),
                        $"saves/{save.FolderName}/icon.png"
                    )
                ),
                save.Name,
                save.Seed,
                save.GameVersion,
                save.LastPlayed,
                save
            );
            Saves.Add(model);
        }
    }

    public void LoadScreenshots()
    {
        var screenshots = _assetManager.ScanScreenshots(Instance.Inner);
        Screenshots.Clear();
        foreach (var screenshot in screenshots)
        {
            var model = new InstanceScreenshotModel(screenshot.FileName);
            Screenshots.Add(model);
        }
    }

    public async Task LoadInstanceInformationAsync(Action<Uri?, bool> callback)
    {
        var isNeeded = !Instance.Inner.CheckIfRestored(_fileBase, out _);
        Uri? url = null;
        if (Instance.ReferenceSource != null)
        {
            var result = await _resolver.ResolveAsync(
                Instance.ReferenceSource,
                new ResolverContext(Instance.Inner)
            );
            if (result)
                url = result.Value.Resource.Reference;
        }

        callback(url, isNeeded);
    }

    public void SetStateChangeHandler(Action<InstanceState> handler) =>
        stateChangeHandler = handler;

    public void Start(Action<int?> prepare)
    {
        stateChangeHandler?.Invoke(InstanceState.Preparing);
        if (Prepare(prepare) && Launch())
            stateChangeHandler?.Invoke(InstanceState.Running);
        else
            stateChangeHandler?.Invoke(InstanceState.Idle);
    }

    // true if can continue
    public bool Prepare(Action<int?> callback)
    {
        var handle = new AutoResetEvent(false);
        var succ = false;
        var tracker = _gameManager.Prepare(Instance.Inner, callback);
        tracker.FinishCallback = (successful, prepareError, exception, restoreError) =>
        {
            succ = successful;
            handle.Set();
            if (!succ)
            {
                var reason = "未知原因";
                switch (prepareError)
                {
                    case PrepareError.PrepareFailure:
                        switch (restoreError)
                        {
                            case RestoreError.ComponentInstallationFailure:
                                reason = "已找到组件但配置失败";
                                break;
                            case RestoreError.ComponentNotFound:
                                reason = "实例的组件不合法或已失效总之不受支持";
                                break;
                            case RestoreError.IOException:
                                reason = "本地文件或网络文件传输失败，该错误可通过重试尝试解决";
                                break;
                            case RestoreError.ResourceNotReacheable:
                                reason = "资源无法根据已有索引拉取详细信息，该错误可通过重试尝试解决";
                                break;
                        }
                        break;
                    case PrepareError.DownloadFailure:
                        reason = "已拉取文件列表，但下载时部分文件出错，该错误可通过重试尝试解决";
                        break;
                    case PrepareError.ExceptionOcurred:
                        reason = $"意料之外的错误发生，{exception!.Message}";
                        break;
                }
                StartAbort(reason);
            }
        };
        handle.WaitOne();
        if (succ)
        {
            if (_accountManager.TryFindById(Instance.BoundAccountId!, out var account))
            {
                if (!account!.ValidateAsync().Result && !account.RefreshAsync().Result)
                {
                    StartAbort("或账号无法验证");
                }
            }
            else
            {
                StartAbort("未配置账号");
            }
        }
        return succ;
    }

    private void StartAbort(string message)
    {
        _dispatcher
            .TryEnqueue(() =>
            {
                var messageBox = new MessageDialog() { XamlRoot = App.Current.Window.Content.XamlRoot, Title = "挂起", Message = message };
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                messageBox.ShowAsync();
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            });
    }

    // true if can enter running
    public bool Launch()
    {
        _accountManager.TryFindById(Instance.BoundAccountId!, out var account);
        var workingDir = new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", Instance!.Id));
        var assetsRoot = new Uri(ConstPath.CACHE_ASSETS_DIR);
        var nativesRoot = new Uri(ConstPath.INSTANCE_NATIVES_DIR.Replace("{0}", Instance!.Id));
        var librariesRoot = new Uri(ConstPath.CACHE_LIBRARIES_DIR);
        var polylockFile = new Uri(
            ConstPath.INSTANCE_POLYLOCKDATA_FILE.Replace("{0}", Instance!.Id)
        );
        if (_fileBase.TryReadAllText(polylockFile, out var content))
        {
            var polylock = JsonConvert.DeserializeObject<PolylockData>(content);
            var configuration = new CompoundLaunchConfiguration(
                Instance.Inner.Configuration,
                _configurationManager.Current.GameGlobals
            );
            var autoDetectJava = configuration.AutoDetectJava ?? false;
            var javaHomes = autoDetectJava
                ? _javaManager.QueryJavaInstallations()
                : new[] { configuration.JavaHome }!;
            JavaInstallationModel? selectedJava = null;
            foreach (var javaHome in javaHomes)
            {
                var verify = _javaManager.VerifyJavaHome(javaHome);
                if (
                    verify.TryUnwrap(out var model)
                    && (
                        (
                            model!.JavaVersion?.StartsWith("1.8") == true
                                ? "8"
                                : model.JavaVersion ?? string.Empty
                        ).StartsWith(polylock.JavaMajorVersionRequired.ToString())
                        || (configuration.SkipJavaVersionCheck == true && !autoDetectJava)
                    )
                )
                {
                    selectedJava = model;
                    break;
                }
            }
            if (selectedJava != null)
            {
                // log the java information model
                var builder = new PlanetaryEngineBuilder();
                builder
                    .WithJavaPath(Path.Combine(selectedJava.HomePath, "bin", "java.exe"))
                    .WithMainClass(polylock.MainClass)
                    .WithWorkingDirectory(_fileBase.Locate(workingDir))
                    .WithGameArguments(
                        polylock.GameArguments.Concat(
                            new[]
                            {
                                "--width",
                                "${resolution_width}",
                                "--height",
                                "${resolution_height}"
                            }
                        )
                    )
                    .WithJvmArguments(
                        polylock.JvmArguments
                            .Concat(new[] { "-Xmx${jvm_max_memory}m" })
                            .Concat(
                                (configuration.AdditionalJvmArguments ?? string.Empty).Split(' ')
                            )
                    )
                    .CraftStarship(configure =>
                    {
                        configure
                            .AddCargo(polylock.Cargo)
                            .AddCrate("auth_player_name", account!.Nickname)
                            .AddCrate("version_name", Instance.Name)
                            .AddCrate("game_directory", _fileBase.Locate(workingDir))
                            .AddCrate("assets_root", _fileBase.Locate(assetsRoot))
                            .AddCrate("assets_index_name", polylock.AssetIndex.Id)
                            .AddCrate("auth_uuid", account.UUID)
                            .AddCrate(
                                "auth_access_token",
                                !string.IsNullOrWhiteSpace(account!.AccessToken)
                                    ? account.AccessToken
                                    : "unauthorized"
                            )
                            .AddCrate("user_type", account.LoginType)
                            .AddCrate("version_type", "Polymerium")
                            // rule os
                            .AddCrate("os.name", "windows")
                            .AddCrate("os.arch", "x86")
                            .AddCrate("os.version", Environment.OSVersion.Version.ToString())
                            // game resolution
                            .AddCrate(
                                "resolution_width",
                                (configuration.WindowWidth ?? 854).ToString()
                            )
                            .AddCrate(
                                "resolution_height",
                                (configuration.WindowHeight ?? 480).ToString()
                            )
                            // jvm
                            .AddCrate("natives_directory", _fileBase.Locate(nativesRoot))
                            .AddCrate("classpath_separator", ";")
                            .AddCrate(
                                "classpath",
                                string.Join(
                                    ';',
                                    polylock.Libraries
                                        .Where(
                                            x => x is { PresentInClassPath: true, IsNative: false }
                                        )
                                        .Select(
                                            x => _fileBase.Locate(new Uri(librariesRoot, x.Path))
                                        )
                                )
                            )
                            .AddCrate("launcher_name", "Polymerium")
                            .AddCrate(
                                "launcher_version",
                                GetType().Assembly.GetName().Version?.ToString() ?? "0.0"
                            )
                            // custom jvm argument patches
                            .AddCrate(
                                "jvm_max_memory",
                                configuration.JvmMaxMemory?.ToString() ?? "4096"
                            );
                    });
                _gameManager.LaunchFireForget(builder);
                Instance.LastPlay = DateTimeOffset.Now;
                _notificationService.Enqueue("游戏发射", "就是游戏发射的意思");
                // false 'cause Launch**FireForge**
                return false;
            }
            else
            {
                StartAbort("JavaHome 的配置项目不可用或没有找到系统中已安装适配版本的 Java 版本");
            }
        }
        else
        {
            StartAbort("Polylock 文件以错误的方式出现，这可能是还原过程不够彻底或发生未察觉的异常导致");
        }
        return false;
    }

    public InstanceState QueryInstanceState(Action<int?> prepareCallback)
    {
        if (_gameManager.IsPreparing(Instance.Id, out var prepare))
        {
            prepare!.UpdateCallback = prepareCallback;
            return InstanceState.Preparing;
        }
        else if (_gameManager.IsRunning(Instance.Id, out var run))
        {
            // TODO
            return InstanceState.Running;
        }
        else
        {
            return InstanceState.Idle;
        }
    }
}
