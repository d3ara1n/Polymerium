using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Polymerium.App.Controls
{
    public class IconButton : ButtonBase
    {
        public IconButton()
        {
            IsEnabledChanged += (_, e) =>
            {
                if (e.NewValue?.ToString().ToLower() == "true")
                {
                    VisualStateManager.GoToState(this, IsPointerOver ? "PointerOver" : "Normal", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Disabled", true);
                }
            };
        }
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            if (IsEnabled) VisualStateManager.GoToState(this, "PointerOver", true);
            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            if (IsEnabled && !IsPressed) VisualStateManager.GoToState(this, "Normal", true);
            base.OnPointerExited(e);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            if (IsEnabled) VisualStateManager.GoToState(this, "Pressed", true);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            if (IsPointerOver)
                VisualStateManager.GoToState(this, "PointerOver", true);
            else
                VisualStateManager.GoToState(this, "Normal", true);
            base.OnPointerReleased(e);
        }
    }
}
