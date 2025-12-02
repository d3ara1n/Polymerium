namespace Polymerium.App.Models;

/// <summary>
/// Defines the proxy protocol type.
/// </summary>
public enum ProxyProtocol
{
    /// <summary>
    /// HTTP proxy protocol.
    /// </summary>
    Http = 0,

    /// <summary>
    /// SOCKS4 proxy protocol (.NET 6+ native support).
    /// </summary>
    Socks4 = 1,

    /// <summary>
    /// SOCKS5 proxy protocol (.NET 6+ native support).
    /// </summary>
    Socks5 = 2
}
