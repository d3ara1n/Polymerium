using Polymerium.Trident.Repositories;

namespace Polymerium.Trident.Resolving;

public readonly struct ResolveContext(IEnumerable<IRepository> repositories, IEnumerable<string> purls, IRepository.Filter filter)
{
    public IEnumerable<string> Purls => purls;
    public IEnumerable<IRepository> Repositories => repositories;
    public IRepository.Filter Filter => filter;
}