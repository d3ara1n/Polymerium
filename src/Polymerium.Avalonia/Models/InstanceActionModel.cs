using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceActionModel(
    string? oldPref,
    string? newPref,
    DateTimeOffset modifiedAt,
    bool canUndo) : ModelBase
{
    #region Direct

    // NOTE: Kind 由 Old/New Pref 存在性推导，与能否解析无关——解析失败的 Update 仍是 Update，
    //  只是退化为展示原始 Pref。
    public InstanceActionKind Kind =>
        (oldPref, newPref) switch
        {
            (not null, not null) => InstanceActionKind.Update,
            (null, not null) => InstanceActionKind.Add,
            (not null, null) => InstanceActionKind.Remove,
            _ => InstanceActionKind.Unknown
        };

    // NOTE: 原始 Pref 始终可得（来自 Action 记录），是解析失败时的兜底展示。
    public string? OldPref => oldPref;
    public string? NewPref => newPref;

    public DateTimeOffset ModifiedAtRaw => modifiedAt;
    public string ModifiedAt => modifiedAt.Humanize();

    public bool CanUndo => canUndo;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    // NOTE: Info 非空即解析成功，null 即失败——失败时卡片退化为展示原始 Pref。
    [ObservableProperty]
    public partial InstanceActionInfoModel? Info { get; set; }

    #endregion
}
