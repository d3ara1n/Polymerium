using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public class NotificationService
{
    private static readonly Animation Countdown = new()
    {
        Duration = TimeSpan.FromSeconds(7),
        FillMode = FillMode.Forward,
        Children =
        {
            new()
            {
                Cue = new(0),
                Setters =
                {
                    new Setter { Property = GrowlItem.ProgressProperty, Value = 100d },
                },
            },
            new()
            {
                Cue = new(1),
                Setters =
                {
                    new Setter { Property = GrowlItem.ProgressProperty, Value = 0d },
                },
            },
        },
    };

    private Action<NotificationModel>? _notificationHandler;
    private Action<GrowlItem>? _growlHandler;

    internal void SetHandler(Action<NotificationModel> handler) => _notificationHandler = handler;

    internal void SetHandler(Action<GrowlItem> handler) => _growlHandler = handler;

    private void Pop(NotificationModel model, GrowlItem item)
    {
        item.DismissRequested += OnDismissRequested;
        _notificationHandler?.Invoke(model);
        _growlHandler?.Invoke(item);
        return;

        void OnDismissRequested(object? sender, GrowlItem.DismissRequestedEventArgs args)
        {
            item.DismissRequested -= OnDismissRequested;
            // item.Token 现在是给自己用了，纯 UI 功能，不在和业务相关
            // 提出 model 的意义就是延长通知的生命周期，至少延长到程序关闭（或 model 被移除），所以 handle.Token 应该是应用的 model.Token，后者只有在被移除时（或手动提前）触发
            model.IsRead = true;
        }
    }

    public void PopMessage(
        string message,
        string title = "Notification",
        GrowlLevel level = GrowlLevel.Information,
        bool forceExpire = false,
        params GrowlAction[]? actions
    ) =>
        Dispatcher.UIThread.Post(() =>
        {
            var sharedActions = new AvaloniaList<GrowlAction>(actions ?? []);
            var notification = new NotificationModel
            {
                Title = title,
                Message = message,
                Level = level,
                PublishedAtRaw = DateTimeOffset.Now,
                Thumbnail = null,
                Actions = sharedActions,
            };

            var item = CreateGrowlFromNotificationModel(notification);
            Pop(notification, item);

            if (ShouldExpire(level, actions, forceExpire))
            {
                item.IsProgressBarVisible = true;
                Countdown
                    .RunAsync(item, item.Token)
                    .ContinueWith(
                        _ => item.Dismiss(),
                        TaskScheduler.FromCurrentSynchronizationContext()
                    );
            }
        });

    public void PopMessage(
        Exception? ex,
        string title = "Operation failed",
        GrowlLevel level = GrowlLevel.Danger,
        params GrowlAction[]? actions
    ) =>
        PopMessage(
            ex is not null
                ? Program.IsDebug
                    ? ex.ToString()
                    : ex.Message
                : "Unknown error",
            title,
            level,
            false,
            actions
        );

    public ProgressHandle PopProgress(
        string message,
        string title = "Progress",
        GrowlLevel level = GrowlLevel.Information,
        params GrowlAction[]? actions
    ) =>
        Dispatcher.UIThread.Invoke(() =>
        {
            var sharedActions = new AvaloniaList<GrowlAction>(actions ?? []);
            var notification = new NotificationModel
            {
                Title = title,
                Message = message,
                Level = level,
                PublishedAtRaw = DateTimeOffset.Now,
                Actions = sharedActions,
                Thumbnail = null,
                Progress = 0,
                IsProgressBarVisible = true,
                IsProgressIndeterminate = true,
            };

            var item = CreateGrowlFromNotificationModel(notification);
            Pop(notification, item);

            return new ProgressHandle(notification, item, sharedActions);
        });

    private GrowlItem CreateGrowlFromNotificationModel(NotificationModel notification)
    {
        var item = new GrowlItem
        {
            Content = notification.Message,
            Title = notification.Title,
            Level = notification.Level,
            Progress = notification.Progress,
            IsProgressBarVisible = notification.IsProgressBarVisible,
            IsProgressIndeterminate = notification.IsProgressIndeterminate,
            Actions = notification.Actions,
        };
        return item;
    }

    private static bool ShouldExpire(GrowlLevel level, GrowlAction[]? actions, bool forceExpire) =>
        (
            level is GrowlLevel.Information or GrowlLevel.Warning or GrowlLevel.Success
            && actions is { Length: 0 } or null
        ) || forceExpire;

    #region Nested type: ProgressHandle

    public class ProgressHandle(
        NotificationModel model,
        GrowlItem item,
        AvaloniaList<GrowlAction> actions
    ) : IProgress<double>, IProgress<string>, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public CancellationToken Token => model.Token;

        #region IDisposable Members

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                // 前者时失效
                model.Cancel();
                // 后者消失
                item.Dismiss();
            }
        }

        #endregion

        #region IProgress<double> Members

        public void Report(double value)
        {
            if (!IsDisposed)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    model.Progress = value;
                    model.IsProgressIndeterminate = false;

                    item.Progress = value;
                    item.IsProgressIndeterminate = false;
                });
            }
        }

        #endregion

        #region IProgress<string> Members

        public void Report(string value)
        {
            if (!IsDisposed)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    model.Message = value;

                    item.Content = value;
                });
            }
        }

        #endregion

        // 这个 Action 没法通过构造传入，因为 Action 里可能需要访问构造出来的 Handle 导致提前访问，所以只能用这个别扭的方式
        public void AppendAction(GrowlAction action) => actions.Add(action);
    }

    #endregion
}
