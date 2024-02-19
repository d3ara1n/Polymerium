using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Controls
{
    public class EntryCard : Button
    {
        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(EntryCard), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for HeaderPadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderPaddingProperty =
            DependencyProperty.Register("HeaderPadding", typeof(Thickness), typeof(EntryCard),
                new PropertyMetadata(new Thickness(0, 0, 0, 0)));


        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }


        public Thickness HeaderPadding
        {
            get => (Thickness)GetValue(HeaderPaddingProperty);
            set => SetValue(HeaderPaddingProperty, value);
        }
    }
}