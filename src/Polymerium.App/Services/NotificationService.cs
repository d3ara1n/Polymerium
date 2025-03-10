﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Styling;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;

namespace Polymerium.App.Services;

public class NotificationService
{
    private static readonly Animation COUNTDOWN = new()
    {
        Duration = TimeSpan.FromSeconds(7),
        FillMode = FillMode.Forward,
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0),
                Setters = { new Setter { Property = NotificationItem.ProgressProperty, Value = 100d } }
            },
            new KeyFrame
            {
                Cue = new Cue(1),
                Setters = { new Setter { Property = NotificationItem.ProgressProperty, Value = 0d } }
            }
        }
    };

    private Action<NotificationItem>? _handler;

    internal void SetHandler(Action<NotificationItem> handler) => _handler = handler;

    public void Pop(NotificationItem item) => Dispatcher.UIThread.Post(() => _handler?.Invoke(item));

    public void PopMessage(
        string message,
        string title = "Notification",
        NotificationLevel level = NotificationLevel.Information,
        bool forceExpire = false) =>
        Dispatcher.UIThread.Post(() =>
        {
            NotificationItem? item = new() { Content = message, Title = title, Level = level };
            Pop(item);
            if (level is NotificationLevel.Information or NotificationLevel.Warning or NotificationLevel.Success
             || forceExpire)
            {
                item.IsProgressBarVisible = true;
                COUNTDOWN
                   .RunAsync(item)
                   .ContinueWith(_ => item.IsOpen = false, TaskScheduler.FromCurrentSynchronizationContext());
            }
        });

    public void PopMessage(
        Exception ex,
        string title = "Operation failed",
        NotificationLevel level = NotificationLevel.Danger) =>
        PopMessage(Debugger.IsAttached ? ex.ToString() : ex.Message, title, level);

    public void PopProgress(
        string message,
        string title = "Progress",
        NotificationLevel level = NotificationLevel.Information) =>
        Dispatcher.UIThread.Post(() =>
        {
            NotificationItem? item = new()
            {
                Content = message, Title = title, Level = level, IsProgressBarVisible = true
            };
            // TODO: return IProgressReporter
            Pop(item);
        });
}