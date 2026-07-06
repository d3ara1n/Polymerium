using System;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     包来源归属与锁定语义的纯函数工具：所有判定都基于 <see cref="Entry.Source" /> 与当前实例引用
///     <c>profile.Setup.Source</c>，不持有任何状态。PackageModel 与未来的 Group VM 都从此投影，
///     无第二份真相。详见 <c>plans/SOURCE-REFERENCE-SEMANTICS.md</c> §3。
/// </summary>
public static class PackageSourceHelper
{
    /// <summary>
    ///     <see cref="Entry.Source" /> 的归属分类，由 <see cref="Classify" /> 产出。是分组 UI、部署优先级、
    ///     Exhibit 的共同输入。详见 <c>plans/SOURCE-REFERENCE-SEMANTICS.md</c> §3.1。
    /// </summary>
    public enum Kind
    {
        /// <summary>用户手动添加（<c>Source == null</c>），自由包。</summary>
        Manual,

        /// <summary>当前整合包带来（<c>Source == profile.Setup.Source</c>），版本被整合包占有。</summary>
        Modpack,

        /// <summary>recipe 带来（<c>Source</c> 为 <c>recipe://</c>），锁组但不占版本。</summary>
        Recipe,

        /// <summary>曾属某整合包、已解绑（<c>Source</c> 非 null 且不等于当前引用），保留来源标签。</summary>
        Legacy,
    }

    /// <summary>
    ///     把 <paramref name="source" /> 归入四种归属之一。<paramref name="current" /> 即
    ///     <c>profile.Setup.Source</c>。
    /// </summary>
    public static Kind Classify(string? source, string? current) =>
        source is null
            ? Kind.Manual
            : source == current
                ? Kind.Modpack
                : InternalUriHelper.IsKind(source, "recipe")
                    ? Kind.Recipe
                    : Kind.Legacy;

    /// <summary>单包能否删除：只有手动包（不属任何组）可删。不依赖 <paramref name="current" />。</summary>
    public static bool CanRemove(string? source, string? current) => source is null;

    /// <summary>
    ///     单包能否改版本：只有当前整合包占有版本（<c>source == current</c>），其余都可改。
    ///     边界：<c>current == null</c> 时手动包经 <c>source is null</c> 短路得 <c>true</c>，不误锁。
    /// </summary>
    public static bool CanUpdate(string? source, string? current) =>
        source is null || source != current;

    /// <summary>整组能否解散（Ungroup）：当前整合包组不可（须先解绑降级），Recipe / Legacy 可。</summary>
    public static bool CanUngroup(string? source, string? current) =>
        source is not null && source != current;
}
