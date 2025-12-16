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

public partial class OobeModal : Modal
{
    public static readonly DirectProperty<OobeModal, object?> CurrentStepProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, object?>(nameof(CurrentStep),
                                                            o => o.CurrentStep,
                                                            (o, v) => o.CurrentStep = v);

    public static readonly DirectProperty<OobeModal, bool> IsReversedProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, bool>(nameof(IsReversed),
                                                         o => o.IsReversed,
                                                         (o, v) => o.IsReversed = v);

    public static readonly DirectProperty<OobeModal, bool> IsBackAvailableProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, bool>(nameof(IsBackAvailable),
                                                         o => o.IsBackAvailable,
                                                         (o, v) => o.IsBackAvailable = v);

    public static readonly DirectProperty<OobeModal, bool> IsLastProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, bool>(nameof(IsLast),
                                                         o => o.IsLast,
                                                         (o, v) => o.IsLast = v);

    public static readonly DirectProperty<OobeModal, int> StepIndexProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(nameof(StepIndex),
                                                        o => o.StepIndex,
                                                        (o, v) => o.StepIndex = v);

    public static readonly DirectProperty<OobeModal, int> StepCountProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(nameof(StepCount),
                                                        o => o.StepCount,
                                                        (o, v) => o.StepCount = v);

    private readonly List<OobeStep> _steps = [];

    public OobeModal() => InitializeComponent();

    public required ConfigurationService ConfigurationService { get; init; }
    public required OverlayService OverlayService { get; init; }
    public NotificationService? NotificationService { get; init; }
    public Action? OnCompleted { get; init; }

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

        _steps.Add(new OobeQuickSetup { ConfigurationService = ConfigurationService, OverlayService = OverlayService });
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
