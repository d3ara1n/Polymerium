namespace Trident.Abstractions.Exceptions;

public class BadNetworkException : Exception
{
    public BadNetworkException(string file, Exception e) : base($"Failed to download file({file}): {e.Message}", e)
    {
    }

    public BadNetworkException(string message) : base(message)
    {
    }

    public BadNetworkException(string file, int code) : this(
        $"Failed to download file({file}): Returned {code}")
    {
    }

    public BadNetworkException()
    {
    }
}