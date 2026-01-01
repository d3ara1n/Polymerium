namespace Polymerium.App.Models;

/// <summary>
///     Defines the proxy operation mode for network connections.
/// </summary>
public enum ProxyMode
{
    /// <summary>
    ///     Use system proxy settings (default HttpClient behavior).
    /// </summary>
    Auto = 0,

    /// <summary>
    ///     Use manually configured proxy settings.
    /// </summary>
    Manual = 1,

    /// <summary>
    ///     Disable all proxies, use direct connection.
    /// </summary>
    Disabled = 2
}
