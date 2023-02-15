using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Abstractions.Models;
using Polymerium.App.Services;
using Polymerium.Core;
using Polymerium.Core.Engines;
using Polymerium.Core.LaunchConfigurations;
using Polymerium.Core.Stars;

namespace Polymerium.App.ViewModels;

public sealed class PrepareGameViewModel : ObservableObject, IDisposable
{
    private readonly AccountManager _accountManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly DispatcherQueue _dispatcher;
    private readonly IFileBaseService _fileBase;
    private readonly JavaManager _javaManager;
    private readonly MemoryStorage _memoryStorage;
    private readonly IOverlayService _overlayService;
    private readonly RestoreEngine _restore;

    private readonly CancellationTokenSource source = new();
    private IGameAccount? account;

    private string labelTitle = string.Empty;
    private string progress = string.Empty;
    private string progressDetails = string.Empty;

    private Action? readyHandler;

    public PrepareGameViewModel(
        RestoreEngine restore,
        AccountManager accountManager,
        ConfigurationManager configurationManager,
        IFileBaseService fileBase,
        IOverlayService overlayService,
        MemoryStorage memoryStorage,
        JavaManager javaManager
    )
    {
        _restore = restore;
        _accountManager = accountManager;
        _configurationManager = configurationManager;
        _fileBase = fileBase;
        _overlayService = overlayService;
        _memoryStorage = memoryStorage;
        _javaManager = javaManager;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        StartCommand = new RelayCommand(Start);
    }

    public ICommand StartCommand { get; }
    public GameInstance? Instance { get; private set; }

    public IGameAccount? Account
    {
        get => account;
        set => SetProperty(ref account, value);
    }

    public string LabelTitle
    {
        get => labelTitle;
        set => SetProperty(ref labelTitle, value);
    }

    public string Progress
    {
        get => progress;
        set => SetProperty(ref progress, value);
    }

    public string ProgressDetails
    {
        get => progressDetails;
        set => SetProperty(ref progressDetails, value);
    }

    public void Dispose()
    {
        // application exit
        Cancel();
    }

    public bool GotInstance(GameInstance instance, Action handler)
    {
        Instance = instance;
        readyHandler = handler;
        if (_accountManager.TryFindById(Instance!.BoundAccountId ?? string.Empty, out var a))
        {
            Account = a;
            return true;
        }

        return false;
    }

    public void Cancel()
    {
        if (!source.IsCancellationRequested)
            source.Cancel();
    }

    public async Task PrepareAsync()
    {
        await PrepareAsync(source.Token);
    }

    public async Task PrepareAsync(CancellationToken token)
    {
        Instance!.PlayCount++;
        var stage = _restore.ProduceStage(Instance, _memoryStorage.SupportedComponents);
        stage.TaskFinishedCallback = UpdateTaskProgressSafe;
        stage.Token = token;
        UpdateLabelSafe(stage.StageName);
        var hasNext = false;
        // TODO: update title as rolling text with stage name
        do
        {
            UpdateTaskProgressSafe("准备中");
            try
            {
                var option = await stage.StartAsync();
                hasNext = option.TryUnwrap(out var lastStage);
                if (hasNext)
                {
                    stage = lastStage!;
                    stage.Token = token;
                    stage.TaskFinishedCallback = UpdateTaskProgressSafe;
                    UpdateLabelSafe(stage.StageName);
                }
            }
            catch (Exception ex)
            {
                Instance.ExceptionCount++;
                CriticalError($"{stage.StageName}\n{ex.Message}:\n{ex.StackTrace}");
                return;
            }
        } while (hasNext);

        if (stage.IsCompletedSuccessfully)
        {
            if (!stage.Token.IsCancellationRequested)
            {
                UpdateLabelSafe("您的游戏已经准备就绪", true);
                UpdateTaskProgressSafe("准备就绪");
                Instance.LastRestore = DateTimeOffset.Now;
            }
            else
            {
                Instance.PlayCount--;
            }
        }
        else
        {
            Instance.ExceptionCount++;
            CriticalError(
                $"{stage.StageName}\n{stage.ErrorMessage}:\n{stage.Exception?.StackTrace}"
            );
        }
    }

