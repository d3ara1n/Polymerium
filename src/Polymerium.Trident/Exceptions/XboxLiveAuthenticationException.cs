namespace Polymerium.Trident.Exceptions;

public class XboxLiveAuthenticationException(XboxLiveAuthenticationException.ErrorKind kind, string message)
    : AccountAuthenticationException(message)
{
    #region ErrorKind enum

    public enum ErrorKind
    {
        Unknown,
        ParentControl,
        UnsupportedRegion
    }

    #endregion

    public ErrorKind Kind { get; } = kind;
}