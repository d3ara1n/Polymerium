using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

/// <summary>
///     存档中的玩家模型
/// </summary>
public partial class AssetWorldPlayerModel : ModelBase
{
    public AssetWorldPlayerModel(
        string uuid,
        string userName,
        Uri faceUrl,
        AssetWorldPlayerStatsModel stats,
        AssetWorldPlayerAdvancementsModel advancements)
    {
        Uuid = uuid;
        UserName = userName;
        FaceUrl = faceUrl;
        Stats = stats;
        Advancements = advancements;
    }

    #region Reactive

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    #endregion

    #region Direct

    public string Uuid { get; }
    public string UserName { get; }
    public Uri FaceUrl { get; }
    public AssetWorldPlayerStatsModel Stats { get; }
    public AssetWorldPlayerAdvancementsModel Advancements { get; }

    #endregion
}