    private void UpdateLabelSafe(string title, bool ready = false)
    {
        _dispatcher.TryEnqueue(
            DispatcherQueuePriority.Normal,
            () =>
            {
                LabelTitle = title;
                if (ready)
                    readyHandler?.Invoke();
            }
        );
    }

    private void UpdateTaskProgressSafe(string message, string? details = null)
    {
        _dispatcher.TryEnqueue(
            DispatcherQueuePriority.Normal,
            () =>
            {
                Progress = message;
                ProgressDetails = details ?? string.Empty;
            }
        );
    }

    public void Start()
    {
        var workingDir = new Uri($"poly-file://{Instance!.Id}/");
        var assetsRoot = new Uri("poly-file:///assets/");
        var nativesRoot = new Uri($"poly-file://{Instance!.Id}/natives/");
        var librariesRoot = new Uri("poly-file:///libraries/");
        var polylockFile = new Uri($"poly-file://{Instance!.Id}/polymerium.lock.json");
        if (_fileBase.TryReadAllText(polylockFile, out var content))
        {
            var polylock = JsonConvert.DeserializeObject<PolylockData>(content);
            var configuration = new CompoundLaunchConfiguration(
                Instance.Configuration,
                _configurationManager.Current.GameGlobals
            );
            var autoDetectJava = configuration.AutoDetectJava ?? false;
            var javaHomes = autoDetectJava
                ? _javaManager.QueryJavaInstallations()
                : new[] { configuration.JavaHome };
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
                    // log the java information model
                    var builder = new PlanetBlenderBuilder();
                    builder
                        .WithJavaPath(Path.Combine(javaHome!, "bin", "java.exe"))
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
                            polylock.JvmArguments.Concat(new[] { "-Xmx${jvm_max_memory}m" })
                        )
                        .ConfigureStarship(configure =>
                        {
                            configure
                                .AddCargo(polylock.Cargo)
                                .AddCrate("auth_player_name", Account!.Nickname)
                                // net.minecraft 的版本，这里试试换实例名会不会有别的影响
                                .AddCrate("version_name", Instance.Name)
                                .AddCrate("game_directory", _fileBase.Locate(workingDir))
                                .AddCrate("assets_root", _fileBase.Locate(assetsRoot))
                                .AddCrate("assets_index_name", polylock.AssetIndex.Id)
                                .AddCrate("auth_uuid", Account!.UUID)
                                // this wont work
                                .AddCrate("auth_access_token", Guid.NewGuid().ToString())
                                // really?
                                .AddCrate("clientid", "00000000402b5328")
                                .AddCrate("user_type", "legacy")
                                .AddCrate("version_type", "Polymerium")
                                // rule os
                                // TODO: 目前只支持 windows x86
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
                                        polylock.Libraries.Select(
                                            x => _fileBase.Locate(new Uri(librariesRoot, x.Path))
                                        )
                                    )
                                )
                                .AddCrate("launcher_name", "Polymerium")
                                .AddCrate("launcher_version", "0.1.0")
                                // custom jvm argument patches
                                .AddCrate(
                                    "jvm_max_memory",
                                    configuration.JvmMaxMemory?.ToString() ?? "4096"
                                );
                        });
                    var blender = builder.Build();
                    blender.Start();
                    Instance.LastPlay = DateTimeOffset.Now;
                    return;
                }
            }

            CriticalError("JavaHome 的配置项目不可用或没有找到系统中已安装适配版本的 Java 版本");
        }
        else
        {
            CriticalError("Polylock 文件以错误的方式出现，这可能是还原过程不够彻底或发生未察觉的异常导致");
        }
    }

    private void CriticalError(string msg)
    {
        _dispatcher.TryEnqueue(async () =>
        {
            _overlayService.Dismiss();
            var dialog = new ContentDialog();
            dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
            dialog.Title = "关键错误";
            dialog.Content = msg;
            dialog.CloseButtonText = "晓得了";
            await dialog.ShowAsync();
        });
    }
}