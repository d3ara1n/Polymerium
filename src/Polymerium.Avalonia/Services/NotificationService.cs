using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Styling;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Services;

// NOTE: 门面服务：持有 app 级单例 canonical 通知数据，只暴露命令式方法与状态事件；
//  不暴露可绑定集合/命令——View 绑定一律经 NotificationSidebarViewModel 投影。
public class NotificationService
{
    private const int MAX_NOTIFICATION_COUNT = 100;

    private static readonly Animation Countdown = new()
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

    // NOTE: canonical 数据为 app 级单例，不受窗口生命周期影响；仅本类管理方法可变更。
    private readonly ObservableCollection<NotificationModel> _notifications = [];
    private Action<GrowlItem>? _growlHandler;

    private Action<NotificationModel>? _notificationHandler;

    /// <summary>
    ///     当前未读通知数。仅在 UI 线程上通过本类的管理方法变更。
    /// </summary>
    public int UnreadCount { get; private set; }

    /// <summary>
    ///     返回当前通知快照（只读出口），供 ViewModel 做初始填充。
    ///     返回的是同一批 NotificationModel 引用，逐项属性（如 IsRead）由 model 自身可观察，无需 VM 转发。
    /// </summary>
    public IReadOnlyList<NotificationModel> GetSnapshot() => _notifications;

    internal void SetHandler(Action<NotificationModel> handler) => _notificationHandler = handler;

    internal void SetHandler(Action<GrowlItem> handler) => _growlHandler = handler;

    private void Pop(NotificationModel model, GrowlItem item)
    {
        // NOTE: 持久通知记录永远写，不受窗口生命周期影响。
        _notificationHandler?.Invoke(model);

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
        params GrowlAction[]? actions) =>
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
                Actions = sharedActions
            };

            var item = CreateGrowlFromNotificationModel(notification);
            Pop(notification, item);

            if (ShouldExpire(level, actions, forceExpire))
            {
                item.IsProgressBarVisible = true;
                Countdown
                   .RunAsync(item, item.Token)
                   .ContinueWith(_ => item.Dismiss(), TaskScheduler.FromCurrentSynchronizationContext());
            }
        });

    public void PopMessage(
        Exception? ex,
        string title = "Operation failed",
        GrowlLevel level = GrowlLevel.Danger,
        Uri? thumbnail = null,
        params GrowlAction[]? actions) =>
        PopMessage(ex is not null ? Program.IsDebug ? ex.ToString() : ex.Message : "Unknown error",
                   title,
                   level,
                   false,
                   thumbnail,
                   actions);

    public ProgressHandle PopProgress(
        string message,
        string title = "Progress",
        GrowlLevel level = GrowlLevel.Information,
        Uri? thumbnail = null,
        params GrowlAction[]? actions) =>
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
                IsProgressIndeterminate = true
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
            Actions = notification.Actions
        };
        return item;
    }

    private static bool ShouldExpire(GrowlLevel level, GrowlAction[]? actions, bool forceExpire) =>
        (level is GrowlLevel.Information or GrowlLevel.Warning or GrowlLevel.Success
      && actions is { Length: 0 } or null)
     || forceExpire;

    #region Nested type: ProgressHandle

    public class ProgressHandle(NotificationModel model, GrowlItem item, AvaloniaList<GrowlAction> actions)
        : IProgress<double>, IProgress<string>, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public CancellationToken Token => model.Token;

        #region IDisposable Members

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                model.Cancel();
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

        // NOTE: Action 不能经构造传入——构造期内引用 Handle 属提前访问，只能事后挂载。
        public void AddAction(GrowlAction action) => actions.Add(action);

        public void SetThumbnail(Uri? source) => model.Thumbnail = source;

        #endregion
    }

    #endregion

    #region Events

    // NOTE: 所有事件均假定在 UI 线程触发（PopMessage 已 marshal 到 UI 线程）。
    public event Action<NotificationModel>? NotificationAdded;
    public event Action<NotificationModel>? NotificationRemoved;
    public event Action<NotificationModel>? NotificationReadChanged;
    public event Action<int>? UnreadCountChanged;

    #endregion

    #region Management

    public void MarkAllAsRead()
    {
        foreach (var model in _notifications.Where(x => !x.IsRead))
        {
            model.IsRead = true;
            NotificationReadChanged?.Invoke(model);
        }

        if (UnreadCount != 0)
        {
            UnreadCount = 0;
            UnreadCountChanged?.Invoke(UnreadCount);
        }
    }

    public void MarkAsRead(NotificationModel? model)
    {
        if (model is { IsRead: false })
        {
            model.IsRead = true;
            NotificationReadChanged?.Invoke(model);
            UnreadCount--;
            UnreadCountChanged?.Invoke(UnreadCount);
        }
    }

    public void MarkAsUnread(NotificationModel? model)
    {
        if (model is { IsRead: true })
        {
            model.IsRead = false;
            NotificationReadChanged?.Invoke(model);
            UnreadCount++;
            UnreadCountChanged?.Invoke(UnreadCount);
        }
    }

    public void RemoveNotification(NotificationModel? model)
    {
        if (model is not null && _notifications.Contains(model))
        {
            model.OnRemoved();
            _notifications.Remove(model);
            NotificationRemoved?.Invoke(model);
            if (!model.IsRead)
            {
                UnreadCount--;
                UnreadCountChanged?.Invoke(UnreadCount);
            }
        }
    }

    public void PopNotification(NotificationModel model)
    {
        if (_notifications.Count >= MAX_NOTIFICATION_COUNT)
        {
            var first = _notifications.FirstOrDefault();
            if (first != null)
            {
                first.OnRemoved();
                _notifications.Remove(first);
                NotificationRemoved?.Invoke(first);
            }
        }

        _notifications.Add(model);
        UnreadCount++;
        NotificationAdded?.Invoke(model);
        UnreadCountChanged?.Invoke(UnreadCount);
    }

    /// <summary>
    ///     清理所有通知的资源（应用退出时调用）。
    /// </summary>
    public void ClearAll()
    {
        foreach (var model in _notifications)
        {
            model.OnRemoved();
        }
    }

    #endregion
}
