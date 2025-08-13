using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Styling;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;

namespace Polymerium.App.Services
{
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
            bool forceExpire = false,
            params NotificationAction[] actions) =>
            Dispatcher.UIThread.Post(() =>
            {
                var item = new NotificationItem { Content = message, Title = title, Level = level };
                item.Actions.AddRange(actions);
                Pop(item);
                if ((level is NotificationLevel.Information or NotificationLevel.Warning or NotificationLevel.Success
                  && actions is { Length: 0 } or null)
                 || forceExpire)
                {
                    item.IsProgressBarVisible = true;
                    COUNTDOWN
                       .RunAsync(item, item.Token)
                       .ContinueWith(_ => item.Close(), TaskScheduler.FromCurrentSynchronizationContext());
                }
            });

        public void PopMessage(
            Exception? ex,
            string title = "Operation failed",
            NotificationLevel level = NotificationLevel.Danger,
            params NotificationAction[] actions) =>
            PopMessage(ex is not null ? Program.Debug ? ex.ToString() : ex.Message : "Unknown error",
                       title,
                       level,
                       false,
                       actions);

        public ProgressHandle PopProgress(
            string message,
            string title = "Progress",
            NotificationLevel level = NotificationLevel.Information) =>
            Dispatcher.UIThread.Invoke(() =>
            {
                var item = new NotificationItem
                {
                    Content = message, Title = title, Level = level, IsProgressBarVisible = true
                };
                Pop(item);

                return new ProgressHandle(item);
            });

        #region Nested type: ProgressHandle

        public class ProgressHandle(NotificationItem item) : IProgress<double>, IProgress<string>, IDisposable
        {
            public bool IsDisposed { get; private set; }

            #region IDisposable Members

            public void Dispose()
            {
                IsDisposed = true;
                item.Close();
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
}
