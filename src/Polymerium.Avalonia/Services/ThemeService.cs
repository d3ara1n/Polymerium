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
    private AccentColor _accent = configurationService.Value.ApplicationStyleAccent;

    private int _themeVariantIndex = configurationService.Value.ApplicationStyleThemeVariant;

    private int _transparencyIndex = configurationService.Value.ApplicationStyleBackground;

    private CornerStyle _corner = configurationService.Value.ApplicationStyleCorner;

    private bool _titleBarVisible = configurationService.Value.ApplicationTitleBarVisibility;

    private bool _leftPanelMode = configurationService.Value.ApplicationLeftPanelMode;

    public AccentColor Accent
    {
        get => _accent;
        set => Set(ref _accent, value, v => configurationService.Value.ApplicationStyleAccent = v);
    }

    public int ThemeVariantIndex
    {
        get => _themeVariantIndex;
        set => Set(
            ref _themeVariantIndex,
            value,
            v => configurationService.Value.ApplicationStyleThemeVariant = v
        );
    }

    public int TransparencyIndex
    {
        get => _transparencyIndex;
        set => Set(
            ref _transparencyIndex,
            value,
            v => configurationService.Value.ApplicationStyleBackground = v
        );
    }

    public CornerStyle Corner
    {
        get => _corner;
        set => Set(ref _corner, value, v => configurationService.Value.ApplicationStyleCorner = v);
    }

    public bool TitleBarVisible
    {
        get => _titleBarVisible;
        set => Set(
            ref _titleBarVisible,
            value,
            v => configurationService.Value.ApplicationTitleBarVisibility = v
        );
    }

    public bool LeftPanelMode
    {
        get => _leftPanelMode;
        set => Set(
            ref _leftPanelMode,
            value,
            v => configurationService.Value.ApplicationLeftPanelMode = v
        );
    }

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
