// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels.AddAccountWizard;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Polymerium.App.Views.AddAccountWizard
{
    public sealed partial class OfflineAccountView : Page
    {
        public OfflineAccountViewModel ViewModel { get; private set; }
        private AddAccountWizardStateHandler handler;
        public OfflineAccountView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Provider.GetRequiredService<OfflineAccountViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            (handler, _) = ((AddAccountWizardStateHandler, CancellationToken))e.Parameter;
            handler(null, true, Finish);
            base.OnNavigatedTo(e);
        }

        private bool Finish()
        {
            var errors = ViewModel.GetErrors();
            if (errors != null && errors.Any())
            {
                ViewModel.ErrorMessage = string.Join('\n', errors.Select(x => x.ErrorMessage));
                return false;
            }
            else
            {
                ViewModel.ErrorMessage = string.Empty;
                ViewModel.Register();
                return true;
            }
        }
    }
}
