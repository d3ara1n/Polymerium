using System;
using System.Collections.Generic;
using System.Globalization;
using Huskui.Avalonia;
using Polymerium.App.Models;

namespace Polymerium.App;

public class Configuration
{
    public const string APPLICATION_SUPERPOWER_ACTIVATED = "Application.SuperpowerActivated";
    public const string APPLICATION_TITLEBAR_VISIBILITY = "Application.TitleBar.Visibility";
    public const string APPLICATION_LEFT_PANEL_MODE = "Application.LeftPanelMode";
    public const string APPLICATION_STYLE_ACCENT = "Application.Style.Accent";
    public const string APPLICATION_STYLE_CORNER = "Application.Style.Corner";
    public const string APPLICATION_STYLE_BACKGROUND = "Application.Style.Background";
    public const string APPLICATION_STYLE_THEME_VARIANT = "Application.Style.ThemeVariant";
    public const string APPLICATION_LANGUAGE = "Application.Language";
    public const string INTERFACE_SETUP_LAYOUT = "Interface.Setup.Layout";
    public const string INTERFACE_MARKETPLACE_LAYOUT = "Interface.Marketplace.Layout";
    public const string RUNTIME_JAVA_HOME_8 = "Runtime.Java.Home.8";
    public const string RUNTIME_JAVA_HOME_11 = "Runtime.Java.Home.11";
    public const string RUNTIME_JAVA_HOME_17 = "Runtime.Java.Home.17";
    public const string RUNTIME_JAVA_HOME_21 = "Runtime.Java.Home.21";
    public const string RUNTIME_JAVA_HOME_25 = "Runtime.Java.Home.25";
    public const string GAME_JAVA_MAX_MEMORY = "Game.Java.MaxMemory";
    public const string GAME_JAVA_ADDITIONAL_ARGUMENTS = "Game.Java.AdditionalArguments";
    public const string GAME_WINDOW_HEIGHT = "Game.Window.Height";
    public const string GAME_WINDOW_WIDTH = "Game.Window.Width";
    public const string NETWORK_PROXY_MODE = "Network.Proxy.Mode";
    public const string NETWORK_PROXY_PROTOCOL = "Network.Proxy.Protocol";
    public const string NETWORK_PROXY_ENABLED = "Network.Proxy.Enabled";
    public const string NETWORK_PROXY_ADDRESS = "Network.Proxy.Address";
    public const string NETWORK_PROXY_PORT = "Network.Proxy.Port";
    public const string NETWORK_PROXY_USERNAME = "Network.Proxy.Username";
    public const string NETWORK_PROXY_PASSWORD = "Network.Proxy.Password";
    public const string UPDATE_SOURCE = "Update.Source";
    public const string UPDATE_MIRRORCHYAN_CDK = "Update.MirrorChyan.Cdk";

    private static readonly Dictionary<string, object?> Defaults = new()
    {
        { APPLICATION_SUPERPOWER_ACTIVATED, false },
        { APPLICATION_TITLEBAR_VISIBILITY, !OperatingSystem.IsLinux() },
        { APPLICATION_LEFT_PANEL_MODE, false },
        { APPLICATION_STYLE_ACCENT, AccentColor.System },
        { APPLICATION_STYLE_CORNER, CornerStyle.Normal },
        { APPLICATION_STYLE_BACKGROUND, 0 },
        { APPLICATION_STYLE_THEME_VARIANT, 0 },
        { APPLICATION_LANGUAGE, CultureInfo.InstalledUICulture.Name },
        { INTERFACE_SETUP_LAYOUT, 0 },
        { INTERFACE_MARKETPLACE_LAYOUT, 0 },
        { RUNTIME_JAVA_HOME_8, string.Empty },
        { RUNTIME_JAVA_HOME_11, string.Empty },
        { RUNTIME_JAVA_HOME_17, string.Empty },
        { RUNTIME_JAVA_HOME_21, string.Empty },
        { RUNTIME_JAVA_HOME_25, string.Empty },
        { GAME_JAVA_MAX_MEMORY, 4096u },
        { GAME_JAVA_ADDITIONAL_ARGUMENTS, string.Empty },
        { GAME_WINDOW_WIDTH, 1270u },
        { GAME_WINDOW_HEIGHT, 720u },
        { NETWORK_PROXY_MODE, (int)ProxyMode.Auto },
        { NETWORK_PROXY_PROTOCOL, (int)ProxyProtocol.Http },
        { NETWORK_PROXY_ENABLED, false },
        { NETWORK_PROXY_ADDRESS, "127.0.0.1" },
        { NETWORK_PROXY_PORT, 7890u },
        { NETWORK_PROXY_USERNAME, string.Empty },
        { NETWORK_PROXY_PASSWORD, string.Empty },
        { UPDATE_SOURCE, 1 },
        { UPDATE_MIRRORCHYAN_CDK, string.Empty }
    };

