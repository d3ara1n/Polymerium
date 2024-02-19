using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Services;
using System.Collections;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines
{
    public class DeployEngine(
        IServiceProvider provider,
        ILoggerFactory loggerFactory,
        TridentContext trident,
        IHttpClientFactory clientFactory,
        JsonSerializerOptions options)
        : IEngine<StageBase>
    {
        private DeployContext? context;

        public IEnumerator<StageBase> GetEnumerator()
        {
            ArgumentNullException.ThrowIfNull(context);
            return new DeployEngineEnumerator(context, provider, loggerFactory, clientFactory);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetProfile(string key, Metadata metadata, ICollection<string> keywords,
            CancellationToken token = default)
        {
            context = new DeployContext(trident, key, metadata, keywords, options, token);
        }

        public class DeployEngineEnumerator(
            DeployContext context,
            IServiceProvider provider,
            ILoggerFactory loggerFactory,
            IHttpClientFactory factory)
            : IEnumerator<StageBase>
        {
            public bool MoveNext()
            {
                if (context.Token.IsCancellationRequested || context.IsAborted || context.IsFinished)
                {
                    return false;
                }

                // trident.artifact.json
                // Deploy 包含以下过程：
                // Build artifact:
                //   Install game, Process loaders, Resolve attachments
                // Build transient data:
                //   Run processors
                // Solidify transient data:
                //   Download libraries, Download & link attachments, Restore assets
                if (context.Transient != null)
                {
                    if (context.IsSolidified)
                    {
                        return false;
                    }

                    // solidify
                    Current = SolidifyTransient();
                    return true;
                }

                if (context.Artifact != null)
                {
                    // build transient
                    Current = BuildTransient();
                    return true;
                }

                if (context.ArtifactBuilder != null)
                {
                    if (context.IsAttachmentResolved)
                    {
                        // build artifact
                        Current = BuildArtifact();
                        return true;
                    }

                    if (context.IsLoaderProcessed)
                    {
                        // resolve attachment
                        Current = ResolveAttachment();
                        return true;
                    }

                    if (context.IsGameInstalled)
                    {
                        // process loaders
                        Current = ProcessLoader();
                        return true;
                    }

                    // install game
                    Current = InstallVanilla();
                    return true;
                }

                Current = CheckArtifact();
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public StageBase Current { get; private set; } = null!;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // do nothing
            }

            private CheckArtifactStage CheckArtifact()
            {
                return CreateStage(() => new CheckArtifactStage());
            }

            private BuildArtifactStage BuildArtifact()
            {
                return CreateStage(() => new BuildArtifactStage());
            }

            private InstallVanillaStage InstallVanilla()
            {
                return CreateStage(() => new InstallVanillaStage(factory));
            }

            private ResolveAttachmentStage ResolveAttachment()
            {
                ResolveEngine engine = provider.GetRequiredService<ResolveEngine>();
                return CreateStage(() => new ResolveAttachmentStage(engine));
            }

            private ProcessLoaderStage ProcessLoader()
            {
                return CreateStage(() => new ProcessLoaderStage(factory));
            }

            private BuildTransientStage BuildTransient()
            {
                return CreateStage(() => new BuildTransientStage(factory));
            }

            private SolidifyTransientStage SolidifyTransient()
            {
                DownloadEngine downloader = provider.GetRequiredService<DownloadEngine>();
                return CreateStage(() => new SolidifyTransientStage(downloader));
            }

            private T CreateStage<T>(Func<T> factory)
                where T : StageBase
            {
                T stage = factory();
                stage.Logger = loggerFactory.CreateLogger<T>();
                stage.Context = context;
                return stage;
            }
        }
    }
}