using Polymerium.Trident.Repositories;

namespace Polymerium.Trident.Resolving;

public class ResolveInputBuilder : IBuilder.IBuilder<ResolveInput>
{
    private readonly IList<string> purls = new List<string>();

    private IRepository.Filter? filter;

    public ResolveInputBuilder Add(string purl)
    {
        purls.Add(purl);
        return this;
    }

    public ResolveInputBuilder WithFilter(IRepository.Filter value)
    {
        filter = value;
        return this;
    }

    public ResolveInput Build() => new(purls, filter ?? IRepository.Filter.Empty);
}