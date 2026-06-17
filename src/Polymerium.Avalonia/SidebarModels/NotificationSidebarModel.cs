using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.SidebarModels;

// NotificationSidebar 的 Model：订阅 NotificationService 的事件，维护可绑定投影集合与未读计数，暴露转发命令。
// 经 OverlayService.PopSidebar<NotificationSidebar>() 由 activator 激活：
//   - activator 按命名约定（Sidebars.NotificationSidebar → SidebarModels.NotificationSidebarModel）配对本类型，
//     通过 DI 创建实例（注入 NotificationService）、设为 View 的 DataContext，并挂载 ViewModelMixin。
//   - Sidebar 加入可视树（Loaded）时 Mixin 调用 OnInitializeAsync（订阅事件 + 填充初始快照）；
//     关闭（Unloaded）时 Mixin 调用 OnDeinitializeAsync（退订）——生命周期完全托管，无需手动触发。
// 数据归属仍在 NotificationService（app 级单例），本类仅持有同一批 NotificationModel 引用的镜像。
public partial class NotificationSidebarModel : ViewModelBase
{
    private readonly NotificationService _service;

    public ObservableCollection<NotificationModel> Notifications { get; } = [];

    [ObservableProperty]
    public partial int UnreadNotificationCount { get; set; }

    public NotificationSidebarModel(NotificationService service)
    {
        _service = service;
    }

    [RelayCommand]
    private void MarkAllAsRead() => _service.MarkAllAsRead();

    [RelayCommand]
    private void MarkAsRead(NotificationModel? model) => _service.MarkAsRead(model);

    [RelayCommand]
    private void MarkAsUnread(NotificationModel? model) => _service.MarkAsUnread(model);

    [RelayCommand]
    private void RemoveNotification(NotificationModel? model) => _service.RemoveNotification(model);

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        // 初始快照：Sidebar 可能在已有多条通知后才被打开
        foreach (var n in _service.GetSnapshot())
        {
            Notifications.Add(n);
        }

        UnreadNotificationCount = _service.UnreadCount;

        _service.NotificationAdded += OnAdded;
        _service.NotificationRemoved += OnRemoved;
        _service.UnreadCountChanged += OnUnreadCountChanged;
        // NotificationReadChanged 无需处理：逐项 IsRead 在 NotificationModel 上已可观察，镜像共享同一引用。
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

    private void OnAdded(NotificationModel model) => Notifications.Add(model);

    private void OnRemoved(NotificationModel model) => Notifications.Remove(model);

    private void OnUnreadCountChanged(int count) => UnreadNotificationCount = count;
}
