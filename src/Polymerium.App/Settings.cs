using System.Collections.Generic;
using Windows.Storage;

namespace Polymerium.App
{
    public static class Settings
    {
        private const string GENERIC_IS_SUPERPOWER_ACTIVATED = "Generic.IsSuperpowerActivated";
        private const string GENERIC_LANGUAGE = "Generic.Language";
        private const string RUNTIME_JAVA_8 = "Runtime.Java.8";
        private const string RUNTIME_JAVA_11 = "Runtime.Java.11";
        private const string RUNTIME_JAVA_17 = "Runtime.Java.17";

        public const string GAME_JVM_MAX_MEMORY = "Game.Jvm.MaxMemory";
        public const string GAME_JVM_ADDITIONAL_ARGUMENTS = "Game.Jvm.AdditionalArguments";
        public const string GAME_JVM_HOME = "Game.Jvm.Home";
        public const string GAME_WINDOW_HEIGHT = "Game.Window.Height";
        public const string GAME_WINDOW_WIDTH = "Game.Window.Width";


        private static readonly ApplicationDataContainer inner = ApplicationData.Current.LocalSettings;

        private static readonly IDictionary<string, object> defaults = new Dictionary<string, object>
        {
            { GENERIC_IS_SUPERPOWER_ACTIVATED, false },
            { GENERIC_LANGUAGE, "en_US" },
            { RUNTIME_JAVA_8, string.Empty },
            { RUNTIME_JAVA_11, string.Empty },
            { RUNTIME_JAVA_17, string.Empty },
            { GAME_JVM_MAX_MEMORY, 4096u },
            { GAME_JVM_ADDITIONAL_ARGUMENTS, string.Empty },
            { GAME_JVM_HOME, string.Empty },
            { GAME_WINDOW_WIDTH, 1270u },
            { GAME_WINDOW_HEIGHT, 720u }
        };

        public static bool IsSuperpowerActivated
        {
            get => GetValue<bool>(GENERIC_IS_SUPERPOWER_ACTIVATED);
            set => SetValue(GENERIC_IS_SUPERPOWER_ACTIVATED, value);
        }

        public static string Language
        {
            get => GetValue<string>(GENERIC_LANGUAGE);
            set => SetValue(GENERIC_LANGUAGE, value);
        }


        public static string Java8
        {
            get => GetValue<string>(RUNTIME_JAVA_8);
            set => SetValue(RUNTIME_JAVA_8, value);
        }

        public static string Java11
        {
            get => GetValue<string>(RUNTIME_JAVA_11);
            set => SetValue(RUNTIME_JAVA_11, value);
        }

        public static string Java17
        {
            get => GetValue<string>(RUNTIME_JAVA_17);
            set => SetValue(RUNTIME_JAVA_17, value);
        }

        public static uint GameJvmMaxMemory
        {
            get => GetValue<uint>(GAME_JVM_MAX_MEMORY);
            set => SetValue(GAME_JVM_MAX_MEMORY, value);
        }

        public static string GameJvmAdditionalArguments
        {
            get => GetValue<string>(GAME_JVM_ADDITIONAL_ARGUMENTS);
            set => SetValue(GAME_JVM_ADDITIONAL_ARGUMENTS, value);
        }

        public static uint GameWindowHeight
        {
            get => GetValue<uint>(GAME_WINDOW_HEIGHT);
            set => SetValue(GAME_WINDOW_WIDTH, value);
        }

        public static uint GameWindowWidth
        {
            get => GetValue<uint>(GAME_WINDOW_WIDTH);
            set => SetValue(GAME_WINDOW_WIDTH, value);
        }

        public static void SetValue<T>(string key, T v)
        {
            inner.Values[key] = v;
        }

        public static T GetValue<T>(string key)
        {
            return inner.Values.TryGetValue(key, out var v) && v is T r ? r : (T)defaults[key];
        }
    }
}