namespace Polymerium.Avalonia.Models;

/// <summary>
///     皮肤渲染测试样本：一个 <c>skin://</c> 渲染 URL 配上展示标签，
///     供 UnknownPage 的 Skin 测试 Tab 并排对比各 <see cref="Rendering.SkinViewType" />。
/// </summary>
public sealed record SkinViewSample(string Label, string Url);
