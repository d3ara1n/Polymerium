using Trident.Abstractions.Errors;

namespace Polymerium.Trident.Exceptions;

public class ExtractException(ExtractError error, string? reference)
    : Exception($"Extract {reference ?? "in-memory data"} failed for {error}")
{
    public ExtractError Error { get; init; } = error;
    public string? Reference { get; init; } = reference;
}