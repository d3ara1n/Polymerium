using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Modals;

public partial class AccountEntryModal : Modal
{
    public AccountEntryModal()
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