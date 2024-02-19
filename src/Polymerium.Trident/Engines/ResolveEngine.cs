using Polymerium.Trident.Engines.Resolving;
using Polymerium.Trident.Services;
using System.Collections.Concurrent;
using Trident.Abstractions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines
{
    public class ResolveEngine(RepositoryAgent agent) : IAsyncEngine<ResolveResult>
    {
        private readonly IList<Attachment> attachments = new List<Attachment>();
        private Filter? repoFilter;

        public IAsyncEnumerator<ResolveResult> GetAsyncEnumerator(CancellationToken token = default)
        {
            ResolveContext context = new(attachments);
            return new ResolveEngineEnumerator(context, agent, repoFilter ?? Filter.EMPTY, token);
        }

        public void AddAttachment(Attachment attachment)
        {
            attachments.Add(attachment);
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
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (tasks.TryDequeue(out Task<ResolveResult>? task))
                {
                    ResolveResult package = await task;
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

            private static async Task<ResolveResult> ResolveAsync(Attachment attachment, RepositoryAgent agent,
                Filter filter,
                CancellationToken token = default)
            {
                if (token.IsCancellationRequested)
                {
                    return new ResolveResult(attachment, new OperationCanceledException());
                }

                try
                {
                    Package package = await agent.ResolveAsync(attachment.Label, attachment.ProjectId,
                        attachment.VersionId,
                        filter, token);
                    return new ResolveResult(attachment, package);
                }
                catch (Exception e)
                {
                    return new ResolveResult(attachment, e);
                }
            }
        }
    }
}