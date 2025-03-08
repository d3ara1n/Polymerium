using System.Collections.Generic;

namespace Polymerium.App;

public class Configuration
{
    public const string APPLICATION_SUPERPOWER_ACTIVATED = "Application.SuperpowerActivated";
    public const string APPLICATION_STYLE = "Application.Style";
    public const string APPLICATION_LANGUAGE = "Application.Language";
    public const string RUNTIME_JAVA_HOME_8 = "Runtime.Java.Home.8";
    public const string RUNTIME_JAVA_HOME_11 = "Runtime.Java.Home.11";
    public const string RUNTIME_JAVA_HOME_17 = "Runtime.Java.Home.17";
    public const string RUNTIME_JAVA_HOME_21 = "Runtime.Java.Home.21";
    public const string GAME_JAVA_MAX_MEMORY = "Game.Java.MaxMemory";
    public const string GAME_JAVA_ADDITIONAL_ARGUMENTS = "Game.Java.AdditionalArguments";
    public const string GAME_WINDOW_HEIGHT = "Game.Window.Height";
    public const string GAME_WINDOW_WIDTH = "Game.Window.Width";

    private static readonly IReadOnlyDictionary<string, object?> Defaults = new Dictionary<string, object?>
    {
        { APPLICATION_SUPERPOWER_ACTIVATED, false },
        { APPLICATION_STYLE, "Mica" },
        { APPLICATION_LANGUAGE, "en_US" },
        { RUNTIME_JAVA_HOME_8, string.Empty },
        { RUNTIME_JAVA_HOME_11, string.Empty },
        { RUNTIME_JAVA_HOME_17, string.Empty },
        { RUNTIME_JAVA_HOME_21, string.Empty },
        { GAME_JAVA_MAX_MEMORY, 4096u },
        { GAME_JAVA_ADDITIONAL_ARGUMENTS, string.Empty },
        { GAME_WINDOW_WIDTH, 1270u },
        { GAME_WINDOW_HEIGHT, 720u }
    };

    public bool ApplicationSuperPowerActivated { get; set; } = AccessDefault<bool>(APPLICATION_SUPERPOWER_ACTIVATED);
    public string ApplicationLanguage { get; set; } = AccessDefault<string>(APPLICATION_STYLE);
    public string RuntimeJavaHome8 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_8);
    public string RuntimeJavaHome11 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_11);
    public string RuntimeJavaHome17 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_17);
    public string RuntimeJavaHome21 { get; set; } = AccessDefault<string>(RUNTIME_JAVA_HOME_21);
    public uint GameJavaMaxMemory { get; set; } = AccessDefault<uint>(GAME_JAVA_MAX_MEMORY);
    public string GameJavaAdditionalArguments { get; set; } = AccessDefault<string>(GAME_JAVA_ADDITIONAL_ARGUMENTS);
    public uint GameWindowInitialWidth { get; set; } = AccessDefault<uint>(GAME_WINDOW_WIDTH);
    public uint GameWindowInitialHeight { get; set; } = AccessDefault<uint>(GAME_WINDOW_HEIGHT);

    public static T AccessDefault<T>(string key) => (T)Defaults[key]!;
}