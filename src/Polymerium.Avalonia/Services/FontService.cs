using System;
using Avalonia;
using Avalonia.Media;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Services;

// 字体配置的唯一应用点。启动 ApplyFromConfiguration 从 Configuration 加载三处字体应用到 Application.Resources；
// 设置页调 Set 更新单个字体并持久化。三处资源键（Main/Code/Log）由 App.axaml 的全局 Style 与各控件 DynamicResource 消费。
public sealed class FontService(ConfigurationService configurationService)
{
    public const string MainKey = "MainFontFamily";
    public const string CodeKey = "CodeFontFamily";
    public const string LogKey = "LogFontFamily";

    public static readonly FontFamily MainFallback = new("fonts:AlimamaFangYuanTi#AlimamaFangYuanTi");

    public static readonly FontFamily CodeFallback = new("Cascadia Code, Consolas, Courier New, monospace");

    public static readonly FontFamily LogFallback = new("Cascadia Code, Consolas, monospace");
    private FontModelBase _code = new DefaultFontModel(CodeFallback);
    private FontModelBase _log = new DefaultFontModel(LogFallback);

    private FontModelBase _main = new DefaultFontModel(MainFallback);

    public FontModelBase Main => _main;

    public FontModelBase Code => _code;

    public FontModelBase Log => _log;

    public void ApplyFromConfiguration()
    {
        _main = FontModelBase.FromConfig(configurationService.Value.ApplicationFontGlobal, MainFallback);
        _code = FontModelBase.FromConfig(configurationService.Value.ApplicationFontCode, CodeFallback);
        _log = FontModelBase.FromConfig(configurationService.Value.ApplicationFontLog, LogFallback);
        ApplyResource(MainKey, _main);
        ApplyResource(CodeKey, _code);
        ApplyResource(LogKey, _log);
    }

    // DefaultFontModel 的 Preview 即 fallback，与 App.axaml 已声明的默认资源值一致——跳过赋值，
    // 避免启动时无意义的全局 DynamicResource 刷新（所有 :is(Control) 控件重求值字体）造成卡顿。
    private static void ApplyResource(string key, FontModelBase selection)
    {
        if (selection is DefaultFontModel)
        {
            return;
        }

        Application.Current!.Resources[key] = selection.Preview;
    }

    public void SetMain(FontModelBase selection) =>
        Apply(MainKey, ref _main, selection, v => configurationService.Value.ApplicationFontGlobal = v);

    public void SetCode(FontModelBase selection) =>
        Apply(CodeKey, ref _code, selection, v => configurationService.Value.ApplicationFontCode = v);

    public void SetLog(FontModelBase selection) =>
        Apply(LogKey, ref _log, selection, v => configurationService.Value.ApplicationFontLog = v);

    private static void Apply(string key, ref FontModelBase cache, FontModelBase selection, Action<string> persist)
    {
        cache = selection;
        persist(selection.Raw);
        Application.Current!.Resources[key] = selection.Preview;
    }
}
