using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Properties;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models
{
    public partial class InstanceBasicModel : ModelBase
    {
        public InstanceBasicModel(string key, string name, string version, string? loader, string? source)
        {
            Key = key;
            Name = name;
            Version = version;
            Loader = loader;
            Source = source;
            Thumbnail = AssetUriIndex.DIRT_IMAGE_BITMAP;

            UpdateIcon();
        }

        #region Direct

        public string Key { get; }

        #endregion

        public void UpdateIcon()
        {
            var iconPath = ProfileHelper.PickIcon(Key);
            Thumbnail = iconPath is not null ? new Bitmap(iconPath) : AssetUriIndex.DIRT_IMAGE_BITMAP;
        }

        #region Reactive

        [ObservableProperty]
        public partial string Name { get; set; }

        [ObservableProperty]
        public partial string Version { get; set; }

        [ObservableProperty]
        public partial string LoaderLabel { get; set; } = Resources.Enum_Vanilla;

        [ObservableProperty]
        public partial string SourceLabel { get; set; } = "local";

        [ObservableProperty]
        public partial Bitmap Thumbnail { get; set; }

        [ObservableProperty]
        public partial string? Source { get; set; }

        partial void OnSourceChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value) && PackageHelper.TryParse(value, out var result))
            {
                SourceLabel = result.Label;
            }
        }

        [ObservableProperty]
        public partial string? Loader { get; set; }

        partial void OnLoaderChanged(string? value)
        {
            if (value != null && LoaderHelper.TryParse(value, out var result))
                // TODO: 从语言文件中选取
            {
                LoaderLabel = LoaderHelper.ToDisplayName(result.Identity);
            }
            else
            {
                LoaderLabel = Resources.Enum_Vanilla;
            }
        }

        #endregion
    }
}
