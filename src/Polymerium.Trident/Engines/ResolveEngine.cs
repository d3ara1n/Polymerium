using System.Collections.Concurrent;
using PackageUrl;
using Polymerium.Trident.Engines.Resolving;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Engines;

public class ResolveEngine(RepositoryAgent agent) : IAsyncEngine<ResolveResult>
{
    private readonly IList<string> attachments = new List<string>();
    private Filter? repoFilter;

    public IAsyncEnumerator<ResolveResult> GetAsyncEnumerator(CancellationToken token = default)
    {
        var context = new ResolveContext(attachments);
        return new ResolveEngineEnumerator(context, agent, repoFilter ?? Filter.EMPTY, token);
    }

    public void AddAttachment(string purl)
    {
        attachments.Add(purl);
    }

    public void SetFilter(Filter filter)
    {
        repoFilter = filter;
    }

    public class ResolveEngineEnumerator(
        ResolveContext context,
        RepositoryAgent agent,
        Filter filter,
        CancellationToken token) : IAsyncEnumerator<ResolveResult>
    {
        private readonly ConcurrentQueue<Task<ResolveResult>> tasks =
            new(context.Attachments.Select(x => ResolveAsync(x, agent, filter, token)));


        public async ValueTask<bool> MoveNextAsync()
        {
            if (token.IsCancellationRequested) return false;
            if (tasks.TryDequeue(out var task))
            {
                var package = await task;
                Current = package;
                return true;
            }

            return false;
        }

        public ResolveResult Current { get; private set; } = null!;

        public ValueTask DisposeAsync()
        {
            // TODO 在此释放托管资源
            return ValueTask.CompletedTask;
        }

        private static async Task<ResolveResult> ResolveAsync(string attachment, RepositoryAgent agent, Filter filter,
            CancellationToken token = default)
        {
            try
            {
                var purl = new PackageURL(attachment);
                if (purl.Type == null) throw new BadFormatException(attachment, "label");
                if (purl.Name == null) throw new BadFormatException(attachment, "name");
                var label = purl.Type;
                var name = purl.Name;
                var version = purl.Version;
                var package = await agent.ResolveAsync(label, name, version, filter, token);
                return new ResolveResult(attachment, package);
            }
            catch (Exception e)
            {
                return new ResolveResult(attachment, e);
            }
        }
    }
}