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

        private AttachedCardShadow _shadow;
        private ContentPresenter _container;
        protected override void OnApplyTemplate()
        {
            _shadow = GetTemplateChild("Shadow") as AttachedCardShadow;
            _container = GetTemplateChild("Container") as ContentPresenter;
            base.OnApplyTemplate();
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            _shadow.Opacity = 1.0;
            _container.Foreground = new SolidColorBrush()
            {
                Color = Color.FromArgb(255, 255, 255, 255),
            };
            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            _shadow.Opacity = 0.0;
            _container.Foreground = Foreground;
            base.OnPointerExited(e);
        }
    }
}
