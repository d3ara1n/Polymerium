using System;
using System.IO;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia.Components;

public partial class OobePrivilege : OobeStep
{
    public static readonly DirectProperty<OobePrivilege, bool> IsPrivilegeGrantedProperty =
        AvaloniaProperty.RegisterDirect<OobePrivilege, bool>(nameof(IsPrivilegeGranted),
                                                             o => o.IsPrivilegeGranted,
                                                             (o, v) => o.IsPrivilegeGranted = v);

    public OobePrivilege()
    {
        InitializeComponent();
        CheckPrivilege();
    }

    public NotificationService? NotificationService { get; init; }

    public bool IsPrivilegeGranted
    {
        get;
        set => SetAndRaise(IsPrivilegeGrantedProperty, ref field, value);
    }

    /// <summary>
    ///     Checks if the application has the privilege to create symbolic links.
    /// </summary>
    /// <returns>True if symlink creation is allowed, false otherwise.</returns>
    private bool Check()
    {
        // NOTE: 检查能否在 ~/.trident/.polymerium 下创建指向 first_run 的符号链接。
        var first = PathDef.Default.FileOfFirstRun();
        var symlink = PathDef.Default.FileOfSymlink();

        if (File.Exists(first)
         && File.Exists(symlink)
         && File.ResolveLinkTarget(symlink, false) is { FullName: { } file }
         && first.Equals(file, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        var dir = Path.GetDirectoryName(first);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(first))
        {
            File.WriteAllText(first, Program.MagicWords);
        }

        if (File.Exists(symlink))
        {
            File.Delete(symlink);
        }

        try
        {
            File.CreateSymbolicLink(symlink, first);
        }
        catch (IOException io) when (io.HResult == -2147023582)
        {
            // NOTE: ERROR_PRIVILEGE_NOT_HELD —— 用户缺少所需权限。
            return false;
        }
        catch (Exception ex)
        {
            NotificationService?.PopMessage(ex, LanguageManager.Instance.OobePrivilege_CreateSymlinkDangerNotificationTitle.Current());
        }

        return true;
    }

    [RelayCommand]
    private void CheckPrivilege() => IsPrivilegeGranted = Check();
}
