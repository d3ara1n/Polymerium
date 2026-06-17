using System;
using System.Collections.Generic;
using Huskui.Avalonia;

namespace Polymerium.Avalonia.Services;

/// <summary>
///     主题状态的唯一权威：持有所有外观相关状态，写入时持久化到 <see cref="ConfigurationService" />
///     并触发 <see cref="ThemeChanged" />。本服务不关心谁在消费，消费方自行读取当前状态并应用。
/// </summary>
public sealed class ThemeService(ConfigurationService configurationService)
{
    public AccentColor Accent
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationStyleAccent = v);
    } = configurationService.Value.ApplicationStyleAccent;

    public int ThemeVariantIndex
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationStyleThemeVariant = v);
    } = configurationService.Value.ApplicationStyleThemeVariant;

    public int TransparencyIndex
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationStyleBackground = v);
    } = configurationService.Value.ApplicationStyleBackground;

    public CornerStyle Corner
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationStyleCorner = v);
    } = configurationService.Value.ApplicationStyleCorner;

    public bool TitleBarVisible
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationTitleBarVisibility = v);
    } = configurationService.Value.ApplicationTitleBarVisibility;

    public bool LeftPanelMode
    {
        get;
        set => Set(ref field, value, v => configurationService.Value.ApplicationLeftPanelMode = v);
    } = configurationService.Value.ApplicationLeftPanelMode;

    /// <summary>
    ///     任意主题状态变化时触发。事件不携带快照，消费方收到通知后自行读取当前状态。
    /// </summary>
    public event EventHandler? ThemeChanged;

    private void Set<T>(ref T field, T value, Action<T> persist)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        persist(value);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