    public static string[] SupportedLanguages { get; } = ["en-US", "zh-Hans"];

    public bool ApplicationSuperPowerActivated { get; set; } = AccessDefault<bool>(APPLICATION_SUPERPOWER_ACTIVATED);

    public bool ApplicationTitleBarVisibility { get; set; } = AccessDefault<bool>(APPLICATION_TITLEBAR_VISIBILITY);
    public bool ApplicationLeftPanelMode { get; set; } = AccessDefault<bool>(APPLICATION_LEFT_PANEL_MODE);
    public AccentColor ApplicationStyleAccent { get; set; } = AccessDefault<AccentColor>(APPLICATION_STYLE_ACCENT);
    public CornerStyle ApplicationStyleCorner { get; set; } = AccessDefault<CornerStyle>(APPLICATION_STYLE_CORNER);
    public int ApplicationStyleBackground { get; set; } = AccessDefault<int>(APPLICATION_STYLE_BACKGROUND);
    public int ApplicationStyleThemeVariant { get; set; } = AccessDefault<int>(APPLICATION_STYLE_THEME_VARIANT);
    public string ApplicationLanguage { get; set; } = AccessDefault<string>(APPLICATION_LANGUAGE);
    public int InterfaceSetupLayout { get; set; } = AccessDefault<int>(INTERFACE_SETUP_LAYOUT);
    public int InterfaceMarketplaceLayout { get; set; } = AccessDefault<int>(INTERFACE_MARKETPLACE_LAYOUT);
    public string RuntimeJavaHome8 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_8);
    public string RuntimeJavaHome11 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_11);
    public string RuntimeJavaHome17 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_17);
    public string RuntimeJavaHome21 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_21);
    public string RuntimeJavaHome25 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_25);
    public uint GameJavaMaxMemory { get; set; } = AccessDefault<uint>(GAME_JAVA_MAX_MEMORY);
    public string GameJavaAdditionalArguments { get; set; } = AccessDefault<string>(GAME_JAVA_ADDITIONAL_ARGUMENTS);
    public uint GameWindowInitialWidth { get; set; } = AccessDefault<uint>(GAME_WINDOW_WIDTH);
    public uint GameWindowInitialHeight { get; set; } = AccessDefault<uint>(GAME_WINDOW_HEIGHT);
    public int NetworkProxyMode { get; set; } = AccessDefault<int>(NETWORK_PROXY_MODE);
    public int NetworkProxyProtocol { get; set; } = AccessDefault<int>(NETWORK_PROXY_PROTOCOL);
    public bool NetworkProxyEnabled { get; set; } = AccessDefault<bool>(NETWORK_PROXY_ENABLED);
    public string NetworkProxyAddress { get; set; } = AccessDefault<string>(NETWORK_PROXY_ADDRESS);
    public uint NetworkProxyPort { get; set; } = AccessDefault<uint>(NETWORK_PROXY_PORT);
    public string NetworkProxyUsername { get; set; } = AccessDefault<string>(NETWORK_PROXY_USERNAME);
    public string NetworkProxyPassword { get; set; } = AccessDefault<string>(NETWORK_PROXY_PASSWORD);
    public int UpdateSource { get; set; } = AccessDefault<int>(UPDATE_SOURCE);
    public string UpdateMirrorChyanCdk { get; set; } = AccessDefault<string>(UPDATE_MIRRORCHYAN_CDK);

    public static T AccessDefault<T>(string key) => (T)Defaults[key]!;
}
