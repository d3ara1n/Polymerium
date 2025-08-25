using System.Reactive.Disposables;
using Trident.Abstractions.Reactive;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public abstract class StageBase : IDisposableLifetime
    {
        public DeployContext Context { get; set; } = null!;

        #region IDisposableLifetime Members

        public CompositeDisposable DisposableLifetime { get; } = new();

        public virtual void Dispose() => DisposableLifetime.Dispose();

        #endregion

        protected abstract Task OnProcessAsync(CancellationToken token);

        public Task ProcessAsync(CancellationToken token) => OnProcessAsync(token);
    }
}
