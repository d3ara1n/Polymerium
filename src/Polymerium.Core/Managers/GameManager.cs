using Polymerium.Abstractions;
using Polymerium.Core.Engines;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Managers.GameModels;
using Polymerium.Core.Stars;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Polymerium.Core.Managers;

// 托管非闲置实例状态，例如还原中的实例。提供接口以供获取某个实例的状态
// 当实例还原时进入托管，启动后根据是否托管来决定是立即释放还是等待其进程结束
public class GameManager
{
    private readonly RestoreEngine _restore;
    private readonly DownloadEngine _download;
    private readonly AssetManager _assetManager;
    private readonly IFileBaseService _fileBase;

    public GameManager(
        RestoreEngine restore,
        DownloadEngine download,
        AssetManager assetManager,
        IFileBaseService fileBase
    )
    {
        _restore = restore;
        _download = download;
        _assetManager = assetManager;
        _fileBase = fileBase;
    }

    private List<PrepareTracker> preparings = new();
    private List<RunTracker> runnings = new();

    public void LaunchFireForget(PlanetaryEngineBuilder builder)
    {
        var blender = builder.Build();
        blender.LaunchFireForget();
    }

    public bool IsRunning(string id, out RunTracker? tracker)
    {
        tracker = runnings.FirstOrDefault(x => x.Instance.Id == id);
        return tracker != null;
    }

    public PrepareTracker Prepare(GameInstance instance, Action<int?> callback)
    {
        // throw if been preparing
        var tracker = new PrepareTracker(instance) { UpdateCallback = callback };
        tracker.Task = Task.Run(() => PrepareInternal(tracker));
        preparings.Add(tracker);
        return tracker;
    }

    private void PrepareInternal(PrepareTracker tracker)
    {
        var pipeline = _restore.ProducePipeline();
        if (
            pipeline.Pump(tracker.Instance, tracker.TokenSource.Token)
            && pipeline.HandleWaste<RestoreContext>(out var waste)
        )
        {
            var count = 0;
            tracker.UpdateCallback?.Invoke(0);
            // wait to download
            var group = new DownloadTaskGroup() { Token = tracker.TokenSource.Token };
            if (tracker.TokenSource.IsCancellationRequested) return;
            foreach (var task in waste!.Tasks)
            {
                var target = _fileBase.Locate(task.Target);
                if (task.Source.Scheme == "poly-file")
                {
                    var source = _fileBase.Locate(task.Source);
                    var dir = Path.GetDirectoryName(target);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir!);
                    File.Copy(source, target, true);
                    tracker.UpdateCallback?.Invoke((++count) * 100 / waste.Tasks.Count);
                }
                else
                {
                    if (group.TryAdd(task.Source.AbsoluteUri, target, out var post))
                    {
                        post!.CompletedCallback = (_, s) =>
                        {
                            if (s)
                            {
                                task.PostAction?.Invoke(task.Source);
                                tracker.UpdateCallback?.Invoke((++count) * 100 / waste.Tasks.Count);
                            }
                        };
                    }
                }
            }
            if (tracker.TokenSource.IsCancellationRequested) return;
            _download.Enqueue(group);
            group.Wait();
            tracker.UpdateCallback?.Invoke(null);
            if (group.DownloadedCount == group.TotalCount)
            {
                try
                {
                    _assetManager.DeployRenewableAssets(tracker.Instance, waste.MergedStates);
                    tracker.FinishCallback?.Invoke(true, null, null, null);
                }
                catch (Exception e)
                {
                    tracker.FinishCallback?.Invoke(false, PrepareError.ExceptionOcurred, e, null);
                }
            }
            else
            {
                // download failure
                tracker.FinishCallback?.Invoke(false, PrepareError.DownloadFailure, null, null);
            }
        }
        else if (pipeline.HandleError(out var error))
        {
            tracker.FinishCallback?.Invoke(false, PrepareError.PrepareFailure, null, error);
        }
        else if (pipeline.HandleException(out var e))
        {
            tracker.FinishCallback?.Invoke(false, PrepareError.ExceptionOcurred, e, null);
        }
        else if (tracker.TokenSource.IsCancellationRequested)
        {
            tracker.FinishCallback?.Invoke(false, PrepareError.Canceled, null, null);
        }
        else
        {
            tracker.FinishCallback?.Invoke(false, PrepareError.Unknown, null, null);
        }
        preparings.Remove(tracker);
        // FinishCallback 的传参没有 rustlike enum 多难受哇
    }

    public bool IsPreparing(string id, out PrepareTracker? tracker)
    {
        tracker = preparings.FirstOrDefault(x => x.Instance.Id == id);
        return tracker != null;
    }
}
