using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Components;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using TridentCore.Abstractions.Accounts;
using TridentCore.Core.Services;

namespace Polymerium.App.Modals;

public partial class AccountCreationModal : StepWizardModalBase
{
    private readonly Stack<object> _history = new();

    public AccountCreationModal() => InitializeComponent();

    public required MicrosoftService MicrosoftService { get; init; }
    public required XboxLiveService XboxLiveService { get; init; }
    public required MinecraftService MinecraftService { get; init; }
    public required NotificationService NotificationService { get; init; }
    public required YggdrasilService YggdrasilService { get; init; }

    public required bool IsOfflineAvailable { get; init; }
    public required Func<IAccount, bool> FinishCallback { get; init; }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        CurrentStep = new AccountCreationPortal
        {
            IsOfflineAvailable = IsOfflineAvailable,
            MicrosoftService = MicrosoftService,
            XboxLiveService = XboxLiveService,
            MinecraftService = MinecraftService,
            NotificationService = NotificationService,
            YggdrasilService = YggdrasilService,
        };
    }

    #region Commands

    [RelayCommand]
    private void GoBack()
    {
        IsReversed = true;
        CurrentStep = _history.Pop();
        IsBackAvailable = _history.Count > 0;
        IsLast = false;
    }

    [RelayCommand]
    private void GoNext()
    {
        var step = CurrentStep as AccountCreationStep;
        if (step == null)
        {
            return;
        }

        if (CurrentStep != null)
        {
            _history.Push(CurrentStep);
        }

        IsBackAvailable = _history.Count > 0;
        IsReversed = false;
        var next = step.NextStep();
        IsLast = next is AccountCreationPreview;
        CurrentStep = next;
    }

    [RelayCommand]
    private void GoFinish()
    {
        if (CurrentStep is AccountCreationPreview { Account: { } account } preview)
        {
            if (FinishCallback(account))
            {
                Dismiss();
            }
        }
    }

    #endregion
}
