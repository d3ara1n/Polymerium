using System;
using System.Reactive.Linq;
using DynamicData;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     把 <see cref="InstanceManager.Activities" /> 整形成 UI 需要的两种消费形态：
///     全量变化集（列表/通知）与单实例流（页面）。
/// </summary>
/// <remarks>
///     <para>
///         这是「整形 + 路由」的薄层，<b>不持状态表</b>：活动的唯一真源是
///         <see cref="InstanceManager" />，本类只做流的变形。
///     </para>
///     <para>实体服务不抽接口：消费方直接依赖具体类。生命周期 = 应用（Startup 注册 Singleton）。</para>
/// </remarks>
public class InstanceStateAggregator
{
    private readonly InstanceManager _instanceManager;

    public InstanceStateAggregator(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;

        // NOTE: 活动流已自带「开始/进度/终态」三类值，缓存只需按 Key 增改删；终态即移除，
        //  故消费方收到 Remove 时拿到的 Current 就是终态快照。
        StateChangeStream = ObservableChangeSet
                           .Create<InstanceActivity, string>(cache => _instanceManager.Activities.Subscribe(activity =>
                            {
                                if (activity.IsCompleted)
                                {
                                    // 先把终态值写进缓存再移除，让 Remove 携带终态而非上一帧的运行态。
                                    cache.AddOrUpdate(activity);
                                    cache.Remove(activity.Key);
                                }
                                else
                                {
                                    cache.AddOrUpdate(activity);
                                }
                            }),
                                                             x => x.Key)
                           .RefCount();
    }

    /// <summary>
    ///     全量变化流。QuickBar Active 区与各 Sink 通过它观察所有实例的活动。
    ///     用 <c>RefCount</c> 确保多个消费者共享同一个底层 cache 实例。
    /// </summary>
    public IObservable<IChangeSet<InstanceActivity, string>> StateChangeStream { get; }

    /// <summary>
    ///     单实例活动流。Subscribe 即自动收到当前态，无需先查询；首帧为 <c>null</c> 表示当下空闲。
    /// </summary>
    /// <remarks>
    ///     终态活动原样流出（不改写为 null），消费方靠 <see cref="InstanceActivity.IsCompleted" />
    ///     判定回 Idle——否则它们拿不到失败原因等终态信息。
    ///     <c>Defer</c> 在每次 RefCount 从 0→1 时实时查当前活动立即吐一帧；<c>Replay(1).RefCount()</c>
    ///     让后到的订阅者复用缓存、无人订阅时释放底层。
    /// </remarks>
    public IObservable<InstanceActivity?> Watch(string key) =>
        Observable
           .Defer(() => Observable.Return(_instanceManager.ActivityOf(key)))
           .Concat(_instanceManager.Activities.Where(x => x.Key == key).Select(InstanceActivity? (x) => x))
           .Replay(1)
           .RefCount();
}
