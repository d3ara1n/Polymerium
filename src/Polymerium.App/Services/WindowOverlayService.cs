using Microsoft.UI.Xaml.Controls;
using System;

namespace Polymerium.App.Services;

public delegate void OverlayShowHandler(ContentControl content);

public delegate ContentControl OverlayDismissHandler();

public class WindowOverlayService : IOverlayService
{
    private OverlayDismissHandler _dismissHandler;
    private OverlayShowHandler _showHandler;

    public void Show(ContentControl content)
    {
        if (_showHandler != null)
            _showHandler(content);
        else
            throw new ArgumentNullException(nameof(_showHandler));
    }

    public ContentControl Dismiss()
    {
        if (_dismissHandler != null)
            return _dismissHandler();
        else
            throw new ArgumentNullException(nameof(_dismissHandler));
    }

    public void Register(OverlayShowHandler showHandler, OverlayDismissHandler dismissHandler)
    {
        _showHandler = showHandler;
        _dismissHandler = dismissHandler;
    }

    public void Unregister(OverlayShowHandler showHandler, OverlayDismissHandler dismissHandler)
    {
        if (showHandler == _showHandler && dismissHandler == _dismissHandler)
        {
            _showHandler = null;
            _dismissHandler = null;
        }
    }
}