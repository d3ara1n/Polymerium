using System;
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
            new()
            {
                Cue = new(0),
                Setters = { new Setter { Property = GrowlItem.ProgressProperty, Value = 100d } }
            },
            new()
            {
                Cue = new(1), Setters = { new Setter { Property = GrowlItem.ProgressProperty, Value = 0d } }
            }
        }
    };

    private Action<GrowlItem>? _handler;

    internal void SetHandler(Action<GrowlItem> handler) => _handler = handler;

    public void Pop(GrowlItem item) => _handler?.Invoke(item);

    public void PopMessage(
        string message,
        string title = "Notification",
        GrowlLevel level = GrowlLevel.Information,
        bool forceExpire = false,
        params GrowlAction[] actions)
    {
        var item = new GrowlItem { Content = message, Title = title, Level = level };
        item.Actions.AddRange(actions);
        Pop(item);
        if ((level is GrowlLevel.Information or GrowlLevel.Warning or GrowlLevel.Success
          && actions is { Length: 0 } or null)
         || forceExpire)
        {
            item.IsProgressBarVisible = true;
            COUNTDOWN
               .RunAsync(item, item.Token)
               .ContinueWith(_ => item.Dismiss(), TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public void PopMessage(
        Exception? ex,
        string title = "Operation failed",
        GrowlLevel level = GrowlLevel.Danger,
        params GrowlAction[] actions) =>
        PopMessage(ex is not null ? Program.Debug ? ex.ToString() : ex.Message : "Unknown error",
                   title,
                   level,
                   false,
                   actions);

    public ProgressHandle PopProgress(
        string message,
        string title = "Progress",
        GrowlLevel level = GrowlLevel.Information) =>
        Dispatcher.UIThread.Invoke(() =>
        {
            var item = new GrowlItem { Content = message, Title = title, Level = level, IsProgressBarVisible = true };
            Pop(item);

            return new ProgressHandle(item);
        });

    #region Nested type: ProgressHandle

    public class ProgressHandle(GrowlItem item) : IProgress<double>, IProgress<string>, IDisposable
    {
        public bool IsDisposed { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            IsDisposed = true;
            item.Dismiss();
        }

        #endregion

        #region IProgress<double> Members

        public void Report(double value)
        {
            if (!IsDisposed)
            {
                item.Progress = value;
            }
        }

        #endregion

        #region IProgress<string> Members

        public void Report(string value)
        {
            if (!IsDisposed)
            {
                item.Content = value;
            }
        }

        #endregion
    }

    #endregion
}
