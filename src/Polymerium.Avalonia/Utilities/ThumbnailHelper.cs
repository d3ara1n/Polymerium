using System;
using Polymerium.Avalonia.Assets;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Utilities;

public static class ThumbnailHelper
{
    public static Uri ForInstance(string key)
    {
        var iconPath = InstanceHelper.PickIcon(key);
        return !string.IsNullOrEmpty(iconPath) ? new(iconPath, UriKind.Absolute) : AssetUriIndex.DirtImage;
    }
}
