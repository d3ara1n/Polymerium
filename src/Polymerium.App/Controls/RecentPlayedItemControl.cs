using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Polymerium.App.Controls
{
    public class RecentPlayedItemControl: ContentControl
    {
        private Border PART_Background;

        protected override void OnApplyTemplate()
        {
            PART_Background = GetTemplateChild("PART_Background") as Border;
            base.OnApplyTemplate();
        }
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            PART_Background.Opacity = 1.0d;
            base.OnPointerEntered(e);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            PART_Background.Opacity = 0.0d;
            base.OnPointerExited(e);
        }
    }
}
