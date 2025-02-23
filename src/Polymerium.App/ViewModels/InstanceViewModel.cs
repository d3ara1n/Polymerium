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

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ViewModelBase
{
    public InstanceViewModel(ViewBag bag, ProfileManager profileManager)
    {
        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                Basic = new InstanceBasicModel(key,
                                               profile.Name,
                                               profile.Setup.Version,
                                               profile.Setup.Loader,
                                               profile.Setup.Source);
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }
    }

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        Dispatcher.UIThread.Post(() => SelectedPage = PageEntries.FirstOrDefault());
        return base.OnInitializedAsync(token);
    }


    #region Commands

    #endregion

    #region Direct

    #endregion

    #region Reactive

    [ObservableProperty]
    private ObservableCollection<InstanceSubpageEntryModel> _pageEntries =
    [
        // Home
        new(typeof(InstanceHomeView), PackIconLucideKind.House),
        // Setup or Metadata
        new(typeof(InstanceSetupView), PackIconLucideKind.Blocks),
        // Widgets
        new(typeof(UnknownView), PackIconLucideKind.Backpack),
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

    #endregion
}