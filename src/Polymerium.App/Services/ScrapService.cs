using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ObservableCollections;
using Polymerium.App.Models;
using Polymerium.Trident.Engines.Launching;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Extensions;

namespace Polymerium.App.Services;

public class ScrapService : IDisposable
{
    private readonly Dictionary<string, ObservableFixedSizeRingBuffer<ScrapModel>> _buffers = new();

    #region Injected

    private readonly InstanceManager _instanceManager;

    #endregion

    public ScrapService(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;

        instanceManager.InstanceLaunching += InstanceManagerOnInstanceLaunching;
    }

    public void Dispose()
    {
        _instanceManager.InstanceLaunching -= InstanceManagerOnInstanceLaunching;
    }

    private void InstanceManagerOnInstanceLaunching(object? _, LaunchTracker e)
    {
        if (!_buffers.TryGetValue(e.Key, out var buffer))
        {
            buffer = new ObservableFixedSizeRingBuffer<ScrapModel>(9527);
            _buffers.Add(e.Key, buffer);
        }

        e
           .ScrapStream.Subscribe(x =>
                                  {
                                      if (x is
                                          {
                                              Level: { } level, Time: { } time, Thread: { } thread, Sender: { } sender
                                          })
                                      {
                                          buffer.AddLast(new ScrapModel(x.Message, level, time, thread, sender));
                                      }
                                      else
                                      {
                                          if (buffer.Count > 0)
                                              buffer[^1].Message += "\n" + x.Message;
                                          else
                                              buffer.AddLast(new ScrapModel(x.Message,
                                                                            ScrapLevel.Information,
                                                                            DateTimeOffset.Now,
                                                                            "Unknown",
                                                                            "Unknown"));
                                      }
                                  },
                                  () =>
                                  {
                                      _buffers.Remove(e.Key);
                                  })
           .DisposeWith(e);
    }

    public bool TryGetBuffer(string key, [MaybeNullWhen(false)] out ObservableFixedSizeRingBuffer<ScrapModel> buffer) =>
        _buffers.TryGetValue(key, out buffer);
}