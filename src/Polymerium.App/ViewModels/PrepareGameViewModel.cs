using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Services;
using Polymerium.Core;
using Polymerium.Core.Engines;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.LaunchConfigurations;
using Polymerium.Core.Models.Mojang.Indexes;
using Polymerium.Core.Stars;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.ViewModels
{
    public sealed partial class PrepareGameViewModel : ObservableObject, IDisposable
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly RestoreEngine _restore;
        private readonly AccountManager _accountManager;
        private readonly ConfigurationManager _configurationManager;
        private readonly IFileBaseService _fileBase;

        private Action readyHandler;

        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }
        private IGameAccount account;
        public IGameAccount Account { get => account; set => SetProperty(ref account, value); }
        private string progress = "准备中";
        public string Progress { get => progress; set => SetProperty(ref progress, value); }
        private string progressDetails;
        public string ProgressDetails { get => progressDetails; set => SetProperty(ref progressDetails, value); }

        public PrepareGameViewModel(RestoreEngine restore, AccountManager accountManager, ConfigurationManager configurationManager, IFileBaseService fileBase)
        {
            _restore = restore;
            _accountManager = accountManager;
            _configurationManager = configurationManager;
            _fileBase = fileBase;
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public bool GotInstance(GameInstance instance, Action handler)
        {
            Instance = instance;
            readyHandler = handler;
            if (_accountManager.TryFindById(instance.BoundAccountId, out var account))
            {
                Account = account;
                return true;
            }
            else
            {
                return false;
            }
        }

        private readonly CancellationTokenSource source = new CancellationTokenSource();

        public void Cancel()
        {
            if (!source.IsCancellationRequested)
            {
                source.Cancel();
            }
        }

        public async Task PrepareAsync() => await PrepareAsync(source.Token);

        public async Task PrepareAsync(CancellationToken token)
        {
            await _restore.RestoreAsync(instance, (_, args) => UpdateProgressSafe(args), token);
        }

        private void UpdateProgressSafe(RestoreProgressEventArgs args)
        {
            _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, new DispatcherQueueHandler(() =>
            {
                switch (args.ProgressType)
                {
                    case RestoreProgressType.ErrorOccurred:
                        Progress = args.Error.ToString();
                        ProgressDetails = args.Exception != null ? args.Exception.ToString() : args.FileName;
                        break;

                    case RestoreProgressType.Core:
                        Progress = "补全游戏本体";
                        ProgressDetails = args.FileName;
                        break;

                    case RestoreProgressType.Download:
                        Progress = $"已下载 {args.Downloaded} 个文件，共 {args.TotalToDownload} 个";
                        ProgressDetails = args.FileName;
                        break;

                    case RestoreProgressType.Assets:
                        Progress = "补全资源文件";
                        ProgressDetails = string.Empty;
                        break;

                    case RestoreProgressType.Libraries:
                        Progress = "补全库文件";
                        ProgressDetails = string.Empty;
                        break;

                    case RestoreProgressType.AllCompleted:
                        Progress = "准备就绪";
                        ProgressDetails = string.Empty;
                        readyHandler();
                        break;
                }
            }));
        }

        [RelayCommand]
        public void Start()
        {
            var workingDir = new Uri($"poly-file://{instance.Id}/");
            var assetsRoot = new Uri("poly-file:///assets/");
            var nativesRoot = new Uri($"poly-file://{instance.Id}/natives/");
            var librariesRoot = new Uri($"poly-file:///libraries/");
            var jarFile = new Uri($"poly-file://{instance.Id}/client.jar");
            var indexFile = new Uri($"poly-file:///local/indexes/{instance.Metadata.CoreVersion}.json");
            if (_fileBase.TryReadAllText(indexFile, out var content))
            {
                var index = JsonConvert.DeserializeObject<Core.Models.Mojang.Index>(content);
                var configuration = new CompoundLaunchConfiguration(Instance.Configuration ?? new FileBasedLaunchConfiguration(), _configurationManager.Current.GameGlobals ?? new FileBasedLaunchConfiguration());
                var builder = new PlanetBlenderBuilder();
                builder
                    .WithJavaPath(configuration.JavaPath)
                    .WithMainClass(index.MainClass)
                    .WithWorkingDirectory(_fileBase.Locate(workingDir))
                    .WithGameArguments(index.Arguments.Game)
                    .WithJvmArguments(index.Arguments.Jvm)
                    .ConfigureStarship(configure =>
                {
                    configure.AddCrate("auth_player_name", Account.Nickname);
                    configure.AddCrate("version_name", Instance.Metadata.CoreVersion);
                    configure.AddCrate("game_directory", _fileBase.Locate(workingDir));
                    configure.AddCrate("assets_root", _fileBase.Locate(assetsRoot));
                    configure.AddCrate("assets_index_name", index.Assets);
                    configure.AddCrate("auth_uuid", account.UUID);
                    // this wont work
                    configure.AddCrate("auth_access_token", Guid.NewGuid().ToString());
                    // really?
                    configure.AddCrate("clientid", "00000000402b5328");
                    configure.AddCrate("user_type", "legacy");
                    configure.AddCrate("version_type", "release");
                    // rule os
                    // TODO: 目前只支持 windows
                    configure.AddCrate("os.name", "windows");
                    // TODO: 目前只支持 x86
                    configure.AddCrate("os.arch", "x86");
                    configure.AddCrate("os.version", Environment.OSVersion.Version.ToString());
                    // jvm
                    configure.AddCrate("natives_directory", _fileBase.Locate(nativesRoot));
                    configure.AddCrate("classpath", string.Join(';', index.Libraries.Where(x => x.Verfy()).Select(x => _fileBase.Locate(new Uri(librariesRoot, x.Downloads.Artifact.Path))).Append(_fileBase.Locate(jarFile))));
                    configure.AddCrate("launcher_name", "Polymerium");
                    configure.AddCrate("launcher_version", "Latest(maybe)");
                });
                var blender = builder.Build();
                blender.Start();
            }
            else
            {
                CriticalError("index.json 文件以错误的方式出现，这可能是还原过程不够彻底或发生未察觉的异常导致");
            }
        }

        private void CriticalError(string msg)
        {
            var dialog = new ContentDialog();
            dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
            dialog.Title = "关键错误";
            dialog.Content = msg;
            dialog.CloseButtonText = "晓得了";
            dialog.ShowAsync();
        }

        public void Dispose()
        {
            // application exit
            Cancel();
        }
    }
}