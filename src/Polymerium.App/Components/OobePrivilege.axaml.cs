using System;
using System.IO;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using Trident.Abstractions;

namespace Polymerium.App.Components;

public partial class OobePrivilege : OobeStep
{
    public static readonly DirectProperty<OobePrivilege, bool> IsPrivilegeGrantedProperty =
        AvaloniaProperty.RegisterDirect<OobePrivilege, bool>(nameof(IsPrivilegeGranted),
                                                             o => o.IsPrivilegeGranted,
                                                             (o, v) => o.IsPrivilegeGranted = v);

    public OobePrivilege()
    {
        InitializeComponent();
        // Perform initial check
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
        // Logic: Check if we can create a symlink from ~/.trident/.polymerium/symlink to first_run
        var first = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "first_run");
        var symlink = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "symlink");

        // If symlink already exists and points to first_run, we're good
        if (File.Exists(first)
         && File.Exists(symlink)
         && File.ResolveLinkTarget(symlink, false) is { FullName: { } file }
         && first.Equals(file, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        // Ensure directory exists
        var dir = Path.GetDirectoryName(first);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Create first_run file if it doesn't exist
        if (!File.Exists(first))
        {
            File.WriteAllText(first, Program.MagicWords);
        }

        // Remove existing symlink if present
        if (File.Exists(symlink))
        {
            File.Delete(symlink);
        }

        // Try to create symlink
        try
        {
            File.CreateSymbolicLink(symlink, first);
        }
        catch (IOException io) when (io.HResult == -2147023582)
        {
            // ERROR_PRIVILEGE_NOT_HELD - The user doesn't have the required privilege
            return false;
        }
        catch (Exception ex)
        {
            NotificationService?.PopMessage(ex, "Failed to create symlink");
        }

        return true;
    }

    [RelayCommand]
    private void CheckPrivilege() => IsPrivilegeGranted = Check();
}
