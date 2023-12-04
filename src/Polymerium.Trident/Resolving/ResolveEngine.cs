using System.Collections;
using DotNext;
using Polymerium.Trident.Errors;
using Polymerium.Trident.Repositories;

namespace Polymerium.Trident.Resolving;

public class ResolveEngine(IEnumerable<IRepository> repositories) : IEngine<ResolveInput, ResolveOutput>
{
    private ResolveInput? initial;

    public void SetContext(ResolveInput fuel) => initial = fuel;

    public IEnumerator<ResolveOutput> GetEnumerator()
        => new ResolveStepper(initial.HasValue
            ? new ResolveContext(repositories, initial.Value.Purls, initial.Value.Filter)
            : throw new ArgumentNullException());

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}