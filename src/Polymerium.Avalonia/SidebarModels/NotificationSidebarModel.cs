using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.SidebarModels;

// NOTE: 数据归属仍在 NotificationService（app 级单例），本类仅持有同一批 NotificationModel 引用的镜像，
//  负责投影集合与未读计数，并转发命令。
public partial class NotificationSidebarModel : ViewModelBase
{
    private readonly NotificationService _service;

    public NotificationSidebarModel(NotificationService service) => _service = service;

    public ObservableCollection<NotificationModel> Notifications { get; } = [];

    [ObservableProperty]
    public partial int UnreadNotificationCount { get; set; }

    [RelayCommand]
    private void MarkAllAsRead() => _service.MarkAllAsRead();

    [RelayCommand]
    private void MarkAsRead(NotificationModel? model) => _service.MarkAsRead(model);

    [RelayCommand]
    private void MarkAsUnread(NotificationModel? model) => _service.MarkAsUnread(model);

    [RelayCommand]
    private void RemoveNotification(NotificationModel? model) => _service.RemoveNotification(model);

    private void OnAdded(NotificationModel model) => Notifications.Add(model);

    private void OnRemoved(NotificationModel model) => Notifications.Remove(model);

    private void OnUnreadCountChanged(int count) => UnreadNotificationCount = count;

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        // NOTE: 初始快照——Sidebar 可能在已有多条通知后才被打开。
        foreach (var n in _service.GetSnapshot())
        {
            Notifications.Add(n);
        }

        UnreadNotificationCount = _service.UnreadCount;

        _service.NotificationAdded += OnAdded;
        _service.NotificationRemoved += OnRemoved;
        _service.UnreadCountChanged += OnUnreadCountChanged;
        // NOTE: NotificationReadChanged 无需处理——逐项 IsRead 在 NotificationModel 上可观察，镜像共享引用。
        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _service.NotificationAdded -= OnAdded;
        _service.NotificationRemoved -= OnRemoved;
        _service.UnreadCountChanged -= OnUnreadCountChanged;
        return Task.CompletedTask;
    }

    #endregion
}
