using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ObservableCollections;
using Polymerium.App.Models;
using Trident.Abstractions.Extensions;
using Trident.Core.Engines.Launching;
using Trident.Core.Services;
using Trident.Core.Services.Instances;

namespace Polymerium.App.Services;

public class ScrapService : IDisposable
{
    public const int CAPACITY = 9527;
    private readonly Dictionary<string, ObservableFixedSizeRingBuffer<ScrapModel>> _buffers = [];

    #region Injected

    private readonly InstanceManager _instanceManager;

    #endregion

    public ScrapService(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;

        instanceManager.InstanceLaunching += InstanceManagerOnInstanceLaunching;
    }

    #region IDisposable Members

    public void Dispose() =>
        _instanceManager.InstanceLaunching -= InstanceManagerOnInstanceLaunching;

    #endregion

    private void InstanceManagerOnInstanceLaunching(object? _, LaunchTracker e)
    {
        if (!_buffers.TryGetValue(e.Key, out var buffer))
        {
            buffer = new(CAPACITY);
            _buffers.Add(e.Key, buffer);
        }

        e.ScrapStream.Subscribe(
                x =>
                {
                    var appended = AppendToModel(x, buffer.LastOrDefault());
                    buffer.AddLast(appended);
                },
                () =>
                {
                    _buffers.Remove(e.Key);
                }
            )
            .DisposeWith(e);
    }

    public bool TryGetBuffer(
        string key,
        [MaybeNullWhen(false)] out ObservableFixedSizeRingBuffer<ScrapModel> buffer
    ) => _buffers.TryGetValue(key, out buffer);

    public static ScrapModel AppendToModel(Scrap item, ScrapModel? last)
    {
        if (item is { Level: { } level, Thread: { } thread, Sender: { } sender })
        {
            return new(item.Message, level, item.Date, item.Time, thread, sender);
        }
        else
        {
            return new(item.Message, last?.Level ?? ScrapLevel.Information, null, null, null, null);
        }
    }
}
