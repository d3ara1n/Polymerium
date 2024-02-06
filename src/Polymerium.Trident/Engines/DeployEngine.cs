using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines;

public class DeployEngine(IServiceProvider provider) : IEngine<DeployContext, StageBase>
{
    private DeployContext? context;

    public void SetContext(DeployContext fuel)
    {
        context = fuel;
    }

    public IEnumerator<StageBase> GetEnumerator()
    {
        ArgumentNullException.ThrowIfNull(context);
        return ActivatorUtilities.CreateInstance<DeployEngineEnumerator>(provider, context);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public class DeployEngineEnumerator : IEnumerator<StageBase>
    {
        private readonly DeployContext _context;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _provider;
        private readonly TridentContext _trident;

        private readonly string artifactPath;

        public DeployEngineEnumerator(IServiceProvider provider, ILoggerFactory loggerFactory, DeployContext context,
            TridentContext trident)
        {
            _provider = provider;
            _loggerFactory = loggerFactory;
            _context = context;
            _trident = trident;

            artifactPath = _trident.InstanceArtifactPath(context.Key);

            Current = CheckArtifact();
        }

        public bool MoveNext()
        {
            // Deploy 包含以下过程：
            // Build artifact:
            //   Resolve attachments, Install game, Process loaders
            // Solidify polylock data:
            //   Download libraries, Download & link attachments, Restore assets
            // trident.artifact.{watermark}.json
            // 水印是 Metadata 最后一次修改的时间
            if (_context.Token.IsCancellationRequested) return false;
            if (_context.Artifact != null)
                // solidify
                return false;
            if (_context.ArtifactBuilder != null)
                return true;
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public StageBase Current { get; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // do nothing
        }

        private CheckArtifactStage CheckArtifact()
        {
            return CreateStage(() => new CheckArtifactStage(artifactPath));
        }

        private T CreateStage<T>(Func<T> factory)
            where T : StageBase
        {
            var stage = factory();
            stage.Logger = _loggerFactory.CreateLogger<T>();
            stage.Provider = _provider;
            stage.Context = _context;
            return stage;
        }
    }
}