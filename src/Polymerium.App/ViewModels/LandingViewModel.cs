﻿using System.Threading;
using System.Threading.Tasks;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class LandingViewModel(NavigationService navigationService) : ViewModelBase
{
    #region Lifecycle

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        // This page is always the root page
        navigationService.ClearHistory();
        return base.OnInitializeAsync(token);
    }

    #endregion
}