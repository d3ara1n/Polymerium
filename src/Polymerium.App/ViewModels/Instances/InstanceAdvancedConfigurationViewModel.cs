using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceAdvancedConfigurationViewModel : ObservableObject
{
    private readonly ConfigurationManager _configurationManager;
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;
    private readonly ILogger _logger;
    private readonly NavigationService _navigation;
    private readonly INotificationService _notification;

    public InstanceAdvancedConfigurationViewModel(
        ViewModelContext context,
        InstanceManager instanceManager,
        IFileBaseService fileBase,
        NavigationService navigation,
        ConfigurationManager configurationManager,
        INotificationService notification,
        ILogger<InstanceAdvancedConfigurationViewModel> logger
    )
    {
        Context = context;
        _instanceManager = instanceManager;
        _fileBase = fileBase;
        _navigation = navigation;
        _logger = logger;
        _configurationManager = configurationManager;
        _notification = notification;
    }

    public ViewModelContext Context { get; }

    // NOTE: 重置对于已解锁的实例只会删除目录，但对于具有 ReferenceSource 的实例，会重新导入元数据

    public InstanceManagerError? RenameInstance(string name)
    {
        var result = _instanceManager.RenameInstanceSafe(Context.AssociatedInstance!.Inner, name);


        Context.AssociatedInstance =
            new GameInstanceModel(Context.AssociatedInstance!.Inner, _configurationManager.Current.GameGlobals);

        return result;
    }

    public bool DeleteInstance()
    {
        var folderDir = new Uri($"poly-file://{Context.AssociatedInstance!.Id}/");
        var folderPath = _fileBase.Locate(folderDir);
        var localDir = new Uri($"poly-file:///local/instances/{Context.AssociatedInstance!.Id}/");
        var localPath = _fileBase.Locate(localDir);
        try
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
            if (Directory.Exists(localPath))
                Directory.Delete(localPath, true);
            _instanceManager.RemoveInstance(Context.AssociatedInstance.Inner);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Deleting instance {} local files failed", folderDir.AbsoluteUri);
            return false;
        }
    }

    public bool ResetInstance()
    {
        var folderDir = new Uri($"poly-file://{Context.AssociatedInstance!.Id}/");
        var folderPath = _fileBase.Locate(folderDir);
        try
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Deleting instance {} local files failed", folderDir.AbsoluteUri);
            return false;
        }
    }

    public void PopNotification(string caption, string message, InfoBarSeverity severity = InfoBarSeverity.Success)
    {
        _notification.Enqueue(caption, message, severity);
    }
}