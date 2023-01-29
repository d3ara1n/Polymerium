using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Services;

public interface IOverlayService
{
    public void Show(ContentControl content);

    public ContentControl Dismiss();
}