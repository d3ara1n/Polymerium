using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Controls
{
    public class ToggleButtonEx : ToggleButton
    {


        public bool IsInteractive
        {
            get { return (bool)GetValue(IsInteractiveProperty); }
            set { SetValue(IsInteractiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInteractive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInteractiveProperty =
            DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(ToggleButtonEx), new PropertyMetadata(true));

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            if (IsInteractive) base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            if (IsInteractive) base.OnPointerExited(e);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            if (IsInteractive) base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            if (IsInteractive) base.OnPointerReleased(e);
        }
    }
}
