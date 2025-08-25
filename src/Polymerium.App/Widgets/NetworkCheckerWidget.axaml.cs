using System.Collections.ObjectModel;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;

namespace Polymerium.App.Widgets
{
    public partial class NetworkCheckerWidget : WidgetBase
    {
        public NetworkCheckerWidget() => AvaloniaXamlLoader.Load(this);

        #region Direct

        public ObservableCollection<ConnectionTestSiteModel> Sites { get; } = new();

        #endregion

        #region Commands

        [RelayCommand]
        private void Perform() { }

        #endregion
    }
}
