// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels.AddAccountWizard;

namespace Polymerium.App.Views.AddAccountWizards;

public sealed partial class OfflineAccountView : Page
{
    private AddAccountWizardStateHandler? handler;

    public OfflineAccountView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<OfflineAccountViewModel>();
        InitializeComponent();
    }

    public OfflineAccountViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        (handler, _) = ((AddAccountWizardStateHandler, CancellationToken))e.Parameter;
        handler?.Invoke(null, true, Finish);
        base.OnNavigatedTo(e);
    }

    private bool Finish()
    {
        var errors = ViewModel.GetErrors();
        var validationResults = errors as ValidationResult[] ?? errors.ToArray();
        if (validationResults.Any())
        {
            ViewModel.ErrorMessage = string.Join(
                '\n',
                validationResults.Select(x => x.ErrorMessage)
            );
            return false;
        }

        ViewModel.ErrorMessage = string.Empty;
        ViewModel.Register();
        return true;
    }
}
