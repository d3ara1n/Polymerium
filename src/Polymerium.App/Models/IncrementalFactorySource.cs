using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Common.Collections;

namespace Polymerium.App.Models;

public class IncrementalFactorySource<T> : IIncrementalSource<T>
{
    private readonly Func<uint, uint, CancellationToken, Task<IEnumerable<T>>> _factory;

    public IncrementalFactorySource(
        Func<uint, uint, CancellationToken, Task<IEnumerable<T>>> factory
    )
    {
        _factory = factory;
    }

    public async Task<IEnumerable<T>> GetPagedItemsAsync(
        int pageIndex,
        int pageSize,
        CancellationToken token = default
    )
    {
        return await _factory((uint)(pageIndex * pageSize), (uint)pageSize, token);
    }
}
