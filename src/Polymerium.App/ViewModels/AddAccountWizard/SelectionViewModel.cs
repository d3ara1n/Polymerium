using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views.AddAccountWizards;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class SelectionViewModel : ObservableObject
{
    public SelectionViewModel(MemoryStorage storage)
    {
        var entries = new List<AccountWizardEntryModel>
        {
            new(
                "微软账号",
                "ms-appx:///Assets/Icons/Brands/microsoft.256x256.png",
                typeof(MicrosoftAccountIntroView))
        };
        if (storage.Accounts.Any(x => x is MicrosoftAccount))
            entries.Add(new AccountWizardEntryModel(
                "离线账号",
                "ms-appx:///Assets/Icons/Brands/minecraft.256x256.png",
                typeof(OfflineAccountView)
            ));
        Entries = entries;
    }

    public IEnumerable<AccountWizardEntryModel> Entries { get; }
}