﻿using System.Threading;
using System.Threading.Tasks;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class LandingViewModel(NavigationService navigationService) : ViewModelBase
{
    protected override Task OnInitializedAsync(CancellationToken token)
    {
        // This page is always the root page
        navigationService.ClearHistory();
        return base.OnInitializedAsync(token);
    }
}