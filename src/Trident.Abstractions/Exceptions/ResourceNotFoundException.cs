namespace Trident.Abstractions.Exceptions;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string name) : base($"Resource {name} not found")
    {
    }
}