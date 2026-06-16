using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Polymerium.Avalonia.Models;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     <see cref="InstanceManager" /> 四个事件（Installing/Updating/Deploying/Launching）的唯一聚合者。
///     把异构 tracker 的生命周期 + 归一化进度，压平成统一的 <see cref="InstanceStateSnapshot" /> 变化流。
/// </summary>
/// <remarks>
///     <para>这是「订阅 + 解析 + 路由」的薄层，<b>不持冗余状态表</b>。状态的唯一真源仍是
///     <see cref="InstanceManager" /> 的 active tracker 字典；Snapshot 由「事件 + 实时查 IsTracking」即时合成。</para>
///     <para>实体服务不抽接口：消费方直接依赖具体类。生命周期 = 应用（Startup 注册 Singleton）。</para>
/// </remarks>
public class InstanceStateAggregator
{
    private readonly InstanceManager _instanceManager;

    // 内部扁平事件管道（非 SourceCache）：(key, snapshot) 语义为「此 key 当前态」；
    // snapshot == null 表示该 key 回到 Idle（Remove）。真源是 InstanceManager._trackers，
    // 此 Subject 仅做事件分发，不维护 key→state 映射。
    private readonly Subject<(string Key, InstanceStateSnapshot? Snapshot)> _stream = new();

    /// <summary>
    ///     全量变化流。QuickBar Active 区、Phase 3 的 Sink 通过 <c>.Bind(out var view)</c> 订阅。
    /// </summary>
    public IObservable<IChangeSet<InstanceStateSnapshot, string>> StateChangeStream =>
        ObservableChangeSet.Create<InstanceStateSnapshot, string>(
            cache =>
            {
                // 把内部管道翻译成 cache 的 AddOrUpdate / Remove，让 DynamicData 的 .Bind/.Sort 可用。
                return _stream.Subscribe(kv =>
                                {
                                    if (kv.Snapshot is null)
                                    {
                                        cache.Remove(kv.Key);
                                    }
                                    else
                                    {
                                        cache.AddOrUpdate(kv.Snapshot);
                                    }
                                });
            },
            x => x.Key);

    /// <summary>
    ///     单实例流（InstancePageModelBase 用）。Subscribe 即自动收到当前态，无需先查询。
    /// </summary>
    /// <remarks>
    ///     <c>Defer</c> 在每次 RefCount 从 0→1 时实时查 <see cref="InstanceManager.IsTracking" /> 立即吐当前
    ///     snapshot；<c>Replay(1).RefCount()</c> 让后到的订阅者复用缓存、断开无人订阅时释放底层。
    ///     tracker 完成（Finished/Faulted）时推 <c>null</c>，消费方据此把页面状态回 Idle。
    /// </remarks>
    public IObservable<InstanceStateSnapshot?> Watch(string key)
    {
        return Observable.Defer(() =>
                   {
                       var current = _instanceManager.IsTracking(key, out var tracker)
                                         ? ToSnapshot(tracker)
                                         : null;
                       return Observable.Return(current);
                   })
                  .Concat(_stream.Where(x => x.Key == key).Select(x => x.Snapshot))
                  .Replay(1)
                  .RefCount();
    }

    /// <summary>
    ///     构造即开始聚合。Aggregator 与 InstanceManager 同为应用级单例、生命周期一致，无需解绑事件。
    /// </summary>
    public InstanceStateAggregator(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;

        instanceManager.InstanceInstalling += OnTracker;
        instanceManager.InstanceUpdating += OnTracker;
        instanceManager.InstanceDeploying += OnTracker;
        instanceManager.InstanceLaunching += OnTracker;
    }

    private void OnTracker<T>(object? sender, T tracker)
        where T : TrackerBase
    {
        // 1) 立即推初始 snapshot（含子类 OnStart 报告的初始 Progress）
        _stream.OnNext((tracker.Key, ToSnapshot(tracker)));

        // 2) 进度节流更新（沿用现状 1s 策略）。Sample 期间值被合并，首个状态已由步骤 1 即时反映。
        tracker.ProgressChanged
              .Sample(TimeSpan.FromSeconds(1))
              .Subscribe(_ => _stream.OnNext((tracker.Key, ToSnapshot(tracker))))
              .DisposeWith(tracker);

        // 3) 完成信号 → Remove。StateUpdated 是 C# event，直接 += 订阅；tracker 完成后 Dispose。
        void OnStateUpdated(TrackerBase sender, TrackerState state)
        {
            if (state is TrackerState.Finished or TrackerState.Faulted)
            {
                _stream.OnNext((sender.Key, null));
                sender.StateUpdated -= OnStateUpdated;
            }
        }
        tracker.StateUpdated += OnStateUpdated;
    }

    private static InstanceStateSnapshot ToSnapshot(TrackerBase tracker) =>
        new(tracker.Key, tracker.Kind, tracker.Progress, tracker);
}
