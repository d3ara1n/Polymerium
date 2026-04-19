using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using Trident.Core.Utilities;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

public partial class AssetServerModel(
    string sourceFilePath,
    Bitmap icon,
    AssetServerMetadataModel metadata
) : ModelBase
{
    public string SourceFilePath { get; } = sourceFilePath;

    public string SourceFileName => Path.GetFileName(SourceFilePath);

    public AssetServerMetadataModel Metadata { get; } = metadata;

    public string DisplayName => Metadata.Name ?? Resources.Enum_Unknown;

    public string Address => Metadata.Ip ?? Resources.Enum_Unknown;

    public bool HasLiveStatus =>
        !string.IsNullOrWhiteSpace(Description)
        || !string.IsNullOrWhiteSpace(VersionName)
        || OnlinePlayers is not null
        || MaxPlayers is not null
        || LatencyMilliseconds is not null;

    #region Reactive

    [ObservableProperty]
    public partial Bitmap Icon { get; set; } = icon;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial bool IsStatusLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial string? Description { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial string? VersionName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial int? OnlinePlayers { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial int? MaxPlayers { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLiveStatus))]
    public partial long? LatencyMilliseconds { get; set; }

    #endregion

    public void ApplyLiveStatus(MinecraftServerStatusHelper.ServerStatusResult status)
    {
        Description = status.Description;
        VersionName = status.VersionName;
        OnlinePlayers = status.OnlinePlayers;
        MaxPlayers = status.MaxPlayers;
        LatencyMilliseconds = status.LatencyMilliseconds;

        var liveIcon = AssetServerHelper.ExtractIcon(status.FaviconBase64);
        if (liveIcon != null)
        {
            Icon = liveIcon;
        }
    }
}
