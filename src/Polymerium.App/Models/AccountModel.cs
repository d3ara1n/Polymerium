using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Core.Accounts;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

public partial class AccountModel : ModelBase
{
    public AccountModel(Type type, string uuid, string userName, DateTimeOffset enrolledAt, DateTimeOffset? lastUsedAt)
    {
        UserName = userName;
        Uuid = uuid;
        Type = type;
        EnrolledAtRaw = enrolledAt;
        LastUsedAtRaw = lastUsedAt;
        if (type.IsAssignableTo(typeof(MicrosoftAccount)))
        {
            TypeName = Resources.Account_Microsoft;
            Color1 = Color.FromArgb(255, 131, 158, 255);
            Color2 = Color.FromArgb(255, 121, 255, 207);
            FaceUrl = new($"https://starlightskins.lunareclipse.studio/render/pixel/{uuid}/face", UriKind.Absolute);
            BodyUrl = new($"https://starlightskins.lunareclipse.studio/render/default/{uuid}/face", UriKind.Absolute);
        }
        else if (type.IsAssignableTo(typeof(TrialAccount)))
        {
            TypeName = Resources.Account_Trial;
            Color1 = Color.FromArgb(255, 253, 160, 133);
            Color2 = Color.FromArgb(255, 246, 211, 101);
            FaceUrl = new($"https://starlightskins.lunareclipse.studio/render/pixel/{userName}/face", UriKind.Absolute);
            BodyUrl = new($"https://starlightskins.lunareclipse.studio/render/default/{userName}/face",
                          UriKind.Absolute);
        }
        else
        {
            TypeName = Resources.Account_Offline;
            Color1 = Color.FromArgb(255, 134, 143, 150);
            Color2 = Color.FromArgb(255, 89, 97, 100);
            FaceUrl = new($"https://starlightskins.lunareclipse.studio/render/pixel/{userName}/face", UriKind.Absolute);
            BodyUrl = new($"https://starlightskins.lunareclipse.studio/render/default/{userName}/face",
                          UriKind.Absolute);
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
