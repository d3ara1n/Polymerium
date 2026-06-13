using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.Accounts;
using TridentCore.Core.Accounts;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.Utilities;

public static class AccountHelper
{
    private static readonly Uri STEVE_FACE_URL =
        new("https://starlightskins.lunareclipse.studio/render/pixel/8667ba71b85a4004af54457a9734eed7/face",
            UriKind.Absolute);

    private static readonly Uri STEVE_BODY_URL =
        new("https://starlightskins.lunareclipse.studio/render/default/8667ba71b85a4004af54457a9734eed7/face",
            UriKind.Absolute);

    public static IAccount ToCooked(PersistenceService.Account raw) =>
        (IAccount?)(raw.Kind switch
        {
            nameof(MicrosoftAccount) => JsonSerializer.Deserialize<MicrosoftAccount>(raw.Data),
            nameof(TrialAccount) => JsonSerializer.Deserialize<TrialAccount>(raw.Data),
            nameof(OfflineAccount) => JsonSerializer.Deserialize<OfflineAccount>(raw.Data),
            nameof(AuthlibAccount) =>
                JsonSerializer.Deserialize<AuthlibAccount>(raw.Data),
            _ => JsonSerializer.Deserialize<OfflineAccount>(raw.Data),
        })
     ?? throw new FormatException("Failed to deserialize account from the raw data");

    public static AccountModel CreateModelFromAccount(
        IAccount account,
        DateTimeOffset? enrolledAt = null,
        DateTimeOffset? lastUsedAt = null)
    {
        var serverUrl = account is AuthlibAccount authlib ? authlib.ServerUrl : null;
        return new(account.GetType(), account.Uuid, account.Username, enrolledAt ?? DateTimeOffset.Now, lastUsedAt, serverUrl);
    }

    public static PersistenceService.Account ToRaw(
        IAccount account,
        DateTimeOffset enrolledAt,
        DateTimeOffset? lastUsedAt,
        bool isDefault) =>
        new()
        {
            Uuid = account.Uuid,
            IsDefault = isDefault,
            EnrolledAt = DateTimeHelper.ToPersistedLocalDateTime(enrolledAt),
            LastUsedAt = DateTimeHelper.ToPersistedLocalDateTime(lastUsedAt),
            Data = JsonSerializer.Serialize(account, account.GetType()),
            Kind = account.GetType().Name,
        };

    public static string ToRaw(IAccount account) => JsonSerializer.Serialize(account, account.GetType());

    public static Uri GetSteveFaceUrl() => STEVE_FACE_URL;

    public static Uri GetSteveBodyUrl() => STEVE_BODY_URL;

    public static Uri GetFaceUrl(string uuidOrUsername) =>
        new($"https://starlightskins.lunareclipse.studio/render/pixel/{uuidOrUsername}/face", UriKind.Absolute);

    public static Uri GetBodyUrl(string uuidOrUsername) =>
        new($"https://starlightskins.lunareclipse.studio/render/default/{uuidOrUsername}/face", UriKind.Absolute);

    private static Uri GetBodyViewUrl(string uuidOrUsername, SkinView view, int scale = 8) =>
        new(
            $"https://api.mineatar.io/body/{view.ToString().ToLowerInvariant()}/{uuidOrUsername}?scale={scale}",
            UriKind.Absolute
        );

    /// <summary>
    ///     Returns the four directional body render URLs (front → right → back → left) used by
    ///     the rotating skin preview. Order matters: it forms a full 360° turn.
    /// </summary>
    public static IReadOnlyList<Uri> GetBodyViewUrls(string uuidOrUsername, int scale = 8) =>
    [
        GetBodyViewUrl(uuidOrUsername, SkinView.Front, scale),
        GetBodyViewUrl(uuidOrUsername, SkinView.Right, scale),
        GetBodyViewUrl(uuidOrUsername, SkinView.Back, scale),
        GetBodyViewUrl(uuidOrUsername, SkinView.Left, scale),
    ];

    /// <summary>
    ///     The four cardinal directions a skin body render can face, ordered so that rotating
    ///     through them yields a continuous 360° turn.
    /// </summary>
    private enum SkinView
    {
        Front,
        Right,
        Back,
        Left,
    }
}
