using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Modals;

public partial  class PrivilegeRequirementModal : Modal
{
    public PrivilegeRequirementModal()
    {
        InitializeComponent();
    }

    #region Commands

    [RelayCommand]
    private void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    #endregion
}