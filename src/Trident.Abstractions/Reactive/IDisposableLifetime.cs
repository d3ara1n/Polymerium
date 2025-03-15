using System.Reactive.Disposables;

namespace Trident.Abstractions.Reactive;

public interface IDisposableLifetime : IDisposable
{
    CompositeDisposable DisposableLifetime { get; }
}