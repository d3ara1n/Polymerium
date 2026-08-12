using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Polymerium.Avalonia.Controls;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia.Components;

public partial class OobePrivacy : OobeStep
{
    public static readonly StyledProperty<bool> IsCrashReportingEnabledProperty =
        AvaloniaProperty.Register<OobePrivacy, bool>(nameof(IsCrashReportingEnabled), defaultValue: true);

    public OobePrivacy() => InitializeComponent();

    // NOTE: opt-out 开关。_no_telemetry_ 文件存在即跳过 SentrySdk.Init（见 Startup），
    //  与 SettingsPageModel.CrashReportingEnabled 共用同一文件作为唯一事实源。
    public bool IsCrashReportingEnabled
    {
        get => GetValue(IsCrashReportingEnabledProperty);
        set => SetValue(IsCrashReportingEnabledProperty, value);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        IsCrashReportingEnabled = !File.Exists(PathDef.Default.FileOfTelemetrySwitch());
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCrashReportingEnabledProperty)
        {
            SyncTelemetrySwitch((bool)change.NewValue);
        }
    }

    private static void SyncTelemetrySwitch(bool enabled)
    {
        var file = PathDef.Default.FileOfTelemetrySwitch();
        try
        {
            if (enabled)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            else if (!File.Exists(file))
            {
                var dir = Path.GetDirectoryName(file);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(file, Program.MagicWords);
            }
        }
        catch
        {
            // OOBE 阶段无 logger，写失败不影响流程。
        }
    }
}
