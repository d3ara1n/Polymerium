// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.AddAccountWizard;
using System.Linq;
using System.Threading;

namespace Polymerium.App.Views.AddAccountWizard
{
    public sealed partial class SelectionView : Page
    {
        public SelectionViewModel ViewModel { get; private set; }

        private AddAccountWizardStateHandler handler;

        public SelectionView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Provider.GetRequiredService<SelectionViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            (handler, _) = ((AddAccountWizardStateHandler, CancellationToken))e.Parameter;
            base.OnNavigatedTo(e);
        }

        private void FirstPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            handler((e.AddedItems.First() as AccountWizardEntryModel).Page);
        }
    }
}