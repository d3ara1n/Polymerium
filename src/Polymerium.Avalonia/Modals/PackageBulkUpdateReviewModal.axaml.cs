using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.ModalModels;

namespace Polymerium.Avalonia.Modals;

public partial class PackageBulkUpdateReviewModal : Modal
{
    public PackageBulkUpdateReviewModal() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty && change.NewValue is PackageBulkUpdateReviewModalModel viewModel)
        {
            viewModel.DismissHandler = Dismiss;
        }
    }
}
