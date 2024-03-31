using System.Linq;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Extensions
{
    public static class ProfileExtensions
    {
        public static string ExtractTypeDisplay(this Profile self)
        {
            var modloader = self.Metadata.Layers.SelectMany(x => x.Loaders)
                .FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x.Id));
            return modloader != null ? Loader.MODLOADER_NAME_MAPPINGS[modloader.Id] : "Vanilla";
        }

        public static T GetOverriddenValue<T>(this Profile self, string key)
        {
            if (self.Overrides.TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }

            return Settings.GetValue<T>(key);
        }

        public static uint GetOverriddenWindowHeight(this Profile self)
        {
            return GetOverriddenValue<uint>(self, Settings.GAME_WINDOW_HEIGHT);
        }

        public static uint GetOverriddenWindowWidth(this Profile self)
        {
            return GetOverriddenValue<uint>(self, Settings.GAME_WINDOW_WIDTH);
        }

        public static uint GetOverriddenJvmMaxMemory(this Profile self)
        {
            return GetOverriddenValue<uint>(self, Settings.GAME_JVM_MAX_MEMORY);
        }

        public static string GetOverriddenJvmAdditionalArguments(this Profile self)
        {
            return GetOverriddenValue<string>(self, Settings.GAME_JVM_ADDITIONAL_ARGUMENTS);
        }

        public static string GetOverriddenJvmHome(this Profile self, uint major)
        {
            if (self.Overrides.TryGetValue(Settings.GAME_JVM_HOME, out var value) && value is string result)
            {
                return result;
            }

            return major switch
            {
                8 => Settings.Java8,
                11 => Settings.Java11,
                17 => Settings.Java17,
                _ => string.Empty
            };
        }
    }
}