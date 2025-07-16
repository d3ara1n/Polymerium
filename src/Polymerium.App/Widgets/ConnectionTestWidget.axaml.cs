using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;

namespace Polymerium.App.Widgets;

public partial class ConnectionTestWidget : WidgetBase
{
    #region Direct

    public ObservableCollection<ConnectionTestSiteModel> Sites { get; } = new();

    #endregion

    #region Commands

    [RelayCommand]
    private void Perform() { }

    #endregion
}