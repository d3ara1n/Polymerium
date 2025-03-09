using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines.Downloading;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Polymerium.Trident.Engines;

public class DownloadEngine(ILogger<DownloadEngine> logger, IHttpClientFactory factory)
    : IAsyncEnumerable<DownloadResult>
{
    private readonly ICollection<DownloadTask> tasks = new List<DownloadTask>();

    public int Count => tasks.Count;

    public IAsyncEnumerator<DownloadResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default) =>
        new DownloadEnumerator(tasks.DistinctBy(x => x.Target).ToList(),
            ((uint)Environment.ProcessorCount / 2) + 1, factory, logger,
            cancellationToken);

    public void AddTask(DownloadTask task) => tasks.Add(task);

    public class DownloadEnumerator : IAsyncEnumerator<DownloadResult>
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger _logger;
        private readonly CancellationToken _token;
        private readonly ConcurrentBag<InternalTask> bag;
        private readonly ConcurrentBag<DownloadResult> finished;
        private readonly int total;

        private int done;

        public DownloadEnumerator(
            ICollection<DownloadTask> tasks,
            uint maxWorkerCount,
            IHttpClientFactory factory,
            ILogger logger,
            CancellationToken token = default)
        {
            _factory = factory;
            _logger = logger;
            _token = token;
            bag = [.. tasks.Select((x, i) =>
                new InternalTask(x.Target, x.Source, x.Sha1, (uint)i, (uint)tasks.Count, x.Tag))];
            finished = [];
            total = bag.Count;

            var needed = Math.Min(maxWorkerCount, tasks.Count);

            for (var i = 0; i < needed; i++)
            {
                Thread worker = new(WorkWork)
                {
                    Name = $"Download Worker ({i + 1}/{needed})",
                    IsBackground = false,
                    Priority = ThreadPriority.BelowNormal
                };
                worker.Start(worker);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync()
        {
            while (done < total && !_token.IsCancellationRequested)
            {
                if (finished.TryTake(out var taken))
                {
                    Current = taken;
                    done++;
                    return ValueTask.FromResult(true);
                }

                Thread.Sleep(500);
            }

            return ValueTask.FromResult(false);
        }

        public DownloadResult Current { get; private set; } = null!;

        private void WorkWork(object? thread)
        {
            if (thread is Thread self)
            {
                using var client = _factory.CreateClient();
                while (bag.TryTake(out var taken))
                {
                    if (_token.IsCancellationRequested)
                    {
                        break;
                    }

                    var overwritten = false;
                    if (File.Exists(taken.Target))
                    {
                        if (taken.Sha1 == null)
                        {
                            finished.Add(new DownloadResult(taken.Target, taken.Source, taken.Sha1, taken.Index,
                                taken.Total, DownloadResult.DownloadResultState.Remained, taken.Tag));
                            continue;
                        }

                        var reader = File.OpenRead(taken.Target);
                        var hash = BitConverter.ToString(SHA1.HashData(reader)).Replace("-", string.Empty);
                        reader.Dispose();
                        if (hash.Equals(taken.Sha1, StringComparison.InvariantCultureIgnoreCase))
                        {
                            finished.Add(new DownloadResult(taken.Target, taken.Source, taken.Sha1, taken.Index,
                                taken.Total, DownloadResult.DownloadResultState.Remained, taken.Tag));
                            continue;
                        }

                        overwritten = true;
                    }

                    try
                    {
                        var dir = Path.GetDirectoryName(taken.Target);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        using var stream = client.GetStreamAsync(taken.Source, _token).GetAwaiter().GetResult();
                        using var writer = File.Create(taken.Target);
                        // NOTE: 转换到异步，在同步来做到取消，否则该线程不会退出
                        stream.CopyToAsync(writer, _token).Wait(_token);
                        stream.Flush();
                        finished.Add(new DownloadResult(taken.Target, taken.Source, taken.Sha1, taken.Index,
                            taken.Total,
                            overwritten
                                ? DownloadResult.DownloadResultState.Overwritten
                                : DownloadResult.DownloadResultState.FreshNew, taken.Tag));
                    }
                    catch (Exception e)
                    {
                        finished.Add(new DownloadResult(taken.Target, taken.Source, taken.Sha1, taken.Index,
                            taken.Total,
                            DownloadResult.DownloadResultState.Broken, taken.Tag));
                        _logger.LogError(e, "File failed to download or write: {target}({source})", taken.Target,
                            taken.Source.AbsoluteUri);
                    }
                }

                _logger.LogInformation("Worker thread retired: {name}({id})", self.Name, self.ManagedThreadId);
            }
        }

        private record InternalTask(string Target, Uri Source, string? Sha1, uint Index, uint Total, object? Tag);
    }
}