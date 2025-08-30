using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Components;
using Polymerium.App.Controls;
using Trident.Core.Services;
using Trident.Abstractions.Accounts;

namespace Polymerium.App.Modals;

public partial class AccountCreationModal : Modal
{
    public static readonly DirectProperty<AccountCreationModal, object?> CurrentStepProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationModal, object?>(nameof(CurrentStep),
                                                                       o => o.CurrentStep,
                                                                       (o, v) => o.CurrentStep = v);

    public static readonly DirectProperty<AccountCreationModal, bool> IsReversedProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationModal, bool>(nameof(IsReversed),
                                                                    o => o.IsReversed,
                                                                    (o, v) => o.IsReversed = v);

    public static readonly DirectProperty<AccountCreationModal, bool> IsBackAvailableProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationModal, bool>(nameof(IsBackAvailable),
                                                                    o => o.IsBackAvailable,
                                                                    (o, v) => o.IsBackAvailable = v);

    public static readonly DirectProperty<AccountCreationModal, bool> IsLastProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationModal, bool>(nameof(IsLast),
                                                                    o => o.IsLast,
                                                                    (o, v) => o.IsLast = v);

    private readonly Stack<object> _history = new();

    public AccountCreationModal() => InitializeComponent();

    public required MicrosoftService MicrosoftService { get; init; }
    public required XboxLiveService XboxLiveService { get; init; }
    public required MinecraftService MinecraftService { get; init; }

    public object? CurrentStep
    {
        get;
        set => SetAndRaise(CurrentStepProperty, ref field, value);
    }

    public bool IsReversed
    {
        get;
        set => SetAndRaise(IsReversedProperty, ref field, value);
    }

    public bool IsBackAvailable
    {
        get;
        set => SetAndRaise(IsBackAvailableProperty, ref field, value);
    }

    public bool IsLast
    {
        get;
        set => SetAndRaise(IsLastProperty, ref field, value);
    }

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
            MinecraftService = MinecraftService
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
