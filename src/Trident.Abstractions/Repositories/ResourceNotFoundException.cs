namespace Trident.Abstractions.Repositories;

public class ResourceNotFoundException(string message) : Exception(message) { }