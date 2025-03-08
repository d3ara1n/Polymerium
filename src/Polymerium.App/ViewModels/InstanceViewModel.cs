using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : InstanceViewModelBase
{
    public InstanceViewModel(ViewBag bag, ProfileManager profileManager, InstanceManager instanceManager) :
        base(bag, instanceManager, profileManager) { }


    protected override Task OnInitializedAsync(CancellationToken token)
    {
        Dispatcher.UIThread.Post(() => SelectedPage = PageEntries.FirstOrDefault());
        return base.OnInitializedAsync(token);
    }

    #region Commands

    [RelayCommand]
    private void OpenFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    private partial ObservableCollection<InstanceSubpageEntryModel> PageEntries { get; set; } =
    [
        // Home
        new(typeof(InstanceHomeView), PackIconLucideKind.LayoutDashboard),
        // Setup or Metadata
        new(typeof(InstanceSetupView), PackIconLucideKind.Boxes),
        // Widgets
        new(typeof(InstanceWidgetsView), PackIconLucideKind.Blocks),
        // Statistics
        new(typeof(InstanceStatisticsView), PackIconLucideKind.ChartNoAxesCombined),
        // Storage
        new(typeof(InstanceStorageView), PackIconLucideKind.ChartPie),
        // Properties
        new(typeof(InstancePropertiesView), PackIconLucideKind.Wrench)
    ];

    [ObservableProperty]
    private partial InstanceSubpageEntryModel? SelectedPage { get; set; }

    #endregion
}