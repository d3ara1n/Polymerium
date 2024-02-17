using Polymerium.App.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Modals;

public sealed partial class ExhibitPreviewModal
{
    public ExhibitPreviewModal(ExhibitModel model)
    {
        Model = model;
        InitializeComponent();
    }

    public ExhibitModel Model { get; }
}