using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;
using Polymerium.App.Views.AddAccountWizard;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels
{
    public partial class AddAccountWizardViewModel : ObservableObject
    {
        internal readonly CancellationTokenSource Source = new();
        public AddAccountWizardViewModel()
        {

        }
    }
}
