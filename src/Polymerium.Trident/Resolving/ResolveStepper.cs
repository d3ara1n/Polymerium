using System.Collections;

namespace Polymerium.Trident.Resolving;

public class ResolveStepper(ResolveContext context) : IEnumerator<ResolveOutput>
{
    public bool MoveNext()
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public ResolveOutput Current { get; }

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}