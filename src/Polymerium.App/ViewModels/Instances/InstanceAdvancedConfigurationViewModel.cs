﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core;
using System;
using System.IO;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceAdvancedConfigurationViewModel : ObservableObject
{
    private readonly ConfigurationManager _configurationManager;
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;
    private readonly ILogger _logger;
    private readonly INotificationService _notification;

    public InstanceAdvancedConfigurationViewModel(
        ViewModelContext context,
        InstanceManager instanceManager,
        IFileBaseService fileBase,
        ConfigurationManager configurationManager,
        INotificationService notification,
        LocalizationService localizationService,
        ILogger<InstanceAdvancedConfigurationViewModel> logger
    )
    {
        Context = context;
        Localization = localizationService;
        _instanceManager = instanceManager;
        _fileBase = fileBase;
        _logger = logger;
        _configurationManager = configurationManager;
        _notification = notification;
    }

    public ViewModelContext Context { get; }

    public LocalizationService Localization { get; }

    // NOTE: 重置对于已解锁的实例只会删除目录，但对于具有 ReferenceSource 的实例，会重新导入元数据

    public InstanceManagerError? RenameInstance(string name)
    {
        var result = _instanceManager.RenameInstanceSafe(Context.AssociatedInstance!.Inner, name);

        Context.AssociatedInstance = new GameInstanceModel(
            Context.AssociatedInstance!.Inner,
            _configurationManager.Current.GameGlobals
        );

        return result;
    }

    public bool DeleteInstance()
    {
        var folderDir = new Uri(
            ConstPath.INSTANCE_BASE.Replace("{0}", Context.AssociatedInstance!.Id)
        );
        var folderPath = _fileBase.Locate(folderDir);
        var localDir = new Uri(
            ConstPath.LOCAL_INSTANCE_BASE.Replace("{0}", Context.AssociatedInstance!.Id)
        );
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
        var folderDir = new Uri(
            ConstPath.INSTANCE_BASE.Replace("{0}", Context.AssociatedInstance!.Id)
        );
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

    public void PopNotification(
        string caption,
        string message,
        InfoBarSeverity severity = InfoBarSeverity.Success
    )
    {
        _notification.Enqueue(caption, message, severity);
    }
}
