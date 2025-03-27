using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel(ViewBag bag, ProfileManager profileManager, InstanceManager instanceManager)
    : InstanceViewModelBase(bag, instanceManager, profileManager)
{
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
        // Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(dir));
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ObservableCollection<InstanceSubpageEntryModel> PageEntries { get; set; } =
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
    public partial InstanceSubpageEntryModel? SelectedPage { get; set; }

    #endregion
}