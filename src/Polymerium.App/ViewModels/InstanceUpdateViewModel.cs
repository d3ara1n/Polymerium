using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.App.Services;
using Polymerium.Core;
using Polymerium.Core.Engines;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RFile = Polymerium.Abstractions.Resources.File;

namespace Polymerium.App.ViewModels;

public class InstanceUpdateViewModel : ObservableObject
{
    private readonly ImportService _importer;
    private readonly ResolveEngine _resolver;
    private readonly IFileBaseService _fileBase;
    private readonly LocalizationService _localizationService;
    private readonly INotificationService _notification;
    public readonly NavigationService NavigationService;

    public InstanceUpdateViewModel(
        ImportService importer,
        ResolveEngine resolver,
        IFileBaseService fileBase,
        LocalizationService localizationService,
        INotificationService notification,
        NavigationService navigationService
    )
    {
        _importer = importer;
        _resolver = resolver;
        _fileBase = fileBase;
        _localizationService = localizationService;
        _notification = notification;
        NavigationService = navigationService;
    }

    public async Task ApplyUpdateAsync(
        GameInstance instance,
        Uri reference,
        bool resetLocal,
        Action<bool> callback
    )
    {
        var context = new ResolverContext(instance);
        var succ = false;
        var fileResult = await _resolver.ResolveToFileAsync(reference, context);
        if (fileResult.IsSuccessful && fileResult.Value.Resource is RFile file)
        {
            try
            {
                var client = new HttpClient();
                using var stream = await client.GetStreamAsync(file.Source);
                var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                if (!Directory.Exists(Path.GetDirectoryName(tmpFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFile)!);
                var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fs);
                await fs.FlushAsync();
                fs.Close();
                var importResult = await _importer.ExtractMetadataFromFileAsync(
                    tmpFile,
                    reference,
                    false
                );
                if (importResult.IsSuccessful)
                {
                    var oldAttachments = instance.Metadata.Attachments
                        .Where(x => x.From == instance.ReferenceSource)
                        .ToList();
                    if (resetLocal)
                    {
                        var resolveResult = await _resolver.ResolveAsync(
                            oldAttachments.Select(x => x.Source),
                            context
                        );
                        if (resolveResult.IsSuccessful)
                        {
                            foreach (var localResult in resolveResult.Value)
                            {
                                if (localResult.Resource is RFile localFile)
                                {
                                    var localPath = _fileBase.Locate(
                                        new Uri(
                                            new Uri(
                                                ConstPath.INSTANCE_BASE.Replace("{0}", instance.Id)
                                            ),
                                            localFile.FileName
                                        )
                                    );
                                    if (File.Exists(localPath))
                                    {
                                        File.Delete(localPath);
                                    }
                                }
                            }
                        }
                        else
                        {
                            EndedError($"应用本地文件错误: {importResult.Error}");
                        }
                    }
                    var postError = await _importer.SolidifyAsync(importResult.Value, instance);
                    if (postError.HasValue)
                    {
                        EndedError($"导入文件错误: {postError.Value}");
                    }
                    else
                    {
                        _notification.Enqueue(
                            "更新完成",
                            $"版本更新到 {file.Name}",
                            InfoBarSeverity.Success
                        );
                        succ = true;
                    }
                }
                else
                {
                    EndedError($"提取信息错误: {importResult.Error}");
                }
            }
            catch (Exception e)
            {
                EndedError($"文件系统错误: {e.Message}");
            }
        }
        else
        {
            EndedError($"解析信息错误: {fileResult.Error}");
        }
        callback(succ);
    }

    private void EndedError(string message)
    {
        _notification.Enqueue("更新失败", message, InfoBarSeverity.Error);
    }
}
