using System;
using Avalonia;
using Avalonia.Media;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Services;

// NOTE: 字体配置的唯一应用点——启动 ApplyFromConfiguration 加载三处字体到 Application.Resources，
//  设置页 Set 更新单个字体并持久化；Main/Code/Log 三键由 App.axaml 全局 Style 与控件 DynamicResource 消费。
public sealed class FontService(ConfigurationService configurationService)
{
    public const string MainKey = "MainFontFamily";
    public const string CodeKey = "CodeFontFamily";
    public const string LogKey = "LogFontFamily";

    public static readonly FontFamily MainFallback = new("fonts:AlimamaFangYuanTi#AlimamaFangYuanTi");

    public static readonly FontFamily CodeFallback = new("Cascadia Code, Menlo, Consolas, Courier New, monospace");

    public static readonly FontFamily LogFallback = new("Cascadia Code, Menlo, Consolas, monospace");
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

    // NOTE: DefaultFontModel.Preview 即 fallback 且与 App.axaml 默认资源一致，跳过赋值，
    //  避免启动时全局 DynamicResource 刷新（所有 :is(Control) 重求值字体）造成卡顿。
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
