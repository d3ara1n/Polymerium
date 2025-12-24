using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ObservableCollections;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Fields

    #endregion

    #region Reactive

    public ObservableCollection<LogSourceModelBase> Sources { get; } = [];

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync()
    {
        RefreshLogSources();
        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    #endregion

    #region Other

    private void RefreshLogSources() { }
    private void UpdateLogSource() { }

    #endregion
}
