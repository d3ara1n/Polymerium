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
    private readonly Dictionary<string, (Guid ActivityId, ObservableFixedSizeRingBuffer<ScrapModel> Buffer)> _buffers = [];

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
                               foreach (var group in batch.GroupBy(x => (x.Key, x.ActivityId)))
                               {
                                   if (!_buffers.TryGetValue(group.Key.Key, out var entry)
                                       || entry.ActivityId != group.Key.ActivityId)
                                   {
                                       continue;
                                   }

                                   var buffer = entry.Buffer;
                                   var last = buffer.LastOrDefault();
                                   foreach (var item in group)
                                   {
                                       last = AppendToModel(item.Scrap, last);
                                       buffer.AddLast(last);
                                   }
                               }
                           })));

        _subscriptions.Add(_instanceManager
                          .Activities.OfType<InstanceActivity.Running>()
                          .Subscribe(activity => Dispatcher.UIThread.Post(() => Track(activity))));
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

    private void Track(InstanceActivity.Running activity)
    {
        var exists = _buffers.TryGetValue(activity.Key, out var entry);
        if (activity.IsCompleted)
        {
            if (exists && entry.ActivityId == activity.Id)
            {
                _buffers.Remove(activity.Key);
            }
        }
        else if (!exists || entry.ActivityId != activity.Id)
        {
            _buffers[activity.Key] = (activity.Id, [with(CAPACITY)]);
        }
    }

    public bool TryGetBuffer(string key, [MaybeNullWhen(false)] out ObservableFixedSizeRingBuffer<ScrapModel> buffer)
    {
        if (_buffers.TryGetValue(key, out var entry))
        {
            buffer = entry.Buffer;
            return true;
        }

        buffer = null;
        return false;
    }

    public static ScrapModel AppendToModel(Scrap item, ScrapModel? last)
    {
        if (item is { Level: { } level, Thread: { } thread, Sender: { } sender })
        {
            return new(item.Message, level, item.Date, item.Time, thread, sender);
        }

        return new(item.Message, last?.Level ?? ScrapLevel.Information, null, null, null, null);
    }
}
