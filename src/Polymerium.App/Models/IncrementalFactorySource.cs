using CommunityToolkit.Common.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
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
            int page,
            int limit,
            CancellationToken token = default
        )
        {
            return await _factory((uint)(page * limit), (uint)limit, token);
        }
    }
}
