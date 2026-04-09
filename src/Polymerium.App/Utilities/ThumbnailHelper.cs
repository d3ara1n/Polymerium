using System;
using Polymerium.App.Assets;
using Trident.Core.Utilities;

namespace Polymerium.App.Utilities;

public static class ThumbnailHelper
{
    public static Uri ForInstance(string key)
    {
        var iconPath = InstanceHelper.PickIcon(key);
        return !string.IsNullOrEmpty(iconPath)
            ? new(iconPath, UriKind.Absolute)
            : AssetUriIndex.DirtImage;
    }
}
