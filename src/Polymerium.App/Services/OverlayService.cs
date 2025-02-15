using System;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Services;

public class OverlayService
{
    private Action<Toast>? _toastHandler;
    private Action<Modal>? _modalHandler;
    private Action<Dialog>? _dialogHandler;
    // private Action<Drawer>? _drawerHandler;

    internal void SetHandler(Action<Toast> toastHandler, Action<Modal> modalHandler, Action<Dialog> dialogHandler)
    {
        _toastHandler = toastHandler;
        _modalHandler = modalHandler;
        _dialogHandler = dialogHandler;
    }

    #region Toasts

    public void PopToast(Toast toast)
    {
        Dispatcher.UIThread.Post(() => _toastHandler?.Invoke(toast));
    }

    #endregion

    #region Modals

    public void PopModal(Modal modal)
    {
        Dispatcher.UIThread.Post(() => _modalHandler?.Invoke(modal));
    }

    #endregion

    #region Dialogs

    public void PopDialog(Dialog dialog)
    {
        Dispatcher.UIThread.Post(() => _dialogHandler?.Invoke(dialog));
    }

    #endregion
}