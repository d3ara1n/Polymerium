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
            base.OnApplyTemplate();
            PART_Background = GetTemplateChild("PART_Background") as Border;
        }
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            PART_Background.Opacity = 1.0d;
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            PART_Background.Opacity = 0.0d;
        }
    }
}
