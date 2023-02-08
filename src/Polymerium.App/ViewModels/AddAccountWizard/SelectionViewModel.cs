using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Views.AddAccountWizards;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class SelectionViewModel : ObservableObject
{
    public SelectionViewModel()
    {
        Entries = new List<AccountWizardEntryModel>
        {
            new("离线账号", "ms-appx:///Assets/Icons/Brands/minecraft.256x256.png", typeof(OfflineAccountView))
        };
    }

    public IEnumerable<AccountWizardEntryModel> Entries { get; }
}