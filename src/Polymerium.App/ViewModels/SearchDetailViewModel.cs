using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Configurations;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core.Engines;
using Polymerium.Core.Helpers;
using Polymerium.Core.Resources;
using File = Polymerium.Abstractions.Resources.File;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    private readonly IMemoryCache _cache;
    private readonly ComponentManager _componentManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly ImportService _importer;
    private readonly LocalizationService _localizationService;
    private readonly MemoryStorage _memoryStorage;
    private readonly INotificationService _notification;
    private readonly ResolveEngine _resolver;
    private readonly AppSettings _settings;

    public SearchDetailViewModel(
        ViewModelContext context,
        MemoryStorage memoryStorage,
        ConfigurationManager configurationManager,
        ResolveEngine resolver,
        INotificationService notification,
        ImportService importer,
        IMemoryCache cache,
        ComponentManager componentManager,
        AppSettings settings,
        LocalizationService localizationService
    )
    {
        Instance = context.AssociatedInstance!;
        _memoryStorage = memoryStorage;
        _configurationManager = configurationManager;
        _resolver = resolver;
        _notification = notification;
        _importer = importer;
        _cache = cache;
        _componentManager = componentManager;
        _settings = settings;
        _localizationService = localizationService;
    }

    public GameInstanceModel Instance { get; }
    public RepositoryAssetMeta? Resource { get; private set; }
    public string? Name { get; private set; }
    public string? Author { get; private set; }
    public Uri? IconSource { get; private set; }
    public GameInstanceModel? Scope { get; private set; }

    public void GotResources(RepositoryAssetMeta resource, GameInstanceModel? scope)
    {
        Resource = resource;
        Name = resource.Name;
        Author = resource.Author;
        IconSource = resource.IconSource;
        Scope = scope;
    }

    public async Task LoadInfoAsync(
        Action<string, IEnumerable<SearchCenterResultItemScreenshotModel>> infoCallback,
        Action<SearchCenterResultItemVersionModel?> versionCallback
    )
    {
        var description = Resource!.Value.Description?.Value ?? string.Empty;
        var screenshots =
            Resource!.Value.Screenshots?.Value.Select(
                x => new SearchCenterResultItemScreenshotModel(x.Item1, x.Item2.AbsoluteUri)
            ) ?? Enumerable.Empty<SearchCenterResultItemScreenshotModel>();
        infoCallback(description, screenshots);
        // 支持的 modloader 和游戏版本应该是 file 的属性，但 modrinth 将其归于 version，这。。。
        var files = Resource!.Value.Repository switch
        {
            RepositoryLabel.Modrinth
                => (await ModrinthHelper.GetProjectVersionsAsync(Resource.Value.Id, _cache)).Select(
                    x =>
                        new SearchCenterResultItemVersionModel(
                            x.Id,
                            x.Name,
                            x.DatePublished,
                            x.Files
                                .Select(
                                    y =>
                                        new RepositoryAssetFile
                                        {
                                            FileName = y.Filename,
                                            Sha1 = y.Hashes.Sha1,
                                            Source = y.Url,
                                            SupportedCoreVersions = x.GameVersions,
                                            SupportedModLoaders = x.Loaders
                                                .Where(
                                                    y =>
                                                        ModrinthHelper.MODLOADERS_MAPPINGS.ContainsKey(
                                                            y
                                                        )
                                                )
                                                .Select(
                                                    y =>
                                                        _componentManager.ToFriendlyName(
                                                            ModrinthHelper.MODLOADERS_MAPPINGS[y]
                                                        ) ?? "unknown_loader"
                                                )
                                        }
                                )
                                .First(),
                            ModrinthHelper.MakeResourceUrl(
                                Resource.Value.Type,
                                Resource.Value.Id,
                                x.Id,
                                Resource.Value.Type
                            )
                        )
                ),
            RepositoryLabel.CurseForge
                => (
                    await CurseForgeHelper.GetModFilesAsync(uint.Parse(Resource.Value.Id), _cache)
                ).Select(
                    x =>
                        new SearchCenterResultItemVersionModel(
                            x.Id.ToString(),
                            x.DisplayName,
                            x.FileDate,
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
                                SupportedCoreVersions = x.GameVersions.Where(
                                    y => !CurseForgeHelper.MODLOADERS_MAPPINGS.ContainsKey(y)
                                ),
                                SupportedModLoaders = x.GameVersions
                                    .Where(y => CurseForgeHelper.MODLOADERS_MAPPINGS.ContainsKey(y))
                                    .Select(
                                        y =>
                                            _componentManager.ToFriendlyName(
                                                CurseForgeHelper.MODLOADERS_MAPPINGS[y]
                                            ) ?? "unknown_loader"
                                    )
                            },
                            CurseForgeHelper.MakeResourceUrl(
                                Resource.Value.Type,
                                Resource.Value.Id,
                                x.Id.ToString()
                            )
                        )
                ),
            _ => throw new NotImplementedException()
        };
        foreach (var file in files)
            versionCallback(file);
        versionCallback(null);
    }

    public IEnumerable<GameInstance> GetGameInstances()
    {
        return _memoryStorage.Instances;
    }

    public void InstallAsset(GameInstanceModel instance, SearchCenterResultItemVersionModel version)
    {
        instance.Attachments.Add(new Attachment(version.ResourceUrl));
        _notification.Enqueue(
            _localizationService.GetString("SearchDetailViewModel_InstallAsset_Success_Caption"),
            _localizationService
                .GetString("SearchDetailViewModel_InstallAsset_Success_Message")
                .Replace("{0}", Resource!.Value.Name)
                .Replace("{1}", instance.Name),
            InfoBarSeverity.Success
        );
    }

    public void InstallAsset(GameInstance instance, SearchCenterResultItemVersionModel version)
    {
        InstallAsset(
            new GameInstanceModel(instance, _configurationManager.Current.GameGlobals),
            version
        );
    }

    public async Task InstallModpackAsync(
        SearchCenterResultItemVersionModel version,
        Action<double?, bool> report,
        CancellationToken token = default
    )
    {
        var url = version.ResourceUrl;
        var resolveResult = await _resolver.ResolveToFileAsync(url, new ResolverContext());
        if (resolveResult.IsSuccessful && resolveResult.Value.Resource is File file)
        {
            report(0, false);
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(file.Source, token);
                var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                if (!Directory.Exists(Path.GetDirectoryName(tmpFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFile)!);
                ulong? totalSize = null;
                if (
                    response.Content.Headers.ContentLength.HasValue
                    && response.Content.Headers.ContentLength.Value > 0
                )
                    totalSize = (ulong)response.Content.Headers.ContentLength.Value;
                using var stream = await response.Content.ReadAsStreamAsync(token);
                var writer = new FileStream(tmpFile, FileMode.Create, FileAccess.Write);
                var buffer = new byte[4096];
                var read = 0;
                var totalRead = 0ul;
                ulong? lastReport = 0ul;
                do
                {
                    read = await stream.ReadAsync(buffer, token);
                    if (read != 0)
                    {
                        await writer.WriteAsync(buffer, 0, read);
                        totalRead += (ulong)read;
                        var progress = totalSize.HasValue ? totalRead * 100 / totalSize : null;
                        if (progress != lastReport)
                        {
                            report(progress, false);
                            lastReport = progress;
                        }
                    }
                } while (read != 0);

                report(null, false);
                writer.Close();
                var importResult = await _importer.ExtractMetadataFromFileAsync(
                    tmpFile,
                    url,
                    _settings.ForceImportOffline,
                    token
                );
                if (importResult.IsSuccessful)
                {
                    var postError = await _importer.SolidifyAsync(importResult.Value, null);
                    if (postError.HasValue)
                        EndedError(
                            _localizationService
                                .GetString("SearchDetailViewModel_SolidifyModpack_Failure_Message")
                                .Replace("{0}", postError.ToString())
                        );
                    else
                        EndedSuccess(
                            _localizationService
                                .GetString("SearchDetailViewModel_InstallModpack_Success_Message")
                                .Replace("{0}", importResult.Value.Content.Name)
                        );
                }
                else
                {
                    EndedError(
                        _localizationService
                            .GetString("SearchDetailViewModel_ImportModpack_Failure_Message")
                            .Replace("{0}", importResult.Error.ToString())
                    );
                }
            }
            catch (Exception e)
            {
                EndedError(
                    _localizationService
                        .GetString("SearchDetailViewModel_InstallModpack_Failure_Message")
                        .Replace("{0}", e.Message)
                );
            }
        }
        else
        {
            EndedError(
                _localizationService
                    .GetString("SearchDetailViewModel_ResolveModpack_Failure_Message")
                    .Replace("{0}", resolveResult.Error.ToString())
            );
        }

        report(null, true);
    }

    private void EndedError(string message)
    {
        _notification.Enqueue(
            _localizationService.GetString("SearchDetailViewModel_InstallModpack_Failure_Caption"),
            message,
            InfoBarSeverity.Error
        );
    }

    private void EndedSuccess(string message)
    {
        _notification.Enqueue(
            _localizationService.GetString("SearchDetailViewModel_InstallModpack_Success_Caption"),
            message,
            InfoBarSeverity.Success
        );
    }

    public string? GetModloaderFriendlyName(string identity)
    {
        return _componentManager.ToFriendlyName(identity);
    }
}
