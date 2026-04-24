using System;
using System.IO;
using System.Threading.Tasks;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Dialogs;

namespace Polymerium.App.Services;

public class OverlayService(IViewActivator activator)
{
    private Action<Dialog>? _dialogHandler;
    private Action<Sidebar>? _drawerHandler;
    private Action<Modal>? _modalHandler;
    private Action<Toast>? _toastHandler;

    internal void SetHandler(
        Action<Toast> toastHandler,
        Action<Sidebar> drawerHandler,
        Action<Modal> modalHandler,
        Action<Dialog> dialogHandler
    )
    {
        _toastHandler = toastHandler;
        _drawerHandler = drawerHandler;
        _modalHandler = modalHandler;
        _dialogHandler = dialogHandler;
    }

    #region Toasts

    public void PopToast(Toast toast) => _toastHandler?.Invoke(toast);

    #endregion

    #region Drawers

    public void PopSidebar(Sidebar sidebar) => _drawerHandler?.Invoke(sidebar);

    #endregion

    #region Modals

    public void PopModal(Modal modal) => _modalHandler?.Invoke(modal);

    public void PopModal<TModal>(object? parameter = null)
        where TModal : Modal
    {
        var modal = (TModal)activator.Activate(typeof(TModal), parameter)!;
        PopModal(modal);
    }

    #endregion

    #region Dialogs

    public void PopDialog(Dialog dialog) => _dialogHandler?.Invoke(dialog);

    public void PopMessage(string message, string title)
    {
        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            IsPrimaryButtonVisible = false,
        };
        PopDialog(dialog);
    }

    public async Task<bool> PopDialogAsync(Dialog dialog)
    {
        var source = dialog.CompletionSource;
        _dialogHandler?.Invoke(dialog);
        return await source.Task;
    }

    public async Task<string?> RequestInputAsync(
        string? message = null,
        string? title = null,
        string? placeholder = null
    )
    {
        var dialog = new UserInputDialog();
        if (title != null)
        {
            dialog.Title = title;
        }

        if (message != null)
        {
            dialog.Message = message;
        }

        if (placeholder != null)
        {
            dialog.PlaceholderText = placeholder;
            dialog.PresetText = placeholder;
        }

        if (await PopDialogAsync(dialog) && dialog.Result is string input)
        {
            return input;
        }

        return null;
    }

    public async Task<bool> RequestConfirmationAsync(string? message = null, string? title = null)
    {
        var dialog = new MessageDialog { IsPrimaryButtonVisible = true };
        if (title != null)
        {
            dialog.Title = title;
        }

        if (message != null)
        {
            dialog.Message = message;
        }

        return await PopDialogAsync(dialog);
    }

    public async Task<string?> RequestFileAsync(string? message = null, string? title = null)
    {
        var dialog = new FilePickerDialog();
        if (title != null)
        {
            dialog.Title = title;
        }

        if (message != null)
        {
            dialog.Message = message;
        }

        if (await PopDialogAsync(dialog) && dialog.Result is string path && File.Exists(path))
        {
            return path;
        }

        return null;
    }

    #endregion
}
