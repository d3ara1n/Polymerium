using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using System;

namespace Polymerium.App.Services;

public class NotificationService
{
    private Action<NotificationItem>? handler;

    public void SetHandler(Action<NotificationItem> action) => handler = action;

    public void Pop(InfoBarSeverity severity, string text) => handler?.Invoke(new NotificationItem(severity, text));

    public void PopInformation(string text) => Pop(InfoBarSeverity.Informational, text);

    public void PopError(string text) => Pop(InfoBarSeverity.Error, text);

    public void PopWarning(string text) => Pop(InfoBarSeverity.Warning, text);

    public void PopSuccess(string text) => Pop(InfoBarSeverity.Success, text);
}