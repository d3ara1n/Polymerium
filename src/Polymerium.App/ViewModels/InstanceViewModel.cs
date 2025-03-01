using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ViewModelBase
{
    public InstanceViewModel(ViewBag bag, ProfileManager profileManager, InstanceManager instanceManager)
    {
        _profileManager = profileManager;
        _instanceManager = instanceManager;
        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
                Basic = new InstanceBasicModel(key,
                                               profile.Name,
                                               profile.Setup.Version,
                                               profile.Setup.Loader,
                                               profile.Setup.Source);
            else
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }
    }

    private void UpdateModels(string key, Profile profile)
    {
        Basic = new InstanceBasicModel(key,
                                       profile.Name,
                                       profile.Setup.Version,
                                       profile.Setup.Loader,
                                       profile.Setup.Source);
    }


    #region Injected

    private readonly ProfileManager _profileManager;
    private readonly InstanceManager _instanceManager;

    #endregion

    #region Tracking

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        _instanceManager.InstanceUpdating += OnProfileUpdating;
        _profileManager.ProfileUpdated += OnProfileUpdated;
        if (_instanceManager.IsTracking(Basic.Key, out var tracker))
            if (tracker is UpdateTracker update)
            {
                // 已经处于更新状态而未收到事件
                State = InstanceState.Updating;
                update.StateUpdated += OnProfileUpdateStateChanged;
            }

        Dispatcher.UIThread.Post(() => SelectedPage = PageEntries.FirstOrDefault());
        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _instanceManager.InstanceUpdating -= OnProfileUpdating;
        _profileManager.ProfileUpdated -= OnProfileUpdated;

        return Task.CompletedTask;
    }

    private void OnProfileUpdateStateChanged(TrackerBase sender, TrackerState state)
    {
        if (sender is UpdateTracker update)
            if (state is TrackerState.Faulted or TrackerState.Finished)
            {
                update.StateUpdated -= OnProfileUpdateStateChanged;
                Dispatcher.UIThread.Post(() => State = InstanceState.Idle);
                // 更新的事情交给 ProfileManager.ProfileUpdated
            }
    }

    private void OnProfileUpdating(object? sender, UpdateTracker tracker)
    {
        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnProfileUpdateStateChanged;
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        UpdateModels(e.Key, e.Value);
    }

    #endregion

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

    [ObservableProperty]
    private InstanceBasicModel _basic;

    [ObservableProperty]
    private InstanceState _state;

    #endregion
}