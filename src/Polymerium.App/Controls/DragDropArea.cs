using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Controls
{
    public class DragDropArea : ContentControl
    {
        protected override void OnDragEnter(DragEventArgs e)
        {
            VisualStateManager.GoToState(this, "DragOver", true);
            base.OnDragEnter(e);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            VisualStateManager.GoToState(this, "Normal", true);
            base.OnDragEnter(e);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            VisualStateManager.GoToState(this, "Normal", true);
            base.OnDrop(e);
        }
    }
}