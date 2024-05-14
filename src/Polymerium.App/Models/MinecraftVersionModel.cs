using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    // 等什么时候 WindowsAppSdk 修好 https://github.com/microsoft/microsoft-ui-xaml/issues/5315 再换回 Record
    public class MinecraftVersionModel(string version, ReleaseType type, DateTimeOffset releasedAt)
    {
        public string Version => version;
        public ReleaseType Type => type;
        public DateTimeOffset ReleasedAt => releasedAt;
    }
}