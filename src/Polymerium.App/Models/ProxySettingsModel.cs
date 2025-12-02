namespace Polymerium.App.Models;

/// <summary>
/// Represents the proxy configuration settings.
/// </summary>
public class ProxySettingsModel
{
    /// <summary>
    /// Gets or sets the proxy operation mode.
    /// </summary>
    public ProxyMode Mode { get; set; } = ProxyMode.Auto;

    /// <summary>
    /// Gets or sets the proxy protocol type.
    /// </summary>
    public ProxyProtocol Protocol { get; set; } = ProxyProtocol.Http;

    /// <summary>
    /// Gets or sets the proxy server address.
    /// </summary>
    public string Address { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the proxy server port.
    /// </summary>
    public uint Port { get; set; } = 7890;

    /// <summary>
    /// Gets or sets the authentication username (optional).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication password (optional).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public ProxySettingsModel Clone() => new()
    {
        Mode = Mode,
        Protocol = Protocol,
        Address = Address,
        Port = Port,
        Username = Username,
        Password = Password
    };

    /// <summary>
    /// Gets the display text for the current proxy status.
    /// </summary>
    public string GetStatusDisplayText()
    {
        return Mode switch
        {
            ProxyMode.Auto => Properties.Resources.SettingsView_ProxyStatusAutoText,
            ProxyMode.Disabled => Properties.Resources.SettingsView_ProxyStatusDisabledText,
            ProxyMode.Manual => Protocol == ProxyProtocol.Socks5
                ? $"socks5://{Address}:{Port}"
                : $"{Address}:{Port}",
            _ => string.Empty
        };
    }
}
