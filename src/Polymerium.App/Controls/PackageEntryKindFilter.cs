using Avalonia;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Controls
{
    public class PackageEntryKindFilter : AvaloniaObject
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<PackageEntryKindFilter, string>(nameof(Label));

        public static readonly StyledProperty<ResourceKind?> KindProperty =
            AvaloniaProperty.Register<PackageEntryKindFilter, ResourceKind?>(nameof(Kind));

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public ResourceKind? Kind
        {
            get => GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }
    }
}
