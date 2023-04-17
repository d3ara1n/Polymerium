using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAdvancedConfigurationView : Page
{
    public InstanceAdvancedConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceAdvancedConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceAdvancedConfigurationViewModel ViewModel { get; }

    private async void DeleteInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = "Really?",
            Text = "这一操作不可撤销"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.DeleteInstance())
                ViewModel.PopNotification("删除完成", "实例文件和其本地对象已被清空");
            else
                ViewModel.PopNotification("删除失败", "意料之外的文件系统错误", InfoBarSeverity.Error);
    }

    private async void ResetInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = XamlRoot,
            Title = "Really?",
            Text = "这一操作不可撤销"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            if (ViewModel.ResetInstance())
                ViewModel.PopNotification("重置完成", "实例文件已被清空");
            else
                ViewModel.PopNotification("重置失败", "意料之外的文件系统错误", InfoBarSeverity.Error);
    }

    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var instance = ViewModel.Context.AssociatedInstance;
        var dialog = new TextInputDialog();
        dialog.Title = "重命名";
        dialog.InputTextPlaceholder = instance!.Name;
        dialog.Description = "这会同样会作用于实例所在目录";
        dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var result = ViewModel.RenameInstance(dialog.InputText);
            if (result.HasValue)
            {
                var errorDialog = new MessageDialog
                {
                    XamlRoot = App.Current.Window.Content.XamlRoot,
                    Title = "重命名失败",
                    Message = result.Value.ToString()
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}
