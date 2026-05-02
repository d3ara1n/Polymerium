using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Components;
using Polymerium.App.Controls;
using Polymerium.App.Services;

namespace Polymerium.App.Modals;

public partial class OobeModal : StepWizardModalBase
{
    public static readonly DirectProperty<OobeModal, int> StepIndexProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(
            nameof(StepIndex),
            o => o.StepIndex,
            (o, v) => o.StepIndex = v
        );

    public static readonly DirectProperty<OobeModal, int> StepCountProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(
            nameof(StepCount),
            o => o.StepCount,
            (o, v) => o.StepCount = v
        );

    private readonly List<OobeStep> _steps = [];

    public OobeModal() => InitializeComponent();

    public required ConfigurationService ConfigurationService { get; init; }
    public required OverlayService OverlayService { get; init; }
    public NotificationService? NotificationService { get; init; }
    public Action? OnCompleted { get; init; }

    public int StepIndex
    {
        get;
        set => SetAndRaise(StepIndexProperty, ref field, value);
    }

    public int StepCount
    {
        get;
        set => SetAndRaise(StepCountProperty, ref field, value);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Initialize steps
        _steps.Add(new OobeWelcome());
        _steps.Add(new OobeFeatures());

        // Add privilege step only on Windows
        if (OperatingSystem.IsWindows())
        {
            _steps.Add(new OobePrivilege { NotificationService = NotificationService });
        }

        _steps.Add(
            new OobeQuickSetup
            {
                ConfigurationService = ConfigurationService,
                OverlayService = OverlayService,
            }
        );
        _steps.Add(new OobePrivacy());
        _steps.Add(new OobeFinish());

        // Set step count for UI
        StepCount = _steps.Count;

        // Dynamically add StepItems to the StepIndicator
        if (StepIndicator != null)
        {
            StepIndicator.Items.Clear();
            for (var i = 0; i < _steps.Count; i++)
            {
                StepIndicator.Items.Add(new StepItem());
            }
        }

        // Set initial step
        StepIndex = 0;
        CurrentStep = _steps[0];
        IsBackAvailable = false;
        IsLast = false;
    }

    #region Commands

    [RelayCommand]
    private void GoBack()
    {
        if (StepIndex > 0)
        {
            IsReversed = true;
            StepIndex--;
            CurrentStep = _steps[StepIndex];
            IsBackAvailable = StepIndex > 0;
            IsLast = StepIndex == _steps.Count - 1;
        }
    }

    [RelayCommand]
    private void GoNext()
    {
        if (StepIndex < _steps.Count - 1)
        {
            IsReversed = false;
            StepIndex++;
            CurrentStep = _steps[StepIndex];
            IsBackAvailable = StepIndex > 0;
            IsLast = StepIndex == _steps.Count - 1;
        }
    }

    [RelayCommand]
    private void GoFinish()
    {
        OnCompleted?.Invoke();
        Dismiss();
    }

    [RelayCommand]
    private void Skip()
    {
        OnCompleted?.Invoke();
        Dismiss();
    }

    #endregion
}
