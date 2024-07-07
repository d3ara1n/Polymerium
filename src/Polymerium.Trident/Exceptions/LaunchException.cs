namespace Polymerium.Trident.Exceptions;

public class LaunchException : Exception
{
    public LaunchException(string key, Exception inner) : base(
        $"Exception occurred while launching: {inner.Message}", inner) =>
        Key = key;

    public LaunchException(string key, string message) : base(message) => Key = key;

    public string Key { get; }
}