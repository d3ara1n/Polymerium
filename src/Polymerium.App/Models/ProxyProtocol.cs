namespace Polymerium.App.Models;

/// <summary>
///     Defines the proxy protocol type.
/// </summary>
public enum ProxyProtocol
{
    /// <summary>
    ///     HTTP proxy protocol.
    /// </summary>
    HTTP = 0,

    /// <summary>
    ///     SOCKS4 proxy protocol (.NET 6+ native support).
    /// </summary>
    SOCKS4 = 1,

    /// <summary>
    ///     SOCKS5 proxy protocol (.NET 6+ native support).
    /// </summary>
    SOCKS5 = 2,
}
