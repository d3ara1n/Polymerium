using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.ViewModels;

public partial class UnknownViewModel(ViewBag bag, NotificationService notificationService) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";

    public AvaloniaList<NotificationAction> Actions { get; } =
    [
        new("Information",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null)
                    x.Level = NotificationLevel.Information;
            })),
        new("Success",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null)
                    x.Level = NotificationLevel.Success;
            })),
        new("Warning",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null)
                    x.Level = NotificationLevel.Warning;
            })),
        new("Danger",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null)
                    x.Level = NotificationLevel.Danger;
            }))
    ];

    protected override Task OnInitializedAsync(CancellationToken token) => Task.Delay(TimeSpan.FromSeconds(7), token);

    #region Commands

    [RelayCommand]
    private void Hello() => notificationService.PopMessage("Hello", "Hi there!");

    #endregion
}