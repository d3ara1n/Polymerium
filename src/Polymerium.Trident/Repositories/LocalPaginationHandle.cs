using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Repositories
{
    public class LocalPaginationHandle<T>(IReadOnlyList<T> all, uint pageSize) : IPaginationHandle<T>
    {
        #region IPaginationHandle<T> Members

        public uint PageSize => pageSize;
        public uint PageIndex { get; set; }
        public ulong TotalCount => (ulong)all.Count;

        public Task<IEnumerable<T>> FetchAsync(CancellationToken token)
        {
            var index = PageIndex * PageSize;
            var rv = all.Skip((int)index).Take((int)PageSize);
            return Task.FromResult(rv.AsEnumerable());
        }

        #endregion
    }
}
