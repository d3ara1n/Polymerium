using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

/// <summary>
///     存档模型
/// </summary>
public partial class AssetWorldModel : ModelBase
{
    public AssetWorldModel(
        DirectoryInfo folder,
        Bitmap? icon,
        AssetWorldMetadataModel metadata,
        DateTimeOffset lastPlayed)
    {
        FolderName = folder.Name;
        WorldPath = folder.FullName;
        Icon = icon;
        Metadata = metadata;
        LastPlayedRaw = lastPlayed;
    }

    #region Reactive

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    #endregion

    #region Direct

    public string FolderName { get; }
    public string WorldPath { get; }
    public Bitmap? Icon { get; }
    public AssetWorldMetadataModel Metadata { get; }
    public DateTimeOffset LastPlayedRaw { get; }

    public string DisplayName => Metadata.LevelName ?? FolderName;
    public string LastPlayed => LastPlayedRaw.Humanize();
    public string LastPlayedFormatted => LastPlayedRaw.ToString("g");

    // 从 level.dat 获取的基本信息
    public string GameMode =>
        Metadata.GameType switch
        {
            0 => "Survival",
            1 => "Creative",
            2 => "Adventure",
            3 => "Spectator",
            _ => Resources.Enum_Unknown
        };

    public string Difficulty =>
        Metadata.Difficulty switch
        {
            0 => "Peaceful",
            1 => "Easy",
            2 => "Normal",
            3 => "Hard",
            _ => Resources.Enum_Unknown
        };

    public bool Hardcore => Metadata.Hardcore;
    public bool AllowCommands => Metadata.AllowCommands;
    public long DayTime => Metadata.DayTime;
    public long GameTime => Metadata.Time;
    public int DayCount => (int)(GameTime / 24000);
    public string Version => Metadata.VersionName ?? Resources.Enum_Unknown;

    #endregion
}
