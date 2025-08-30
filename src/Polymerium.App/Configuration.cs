using System;
using System.Collections.Generic;
using System.Globalization;
using Huskui.Avalonia;

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
    public const string GAME_JAVA_MAX_MEMORY = "Game.Java.MaxMemory";
    public const string GAME_JAVA_ADDITIONAL_ARGUMENTS = "Game.Java.AdditionalArguments";
    public const string GAME_WINDOW_HEIGHT = "Game.Window.Height";
    public const string GAME_WINDOW_WIDTH = "Game.Window.Width";

    private static readonly Dictionary<string, object?> DEFAULTS = new()
    {
        { APPLICATION_SUPERPOWER_ACTIVATED, false },
        { APPLICATION_TITLEBAR_VISIBILITY, !OperatingSystem.IsLinux() },
        { APPLICATION_LEFT_PANEL_MODE, false },
        { APPLICATION_STYLE_ACCENT, AccentColor.Amber },
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
        { GAME_JAVA_MAX_MEMORY, 4096u },
        { GAME_JAVA_ADDITIONAL_ARGUMENTS, string.Empty },
        { GAME_WINDOW_WIDTH, 1270u },
        { GAME_WINDOW_HEIGHT, 720u }
    };

    public bool ApplicationSuperPowerActivated { get; set; } =
        AccessDefault<bool>(APPLICATION_SUPERPOWER_ACTIVATED);

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
    public uint GameJavaMaxMemory { get; set; } = AccessDefault<uint>(GAME_JAVA_MAX_MEMORY);
    public string GameJavaAdditionalArguments { get; set; } = AccessDefault<string>(GAME_JAVA_ADDITIONAL_ARGUMENTS);
    public uint GameWindowInitialWidth { get; set; } = AccessDefault<uint>(GAME_WINDOW_WIDTH);
    public uint GameWindowInitialHeight { get; set; } = AccessDefault<uint>(GAME_WINDOW_HEIGHT);

    public static T AccessDefault<T>(string key) => (T)DEFAULTS[key]!;
}
