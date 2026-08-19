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
using TridentCore.Abstractions.Extensions;
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

    public ScrapService(InstanceManager instanceManager) => _instanceManager = instanceManager;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _instanceManager.InstanceLaunching += InstanceManagerOnInstanceLaunching;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _instanceManager.InstanceLaunching -= InstanceManagerOnInstanceLaunching;
        return ValueTask.CompletedTask;
    }

    private void InstanceManagerOnInstanceLaunching(object? _, LaunchTracker e)
    {
        if (!_buffers.TryGetValue(e.Key, out var buffer))
        {
            buffer = [with(CAPACITY)];
            _buffers.Add(e.Key, buffer);
        }

        // NOTE: 游戏输出到达于后台线程，绑定到 UI 的集合若在该线程上变更，VirtualizingStackPanel 可能在布局期间读到并发收缩的列表而索引越界（POLYMERIUM-2E），故攒批后投递到 UI 线程再写入。
        e
           .ScrapStream
           .Buffer(TimeSpan.FromMilliseconds(FLUSH_INTERVAL))
           .Where(batch => batch.Count > 0)
           .Subscribe(batch => Dispatcher.UIThread.Post(() =>
            {
                var last = buffer.LastOrDefault();
                foreach (var scrap in batch)
                {
                    last = AppendToModel(scrap, last);
                    buffer.AddLast(last);
                }
            }),
            () =>
            {
                _buffers.Remove(e.Key);
            })
           .DisposeWith(e);
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
