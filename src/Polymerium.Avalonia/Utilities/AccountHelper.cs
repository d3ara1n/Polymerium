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
        var skinSource = BuildSkinSource(account);
        return new(account.GetType(), account.Uuid, account.Username, enrolledAt ?? DateTimeOffset.Now, lastUsedAt, serverUrl, skinSource);
    }

    /// <summary>
    ///     按账户类型构造本地渲染所需的皮肤数据源（src），是 <c>IAccount → src</c> 的唯一入口：<br />
    ///     Microsoft → <c>mojang:{uuid}</c>（渲染时查 Mojang sessionserver profile）；<br />
    ///     Authlib → 账户 <c>SkinUrl</c>（裸 URL），缺失时回落 Steve；<br />
    ///     Trial/Offline → 内置 Steve。<br />
    ///     所有 <see cref="GetFaceUrl" />/<see cref="GetBodyUrl" /> 等都消费此函数产出的 src，避免在调用方散落账户→src 的组装。
    /// </summary>
    public static string BuildSkinSource(IAccount account) =>
        account switch
        {
            MicrosoftAccount => $"mojang:{account.Uuid}",
            AuthlibAccount { SkinUrl: { } url } => url,
            _ => "asset:Steve",
        };

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

    /// <summary>
    ///     构造本地渲染的 <c>poly://skin</c> URI。<paramref name="src" /> 由
    ///     <see cref="BuildSkinSource" /> 按账户类型产生，由
    ///     <see cref="Services.SkinRenderService" /> 解析路由后离线渲染。
    /// </summary>
    public static Uri GetFaceUrl(string src) =>
        new($"poly://skin?type=face&src={Uri.EscapeDataString(src)}", UriKind.Absolute);

    public static Uri GetBodyUrl(string src) =>
        new($"poly://skin?type=body&src={Uri.EscapeDataString(src)}", UriKind.Absolute);

    /// <summary>
    ///     构造半身像（Cover）的本地渲染 URI：与 <see cref="GetBodyUrl" /> 共用全身缩放，
    ///     头顶贴顶、画布截取上半身，适合方形卡片预览。
    /// </summary>
    public static Uri GetCoverUrl(string src) =>
        new($"poly://skin?type=cover&src={Uri.EscapeDataString(src)}", UriKind.Absolute);

    public static IReadOnlyList<Uri> GetBodyViewUrls(string src) =>
    [
        new($"poly://skin?type=front&src={Uri.EscapeDataString(src)}", UriKind.Absolute),
        new($"poly://skin?type=right&src={Uri.EscapeDataString(src)}", UriKind.Absolute),
        new($"poly://skin?type=back&src={Uri.EscapeDataString(src)}", UriKind.Absolute),
        new($"poly://skin?type=left&src={Uri.EscapeDataString(src)}", UriKind.Absolute),
    ];
}
