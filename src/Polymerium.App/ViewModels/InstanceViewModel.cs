using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Widgets;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel(
    ViewBag bag,
    ProfileManager profileManager,
    InstanceManager instanceManager) : InstanceViewModelBase(bag.Parameter switch
                                                                 {
                                                                     CompositeParameter p => new ViewBag(p.Key),
                                                                     _ => bag
                                                                 },
                                                                 instanceManager,
                                                                 profileManager)
{
    #region Overrides

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        SelectedPage = bag.Parameter switch
                       {
                           CompositeParameter p => PageEntries.FirstOrDefault(x => x.Page == p.Subview),
                           _ => null
                       }
                    ?? PageEntries.FirstOrDefault();

        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(dir));
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ObservableCollection<InstanceSubpageEntryModel> PageEntries { get; set; } =
    [
        // Home
        new(typeof(InstanceHomeView), PackIconLucideKind.LayoutDashboard),
        // Setup
        new(typeof(InstanceSetupView), PackIconLucideKind.Boxes),
        // Widgets
        new(typeof(InstanceWidgetsView), PackIconLucideKind.Blocks),
        // Statistics
        new(typeof(InstanceActivitiesView), PackIconLucideKind.ChartNoAxesCombined),
        // Storage
        new(typeof(InstanceStorageView), PackIconLucideKind.ChartPie),
        // Properties
        new(typeof(InstancePropertiesView), PackIconLucideKind.Wrench)
    ];

    [ObservableProperty]
    public partial InstanceSubpageEntryModel? SelectedPage { get; set; }

    #endregion

    #region Nested type: CompositeParameter

    public record CompositeParameter(string Key, Type Subview);

    #endregion
}