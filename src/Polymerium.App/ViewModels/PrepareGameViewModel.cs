using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Polymerium.Abstractions;
using Polymerium.Core.Engines;
using Polymerium.Core.Engines.Restoring;

namespace Polymerium.App.ViewModels
{
    public sealed class PrepareGameViewModel : ObservableObject, IDisposable
    {
        private DispatcherQueue _dispatcher;
        private RestoreEngine _restore;


        private Action readyHandler;

        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }
        private string progress = "准备中";
        public string Progress { get => progress; set => SetProperty(ref progress, value); }
        private string progressDetails;
        public string ProgressDetails { get => progressDetails; set => SetProperty(ref progressDetails, value); }
        public PrepareGameViewModel(RestoreEngine restore)
        {
            _restore = restore;
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }
        public void GotInstance(GameInstance instance, Action handler)
        {
            Instance = instance;
            readyHandler = handler;
        }

        private CancellationTokenSource source = new CancellationTokenSource();
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
                        Progress = $"下载第 {args.Downloaded} 共 {args.TotalToDownload} 个文件";
                        ProgressDetails = args.FileName;
                        break;
                    case RestoreProgressType.Assets:
                        Progress = "补全资源文件";
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
