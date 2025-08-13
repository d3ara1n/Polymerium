using System;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Services;
using Trident.Abstractions;

namespace Polymerium.App.Modals
{
    public partial class PrivilegeRequirementModal : Modal
    {
        public PrivilegeRequirementModal() => InitializeComponent();

        #region Services

        public required NotificationService NotificationService { get; init; }

        #endregion

        #region Other

        public bool Check()
        {
            // 这里的逻辑是通过 ~/.trident/.polymerium/first_run 文件判断是否完成初次启动检查
            // 通过对上述文件创建 ~/.trident.polymerium/symlink 的文件链接判断是否已完成提权设置

            var first = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "first_run");
            var symlink = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "symlink");

            if (File.Exists(first)
             && File.Exists(symlink)
             && File.ResolveLinkTarget(symlink, false) is { FullName: { } file }
             && first.Equals(file, StringComparison.InvariantCultureIgnoreCase))
                // 曾经的版本或是神秘力量完成了测试
            {
                return true;
            }

            var dir = Path.GetDirectoryName(first);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(first))
                // 这里是没有用 try catch guard 的，要是出现异常奔溃那我没话说
            {
                File.WriteAllText(first, "say u say me");
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
                return false;
            }
            catch (Exception ex)
            {
                NotificationService.PopMessage(ex, "Failed to create symlink");
            }

            return true;
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void Dismiss() => RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));

        [RelayCommand]
        private void Done()
        {
            if (Check())
            {
                RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
            }
            else
            {
                NotificationService.PopMessage("Negative. Try again or just [ignore].");
            }
        }

        #endregion
    }
}
