using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.App.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class ImportModpackWizardDialog : CustomDialog
{
    public static readonly DependencyProperty IsOperableProperty = DependencyProperty.Register(
        nameof(IsOperable),
        typeof(bool),
        typeof(ImportModpackWizardDialog),
        new PropertyMetadata(false)
    );

    public ImportModpackWizardDialog(string fileName)
    {
        ViewModel = App.Current.Provider.GetRequiredService<ImportModpackWizardViewModel>();
        ViewModel.GotFileName(fileName);
        InitializeComponent();
    }

    public bool IsOperable
    {
        get => (bool)GetValue(IsOperableProperty);
        set => SetValue(IsOperableProperty, value);
    }

    public ImportModpackWizardViewModel ViewModel { get; }

    private void ImportingWizardDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(Root, "Loading", false);
        IsOperable = false;
        Task.Run(async () =>
        {
            var task = ViewModel.ExtractInformationAsync(ReadyHandler);
            await task;
            if (!task.IsCompletedSuccessfully)
            {
                ShowError(task.Exception?.Message ?? "未知错误");
            }
        });
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsOperable = false;
        VisualStateManager.GoToState(Root, "Loading", false);
        if (!string.IsNullOrEmpty(ViewModel.InstanceName))
            ViewModel.Exposed!.Name = ViewModel.InstanceName!;
        Task.Run(() =>
        {
            var task = ViewModel.ApplyExtractionAsync(ReadyHandler);
            task.Wait();
            if (!task.IsCompletedSuccessfully)
            {
                ShowError(task.Exception?.Message ?? "未知错误");
            }
        });
    }

    private void ReadyHandler(Result<ImportResult, GameImportError> result, bool dismiss)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (result.IsErr(out var error))
            {
                Dismiss();
                ShowError(error.ToString());
            }
            else if (result.IsOk(out var import))
            {
                ViewModel.Exposed = import!.Instance;
                ViewModel.InstanceName = ViewModel.Exposed.Name;
                IsOperable = true;
                VisualStateManager.GoToState(Root, "Default", false);
            }

            if (dismiss)
                Dismiss();
        });
    }

    private void ShowError(string error)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            var messageBox = new MessageDialog
            {
                XamlRoot = App.Current.Window.Content.XamlRoot,
                Title = "导入失败",
                Message = $"导入时发生错误，唯一错误参考：{error}"
            };
            await messageBox.ShowAsync();
        });
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.RequestCancel();
        Dismiss();
    }
}