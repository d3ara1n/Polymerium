using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Common.Collections;

namespace Polymerium.App.Models;

public class IncrementalFactorySource<T>(Func<uint, uint, CancellationToken, Task<IEnumerable<T>>> factory)
    : IIncrementalSource<T>
{
    public async Task<IEnumerable<T>> GetPagedItemsAsync(
        int page,
        int limit,
        CancellationToken token = default
    )
    {
        return await factory((uint)(page * limit), (uint)limit, token);
    }
}