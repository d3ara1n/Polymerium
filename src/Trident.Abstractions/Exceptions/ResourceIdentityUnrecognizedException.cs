namespace Trident.Abstractions.Exceptions;

public class ResourceIdentityUnrecognizedException : Exception
{
    public ResourceIdentityUnrecognizedException(string identity, string type) : this(identity, type,
        $"Unrecognized identity {identity} for {type}")
    {
    }

    public ResourceIdentityUnrecognizedException(string identity, string type, string message) : base(message)
    {
        ResourceIdentity = identity;
        ResourceType = type;
    }

    public string ResourceType { get; }
    public string ResourceIdentity { get; }
}