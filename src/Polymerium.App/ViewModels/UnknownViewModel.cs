using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public class UnknownViewModel(ViewBag bag) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";

    public AvaloniaList<NotificationAction> Actions { get; } =
    [
        new("Information",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null) x.Level = NotificationLevel.Information;
            })),
        new("Success",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null) x.Level = NotificationLevel.Success;
            })),
        new("Warning",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null) x.Level = NotificationLevel.Warning;
            })),
        new("Danger",
            new RelayCommand<NotificationItem>(x =>
            {
                if (x is not null) x.Level = NotificationLevel.Danger;
            }))
    ];

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        return Task.Delay(TimeSpan.FromSeconds(7), token);
    }
}