namespace Trident.Abstractions.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string name) : this(name, $"Resource {name} not found")
        {
        }

        public ResourceNotFoundException(string name, string message) : base(message)
        {
            ResourceName = name;
        }

        public string ResourceName { get; }
    }
}