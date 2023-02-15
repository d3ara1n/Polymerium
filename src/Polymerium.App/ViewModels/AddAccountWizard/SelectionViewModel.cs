using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Views.AddAccountWizards;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class SelectionViewModel : ObservableObject
{
    public SelectionViewModel()
    {
        var entries = new List<AccountWizardEntryModel>
        {
            new(
                "微软账号",
                "ms-appx:///Assets/Icons/Brands/microsoft.256x256.png",
                typeof(MicrosoftAccountIntroView))
        };
#if DEBUG
        entries.Add(new AccountWizardEntryModel(
            "离线账号",
            "ms-appx:///Assets/Icons/Brands/minecraft.256x256.png",
            typeof(OfflineAccountView)
        ));
#endif
        Entries = entries;
    }

    public IEnumerable<AccountWizardEntryModel> Entries { get; }
}