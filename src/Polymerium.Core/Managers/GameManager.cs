using Polymerium.Abstractions;
using Polymerium.Core.Engines;
using Polymerium.Core.Managers.GameModels;
using Polymerium.Core.Stars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Core.Managers;

// 托管非闲置实例状态，例如还原中的实例。提供接口以供获取某个实例的状态
// 当实例还原时进入托管，启动后根据是否托管来决定是立即释放还是等待其进程结束
public class GameManager
{
    private readonly RestoreEngine _restore;

    public GameManager(RestoreEngine restore)
    {
        _restore = restore;
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

    public PrepareTracker Prepare(GameInstance instance, Action<int?, bool?> callback)
    {
        // throw if been preparing
        var tracker = new PrepareTracker(instance) { Callback = callback };
        tracker.Task = Task.Run(() => PrepareInternal(tracker));
        preparings.Add(tracker);
        return tracker;
    }

    private void PrepareInternal(PrepareTracker tracker)
    {
        var pipeline = _restore.ProducePipeline();
        // setup pipeline
        //pipeline.Pump(tracker.Instance, tracker.TokenSource.Token);
        tracker.Callback?.Invoke(null, null);
        Thread.Sleep(1500);
        tracker.Callback?.Invoke(0, null);
        Thread.Sleep(1500);
        tracker.Callback?.Invoke(15, null);
        Thread.Sleep(1500);
        tracker.Callback?.Invoke(50, null);
        Thread.Sleep(1500);
        tracker.Callback?.Invoke(100, null);
        Thread.Sleep(1500);
        tracker.Callback?.Invoke(null, true);
        // call back to update
    }

    public bool IsPreparing(string id, out PrepareTracker? tracker)
    {
        tracker = preparings.FirstOrDefault(x => x.Instance.Id == id);
        return tracker != null;
    }
}
