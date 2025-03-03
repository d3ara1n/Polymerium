using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : InstanceViewModelBase
{
    public InstanceViewModel(ViewBag bag, ProfileManager profileManager, InstanceManager instanceManager) :
        base(bag, instanceManager, profileManager)
    { }


    protected override Task OnInitializedAsync(CancellationToken token)
    {
        Dispatcher.UIThread.Post(() => SelectedPage = PageEntries.FirstOrDefault());
        return base.OnInitializedAsync(token);
    }

    #region Reactive

    [ObservableProperty]
    private ObservableCollection<InstanceSubpageEntryModel> _pageEntries =
    [
        // Home
        new(typeof(InstanceHomeView), PackIconLucideKind.LayoutDashboard),
        // Setup or Metadata
        new(typeof(InstanceSetupView), PackIconLucideKind.Boxes),
        // Widgets
        new(typeof(UnknownView), PackIconLucideKind.Blocks),
        // Stats
        new(typeof(UnknownView), PackIconLucideKind.ChartNoAxesCombined),
        // Storage
        new(typeof(UnknownView), PackIconLucideKind.ChartPie),
        // Properties
        new(typeof(InstancePropertyView), PackIconLucideKind.Wrench)
    ];

    [ObservableProperty]
    private InstanceSubpageEntryModel? _selectedPage;

    #endregion
}