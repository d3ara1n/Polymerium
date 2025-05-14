namespace Polymerium.Trident.Exceptions;

public class XboxLiveAuthenticationException(XboxLiveAuthenticationException.ErrorKind kind, string message)
    : AccountAuthenticationException(message)
{
    public enum ErrorKind { Unknown, ParentControl, UnsupportedRegion }

    public ErrorKind Kind { get; } = kind;
}