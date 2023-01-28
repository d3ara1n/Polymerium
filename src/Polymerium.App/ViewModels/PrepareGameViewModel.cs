using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Services;
using Polymerium.Core.Engines;
using Polymerium.Core.Engines.Restoring;

namespace Polymerium.App.ViewModels
{
    public sealed class PrepareGameViewModel : ObservableObject, IDisposable
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly RestoreEngine _restore;
        private readonly AccountManager _accountManager;


        private Action readyHandler;

        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }
        private IGameAccount account;
        public IGameAccount Account { get => account; set => SetProperty(ref account, value); }
        private string progress = "准备中";
        public string Progress { get => progress; set => SetProperty(ref progress, value); }
        private string progressDetails;
        public string ProgressDetails { get => progressDetails; set => SetProperty(ref progressDetails, value); }
        public PrepareGameViewModel(RestoreEngine restore, AccountManager accountManager)
        {
            _restore = restore;
            _accountManager = accountManager;
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }
        public bool GotInstance(GameInstance instance, Action handler)
        {
            Instance = instance;
            readyHandler = handler;
            if(_accountManager.TryFindById(instance.BoundAccountId, out var account))
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

        public void Dispose()
        {
            // application exit
            Cancel();
        }
    }
}
