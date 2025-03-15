using System.Reactive.Disposables;
using Trident.Abstractions.Reactive;

namespace Trident.Abstractions.Extensions;

public static class DisposableExtensions
{
    public static IDisposable DisposeWith(this IDisposable self, CompositeDisposable disposable)
    {
        disposable.Add(self);
        return self;
    }

    public static IDisposable DisposeWith(this IDisposable self, IDisposableLifetime disposable)
    {
        disposable.DisposableLifetime.Add(self);
        return self;
    }
}