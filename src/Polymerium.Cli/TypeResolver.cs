using Spectre.Console.Cli;

namespace Polymerium.Cli;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public void Dispose()
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return provider.GetService(type);
    }
}