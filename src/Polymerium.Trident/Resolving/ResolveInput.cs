using Polymerium.Trident.Repositories;

namespace Polymerium.Trident.Resolving;

public readonly struct ResolveInput(IEnumerable<string> purls, IRepository.Filter filter)
{
    public static ResolveInputBuilder Builder() => new ResolveInputBuilder();

    public IEnumerable<string> Purls => purls;
    public IRepository.Filter Filter => filter;
}