using System;
using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Utilities;
using TridentCore.Core.Accounts;
using Resources = Polymerium.Avalonia.Properties.Resources;

namespace Polymerium.Avalonia.Models;

public partial class AccountModel : ModelBase
{
    public AccountModel(
        Type type,
        string uuid,
        string userName,
        DateTimeOffset enrolledAt,
        DateTimeOffset? lastUsedAt,
        string? authlibServerUrl = null
    )
    {
        UserName = userName;
        Uuid = uuid;
        Type = type;
        EnrolledAtRaw = enrolledAt;
        LastUsedAtRaw = lastUsedAt;
        AuthlibServerUrl = authlibServerUrl;
        if (type.IsAssignableTo(typeof(MicrosoftAccount)))
        {
            TypeName = Resources.Account_Microsoft;
            Color1 = Color.FromArgb(255, 131, 158, 255);
            Color2 = Color.FromArgb(255, 121, 255, 207);
            FaceUrl = AccountHelper.GetFaceUrl(uuid);
            BodyUrl = AccountHelper.GetBodyUrl(uuid);
            SkinViews = AccountHelper.GetBodyViewUrls(uuid);
        }
        else if (type.IsAssignableTo(typeof(AuthlibAccount)))
        {
            TypeName = Resources.Account_AuthlibInjector;
            Color1 = Color.FromArgb(255, 131, 200, 255);
            Color2 = Color.FromArgb(255, 180, 130, 255);
            FaceUrl = AccountHelper.GetFaceUrl(uuid);
            BodyUrl = AccountHelper.GetBodyUrl(uuid);
            SkinViews = AccountHelper.GetBodyViewUrls(uuid);
        }
        else if (type.IsAssignableTo(typeof(TrialAccount)))
        {
            TypeName = Resources.Account_Trial;
            Color1 = Color.FromArgb(255, 253, 160, 133);
            Color2 = Color.FromArgb(255, 246, 211, 101);
            FaceUrl = AccountHelper.GetFaceUrl(userName);
            BodyUrl = AccountHelper.GetBodyUrl(userName);
            SkinViews = AccountHelper.GetBodyViewUrls(userName);
        }
        else
        {
            TypeName = Resources.Account_Offline;
            Color1 = Color.FromArgb(255, 134, 143, 150);
            Color2 = Color.FromArgb(255, 89, 97, 100);
            FaceUrl = AccountHelper.GetFaceUrl(userName);
            BodyUrl = AccountHelper.GetBodyUrl(userName);
            SkinViews = AccountHelper.GetBodyViewUrls(userName);
        }
    }

    #region Direct

    public string UserName { get; }

    public string Uuid { get; }

    public string TypeName { get; }

    public Type Type { get; }

    public Color Color1 { get; }
    public Color Color2 { get; }
    public Uri FaceUrl { get; }

    public Uri BodyUrl { get; }

    /// <summary>
    ///     The four directional body render URLs (front → right → back → left) consumed by the
    ///     rotating skin preview. Order matters for a continuous 360° turn.
    /// </summary>
    public IReadOnlyList<Uri> SkinViews { get; } = [];

    public string? AuthlibServerUrl { get; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnrolledAt))]
    public partial DateTimeOffset EnrolledAtRaw { get; set; }

    public string EnrolledAt => EnrolledAtRaw.Humanize();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastUsedAt))]
    public partial DateTimeOffset? LastUsedAtRaw { get; set; }

    public string LastUsedAt => LastUsedAtRaw.Humanize();

    #endregion
}
