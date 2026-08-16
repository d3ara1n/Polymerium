using System;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.ModalModels;

namespace Polymerium.Avalonia.Modals;

public partial class ExhibitProjectModal : Modal
{
    public ExhibitProjectModal() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty && change.NewValue is ExhibitProjectModalModel viewModel)
        {
            viewModel.DismissHandler = Dismiss;
        }
    }
}
