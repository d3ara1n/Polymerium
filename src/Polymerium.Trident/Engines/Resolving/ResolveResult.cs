using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Resolving;

public class ResolveResult
{
    public ResolveResult(string purl, Exception e)
    {
        Purl = purl;
        IsResolvedSuccessfully = false;
        Result = null;
        Exception = new ResolveException(purl, e);
    }

    public ResolveResult(string purl, Package package)
    {
        Purl = purl;
        IsResolvedSuccessfully = true;
        Result = package;
        Exception = null;
    }

    public bool IsResolvedSuccessfully { get; }

    public Package? Result { get; }

    public ResolveException? Exception { get; }
    public string Purl { get; }
}