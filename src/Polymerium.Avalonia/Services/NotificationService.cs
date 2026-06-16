using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Services;

public partial class NotificationService : ObservableObject
{
    private const int MAX_NOTIFICATION_COUNT = 100;

    // 持久通知集合：app 级单例持有，NotificationSidebar 直接绑定，不受窗口生命周期影响。
    public ObservableCollection<NotificationModel> Notifications { get; } = [];

    [ObservableProperty]
    public partial int UnreadNotificationCount { get; set; }

    [RelayCommand]
    private void MarkAllAsRead()
    {
        foreach (var model in Notifications.Where(x => !x.IsRead))
        {
            model.IsRead = true;
        }

        UnreadNotificationCount = 0;
    }

    [RelayCommand]
    private void MarkAsRead(NotificationModel? model)
    {
        if (model is { IsRead: false })
        {
            model.IsRead = true;
            UnreadNotificationCount--;
        }
    }

    [RelayCommand]
    private void MarkAsUnread(NotificationModel? model)
    {
        if (model is { IsRead: true })
        {
            model.IsRead = false;
            UnreadNotificationCount++;
        }
    }

    [RelayCommand]
    private void RemoveNotification(NotificationModel? model)
    {
        if (model is not null && Notifications.Contains(model))
        {
            model.OnRemoved();
            Notifications.Remove(model);
            if (!model.IsRead)
            {
                UnreadNotificationCount--;
            }
        }
    }

    public void PopNotification(NotificationModel model)
    {
        if (Notifications.Count >= MAX_NOTIFICATION_COUNT)
        {
            var first = Notifications.FirstOrDefault();
            if (first != null)
            {
                first.OnRemoved();
                Notifications.Remove(first);
            }
        }

        Notifications.Add(model);
        UnreadNotificationCount++;
    }

    /// <summary>
    ///     清理所有通知的资源（应用退出时调用）。
    /// </summary>
    public void ClearAll()
    {
        foreach (var model in Notifications)
        {
            model.OnRemoved();
        }
    }
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
        // 持久通知记录：永远写，不受窗口生命周期影响
        _notificationHandler?.Invoke(model);

        // growl 弹窗：通过网关检查是否有活跃 TopLevel
        // TODO(B): 无窗口时通过 TrayIcon / macOS Notification Center 发系统通知
        //   现状：无窗口时 growl 静默丢弃（持久记录照写），崩溃诊断也转持久记录
        if (_growlHandler is not null)
        {
            _growlHandler.Invoke(item);
        }
    }

    public void PopMessage(
        string message,
        string title = "Notification",
        GrowlLevel level = GrowlLevel.Information,
        bool forceExpire = false,
        Uri? thumbnail = null,
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
                Thumbnail = thumbnail,
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
        Uri? thumbnail = null,
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
            thumbnail,
            actions
        );

    public ProgressHandle PopProgress(
        string message,
        string title = "Progress",
        GrowlLevel level = GrowlLevel.Information,
        Uri? thumbnail = null,
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
                Thumbnail = thumbnail,
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

        #region Other Setters
        // 这个 Action 没法通过构造传入，因为 Action 里可能需要访问构造出来的 Handle 导致提前访问，所以只能用这个别扭的方式
        public void AddAction(GrowlAction action) => actions.Add(action);

        public void SetThumbnail(Uri? source) => model.Thumbnail = source;
        #endregion
    }

    #endregion
}
