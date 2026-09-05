using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ObservableCollections;
using Polymerium.Avalonia.Models;
using TridentCore.Abstractions.Lifetimes;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.Services;

public class ScrapService : ILifetimeService
{
    public const int CAPACITY = 9527;
    public const int FLUSH_INTERVAL = 100;
    private readonly Dictionary<string, ObservableFixedSizeRingBuffer<ScrapModel>> _buffers = [];

    #region Injected

    private readonly InstanceManager _instanceManager;

    #endregion

    private readonly List<IDisposable> _subscriptions = [];

    public ScrapService(InstanceManager instanceManager) => _instanceManager = instanceManager;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        // 输出流的生命周期属于 manager，订阅一次即覆盖所有实例的所有次启动。
        // WARNING: 游戏输出到达于后台线程，绑定到 UI 的集合若在该线程上变更，VirtualizingStackPanel
        //  可能在布局期间读到并发收缩的列表而索引越界（POLYMERIUM-2E），故攒批后投递到 UI 线程再写入。
        _subscriptions.Add(_instanceManager
                          .Scraps.Buffer(TimeSpan.FromMilliseconds(FLUSH_INTERVAL))
                          .Where(batch => batch.Count > 0)
                          .Subscribe(batch => Dispatcher.UIThread.Post(() =>
                           {
                               foreach (var group in batch.GroupBy(x => x.Key))
                               {
                                   var buffer = BufferOf(group.Key);
                                   var last = buffer.LastOrDefault();
                                   foreach (var item in group)
                                   {
                                       last = AppendToModel(item.Scrap, last);
                                       buffer.AddLast(last);
                                   }
                               }
                           })));

        // 缓冲区随本次运行结束而弃置，下次启动从空白开始。
        // NOTE: 与写入同在 UI 线程摘除，_buffers 因此只被单线程触碰。
        _subscriptions.Add(_instanceManager
                          .Activities.Where(x => x is InstanceActivity.Running { IsCompleted: true })
                          .Subscribe(x => Dispatcher.UIThread.Post(() => _buffers.Remove(x.Key))));
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        return ValueTask.CompletedTask;
    }

    private ObservableFixedSizeRingBuffer<ScrapModel> BufferOf(string key)
    {
        if (!_buffers.TryGetValue(key, out var buffer))
        {
            buffer = [with(CAPACITY)];
            _buffers.Add(key, buffer);
        }

        return buffer;
    }

    public bool TryGetBuffer(string key, [MaybeNullWhen(false)] out ObservableFixedSizeRingBuffer<ScrapModel> buffer) =>
        _buffers.TryGetValue(key, out buffer);

    public static ScrapModel AppendToModel(Scrap item, ScrapModel? last)
    {
        if (item is { Level: { } level, Thread: { } thread, Sender: { } sender })
        {
            return new(item.Message, level, item.Date, item.Time, thread, sender);
        }

        return new(item.Message, last?.Level ?? ScrapLevel.Information, null, null, null, null);
    }
}
