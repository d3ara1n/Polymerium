using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core.Engines;
using Polymerium.Core.Helpers;
using Polymerium.Core.ResourceResolving;
using Polymerium.Core.Resources;
using File = Polymerium.Abstractions.Resources.File;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    private readonly IMemoryCache _cache;
    private readonly ComponentManager _componentManager;
    private readonly ImportService _importer;
    private readonly MemoryStorage _memoryStorage;
    private readonly INotificationService _notification;
    private readonly ResolveEngine _resolver;

    public SearchDetailViewModel(ViewModelContext context, MemoryStorage memoryStorage, ResolveEngine resolver,
        INotificationService notification, ImportService importer, IMemoryCache cache,
        ComponentManager componentManager)
    {
        Context = context;
        _memoryStorage = memoryStorage;
        _resolver = resolver;
        _notification = notification;
        _importer = importer;
        _cache = cache;
        _componentManager = componentManager;
        Versions = new ObservableCollection<SearchCenterResultItemVersionModel>();
    }

    public ViewModelContext Context { get; }
    public RepositoryAssetMeta? Resource { get; private set; }

    public SearchCenterResultItemVersionModel? SelectedVersion { get; set; }

    public ObservableCollection<SearchCenterResultItemVersionModel> Versions { get; }

    public GameInstance? Scope { get; private set; }

    public void GotResources(RepositoryAssetMeta resource, GameInstance? scope)
    {
        Resource = resource;
        Scope = scope;
    }

    public async Task LoadVersionsAsync(Action<SearchCenterResultItemVersionModel?> callback)
    {
        // 支持的 modloader 和游戏版本应该是 file 的属性，但 modrinth 将其归于 version，这。。。
        var files = Resource!.Value.Repository switch
        {
            RepositoryLabel.Modrinth => (await ModrinthHelper.GetProjectVersionsAsync(Resource.Value.Id, _cache))
                .Select(x =>
                    new SearchCenterResultItemVersionModel(x.Id, x.Name, x.DatePublished, x.Files.Select(y =>
                        new RepositoryAssetFile
                        {
                            FileName = y.Filename,
                            Sha1 = y.Hashes.Sha1,
                            Source = y.Url,
                            SupportedCoreVersions = x.GameVersions,
                            SupportedModLoaders = x.Loaders
                                .Where(y => ModrinthHelper.MODLOADERS_MAPPINGS.ContainsKey(y))
                                .Select(y =>
                                    _componentManager.ToFriendlyName(ModrinthHelper.MODLOADERS_MAPPINGS[y]) ??
                                    "unknown_loader")
                        }).First(), ModrinthResolver.MakeResourceUrl(Resource.Value.Type, Resource.Value.Id, x.Id))),
            RepositoryLabel.CurseForge => (await CurseForgeHelper.GetModFilesAsync(uint.Parse(Resource.Value.Id),
                    _cache))
                .Select(x => new SearchCenterResultItemVersionModel(x.Id.ToString(), x.DisplayName, x.FileDate,
                    new RepositoryAssetFile
                    {
                        FileName = Resource.Value.Type switch
                        {
                            ResourceType.ResourcePack => $"resourcepacks/{x.FileName}",
                            ResourceType.Mod => $"mods/{x.FileName}",
                            ResourceType.ShaderPack => $"shaderpacks/{x.FileName}",
                            ResourceType.Modpack => x.FileName,
                            _ => throw new NotImplementedException()
                        },
                        Sha1 = x.ExtractSha1(),
                        Source = x.ExtractDownloadUrl(),
                        SupportedCoreVersions =
                            x.GameVersions.Where(y => !CurseForgeHelper.MODLOADERS_MAPPINGS.ContainsKey(y)),
                        SupportedModLoaders = x.GameVersions
                            .Where(y => CurseForgeHelper.MODLOADERS_MAPPINGS.ContainsKey(y))
                            .Select(y =>
                                _componentManager.ToFriendlyName(CurseForgeHelper.MODLOADERS_MAPPINGS[y]) ??
                                "unknown_loader")
                    }, CurseForgeResolver.MakeResourceUrl(Resource.Value.Type, Resource.Value.Id, x.Id.ToString()))),
            _ => throw new NotImplementedException()
        };
        foreach (var file in files) callback(file);
        callback(null);
    }

    public IEnumerable<GameInstance> GetGameInstances()
    {
        return _memoryStorage.Instances;
    }

    public void InstallAsset(GameInstance instance, SearchCenterResultItemVersionModel version)
    {
        instance.Metadata.Attachments.Add(version.ResourceUrl);
        _notification.Enqueue("添加资产成功", $"对 {Resource!.Value.Name} 的引用被添加到 {instance.Name}", InfoBarSeverity.Success);
    }

    public async Task InstallModpackAsync(SearchCenterResultItemVersionModel version, Action<double?, bool> report,
        CancellationToken token = default)
    {
        var url = version.ResourceUrl;
        var resolveResult = await _resolver.ResolveToFileAsync(url, new ResolverContext());
        if (resolveResult.IsSuccessful && resolveResult.Value.Resource is File file)
        {
            report(0, false);
            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (!Directory.Exists(Path.GetDirectoryName(tmpFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(tmpFile)!);
            using var client = new HttpClient();
            var response = await client.GetAsync(file.Source, token);
            ulong? totalSize = null;
            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > 0)
                totalSize = (ulong)response.Content.Headers.ContentLength.Value;
            using var stream = await response.Content.ReadAsStreamAsync(token);
            var writer = new FileStream(tmpFile, FileMode.Create, FileAccess.Write);
            var buffer = new byte[4096];
            var read = 0;
            var totalRead = 0ul;
            var reportCount = 0u;
            do
            {
                read = await stream.ReadAsync(buffer, token);
                if (read != 0)
                {
                    await writer.WriteAsync(buffer, 0, read);
                    totalRead += (ulong)read;
                    if (reportCount++ % 100 == 0)
                        report(totalSize.HasValue ? (double)totalRead / totalSize : null, false);
                }
            } while (read != 0);

            report(null, false);
            writer.Close();
            var importResult = await _importer.ImportAsync(tmpFile, token);
            if (importResult.IsSuccessful)
            {
                importResult.Value.Instance.ReferenceSource = version.ResourceUrl;
                importResult.Value.Instance.ThumbnailFile = Resource!.Value.IconSource?.AbsoluteUri;
                importResult.Value.Instance.Author = Resource!.Value.Author;
                var postError = await _importer.PostImportAsync(importResult.Value);
                if (postError.HasValue)
                    EndedError($"添加导入的实例失败: {postError}");
                else
                    EndedSuccess($"{importResult.Value.Instance.Name} 已添加");
            }
            else
            {
                EndedError($"导入下载的整合包文件失败: {importResult.Error}");
            }

            report(null, true);
        }
        else
        {
            report(null, true);
            EndedError($"解析整合包所在资源链接失败: {resolveResult.Error}");
        }
    }

    private void EndedError(string message)
    {
        _notification.Enqueue("安装整合包失败", message, InfoBarSeverity.Error);
    }

    private void EndedSuccess(string message)
    {
        _notification.Enqueue("安装整合包完成", message, InfoBarSeverity.Success);
    }
}