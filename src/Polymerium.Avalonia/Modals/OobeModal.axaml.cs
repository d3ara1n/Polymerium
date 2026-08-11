using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Components;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.Modals;

public partial class OobeModal : StepWizardModalBase
{
    public static readonly DirectProperty<OobeModal, int> StepIndexProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(nameof(StepIndex), o => o.StepIndex, (o, v) => o.StepIndex = v);

    public static readonly DirectProperty<OobeModal, int> StepCountProperty =
        AvaloniaProperty.RegisterDirect<OobeModal, int>(nameof(StepCount), o => o.StepCount, (o, v) => o.StepCount = v);

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

        _steps.Add(new OobeWelcome());
        _steps.Add(new OobeFeatures());

        if (OperatingSystem.IsWindows())
        {
            _steps.Add(new OobePrivilege { NotificationService = NotificationService });
        }

        _steps.Add(new OobeQuickSetup
        {
            ConfigurationService = ConfigurationService,
            OverlayService = OverlayService,
            ThemeService = Program.Services!.GetRequiredService<ThemeService>()
        });
        _steps.Add(new OobePrivacy());
        _steps.Add(new OobeFinish());

        StepCount = _steps.Count;

        if (StepIndicator != null)
        {
            StepIndicator.Items.Clear();
            for (var i = 0; i < _steps.Count; i++)
            {
                StepIndicator.Items.Add(new StepItem());
            }
        }

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
