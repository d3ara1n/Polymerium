using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Views.AddAccountWizard;

namespace Polymerium.App.ViewModels.AddAccountWizard
{
    public class SelectionViewModel: ObservableObject
    {
        public IEnumerable<AccountWizardEntryModel> Entries { get; private set; }
        public SelectionViewModel()
        {
            Entries = new List<AccountWizardEntryModel>()
            {
                new AccountWizardEntryModel()
                {
                    Caption = "离线账号",
                    BrandIconSource = "ms-appx:///Assets/Brands/minecraft.256x256.png",
                    Page = typeof(OfflineAccountView),
                }
            };
        }
    }
}
