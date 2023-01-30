using System;

namespace Polymerium.Abstractions.ResourceResolving;

public class ResolveResult
{
    public ResultError Error { get; set; }
    public Exception Exception { get; set; }
    public object Result { get; set; }
    public bool Success { get; set; } = true;
}