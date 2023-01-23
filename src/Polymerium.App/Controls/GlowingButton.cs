using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Polymerium.App.Controls
{
    public class GlowingButton : ButtonBase
    {


        public Color GlowColor
        {
            get { return (Color)GetValue(GlowColorProperty); }
            set { SetValue(GlowColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GlowColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register(nameof(GlowColor), typeof(Color), typeof(GlowingButton), new PropertyMetadata(null));


        public GlowingButton()
        {
            IsEnabledChanged += GlowingButton_IsEnabledChanged;
        }

        private void GlowingButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue?.ToString().ToLower() == "true")
            {
                VisualStateManager.GoToState(this, "Normal", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Disabled", true);
            }
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Pressed", true);
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Normal", true);
            base.OnPointerReleased(e);
        }
    }
}
