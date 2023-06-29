using DotNext;
using FluentPipeline;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Components.Installers;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Extensions;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Models.Mojang;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wupoo;
using RFile = Polymerium.Abstractions.Resources.File;

namespace Polymerium.Core.Engines;

public class RestoreEngine
{
    private readonly IFileBaseService _fileBase;
    private readonly ResolveEngine _resolver;
    private readonly IServiceProvider _provider;
    private readonly SHA1 _sha1 = SHA1.Create();

    public RestoreEngine(
        ResolveEngine resolver,
        IFileBaseService fileBase,
        IServiceProvider provider
    )
    {
        _resolver = resolver;
        _fileBase = fileBase;
        _provider = provider;
    }

    public Pipeline<RestoreError, GameInstance> ProducePipeline()
    {
        return PipelineBuilder<RestoreError, GameInstance>
            .Create(BuildPolylockData)
            .Then(CompleteAssets)
            .Then(CompleteLibraries)
            .Then(CompleteAttachments)
            .Setup()
            .Build();
    }

    private Result<RestoreContext, RestoreError> BuildPolylockData(GameInstance instance)
    {
        var polylockHashUrl = instance.GetPolylockHashUrl();
        var polylockDataUrl = instance.GetPolylockDataUrl();

        if (instance.CheckIfRestored(_fileBase, out var content))
        {
            var polylock = JsonConvert.DeserializeObject<PolylockData>(content!);
            return new Result<RestoreContext, RestoreError>(new RestoreContext(polylock));
        }

        return BuildPolylockDataInternal(instance, polylockHashUrl, polylockDataUrl);
    }

    private Result<RestoreContext, RestoreError> BuildPolylockDataInternal(
        GameInstance instance,
        Uri polylockHashUrl,
        Uri polylockDataUrl
    )
    {
        var installers = new Dictionary<string, Type>
        {
            { ComponentMeta.MINECRAFT, typeof(MinecraftComponentInstaller) },
            { ComponentMeta.FORGE, typeof(ForgeComponentInstaller) },
            { ComponentMeta.FABRIC, typeof(FabricComponentInstaller) },
            { ComponentMeta.QUILT, typeof(QuiltComponentInstaller) }
        };
        var components = instance.Metadata.Components;
        var context = new ComponentInstallerContext(instance);
        foreach (var component in components)
        {
            if (installers.TryGetValue(component.Identity, out var type))
            {
                var installer = (ComponentInstallerBase)
                    ActivatorUtilities.CreateInstance(
                        _provider.CreateScope().ServiceProvider,
                        type
                    );
                installer.Context = context;
                installer.Token = CancellationToken.None;
                var result = installer.StartAsync(component).Result;
                if (result.HasValue)
                {
                    return new Result<RestoreContext, RestoreError>(
                        RestoreError.ComponentInstallationFailure
                    );
                }
            }
            else
            {
                return new Result<RestoreContext, RestoreError>(RestoreError.ComponentNotFound);
            }
        }
        var polylock = context.Build();
        var tasks = new List<Task<Result<ResolveResult, ResolveResultError>>>();
        var resolverContext = new ResolverContext(instance);
        foreach (var attachment in instance.Metadata.Attachments.Where(x => x.Enabled))
            tasks.Add(_resolver.ResolveToFileAsync(attachment.Source, resolverContext));
        Task.WhenAll(tasks).Wait();
        var errors = tasks.Count(
            x => !x.Result.IsSuccessful || x.Result.Value.Type != ResourceType.File
        );
        if (errors > 0)
            return new Result<RestoreContext, RestoreError>(RestoreError.ResourceNotReacheable);
        var product = new List<PolylockAttachment>(polylock.Attachments);
        product.AddRange(
            tasks.Select(x =>
            {
                var file = (RFile)x.Result.Value.Resource;
                return new PolylockAttachment
                {
                    CachedObjectPath = $"{file.Source.Host}/{file.Id}/{file.VersionId}",
                    Source = file.Source,
                    Sha1 = file.Hash,
                    Target = new Uri(
                        new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", instance.Id)),
                        file.FileName
                    )
                };
            })
        );
        polylock.Attachments = product;

        var polylockData = JsonConvert.SerializeObject(polylock);
        _fileBase.WriteAllText(polylockDataUrl, polylockData);
        var md5 = instance.ComputeMetadataHash();
        _fileBase.WriteAllText(polylockHashUrl, md5);
        return new Result<RestoreContext, RestoreError>(new RestoreContext(polylock));
    }

    private Result<RestoreContext, RestoreError> CompleteAssets(
        GameInstance instance,
        RestoreContext context
    )
    {
        var assetIndexFile = new Uri(
            ConstPath.CACHE_ASSETS_INDEX_FILE.Replace("{0}", context.Polylock.AssetIndex.Id)
        );
        string? content = null;
        if (
            !_fileBase
                .VerifyHashAsync(assetIndexFile, context.Polylock.AssetIndex.Sha1, _sha1)
                .Result || !_fileBase.TryReadAllText(assetIndexFile, out content)
        )
        {
            Exception? exception = null;
            Wapoo
                .Wohoo(context.Polylock.AssetIndex.Url)
                .WhenException<Exception>(e => exception = e)
                .ForAnyResult(
                    async (_, stream) =>
                    {
                        using var reader = new StreamReader(stream);
                        content = await reader.ReadToEndAsync();
                    }
                )
                .Fetch();
            if (content == null)
                return new Result<RestoreContext, RestoreError>(RestoreError.ResourceNotReacheable);
            _fileBase.WriteAllText(assetIndexFile, content);
        }
        var assetIndex = JsonConvert.DeserializeObject<AssetsIndex>(content);
        foreach (var item in assetIndex.Objects.Select(x => x.Hash))
        {
            var path = new Uri(
                ConstPath.CACHE_ASSETS_OBJECTS_FILE.Replace("{0}", item[..2]).Replace("{1}", item)
            );
            if (!_fileBase.VerifyHashAsync(path, item, _sha1).Result)
            {
                context.Tasks.Add(
                    new RestoreDownload()
                    {
                        Source = new Uri(
                            $"https://resources.download.minecraft.net/{item[..2]}/{item}"
                        ),
                        Target = path
                    }
                );
            }
        }
        return context;
    }

    private Result<RestoreContext, RestoreError> CompleteLibraries(
        GameInstance instance,
        RestoreContext context
    )
    {
        var libraryDir = new Uri(ConstPath.CACHE_LIBRARIES_DIR);
        var nativesDir = new Uri(ConstPath.INSTANCE_NATIVES_DIR.Replace("{0}", instance.Id));
        _fileBase.RemoveDirectory(nativesDir);
        foreach (var item in context.Polylock.Libraries)
        {
            var libPath = new Uri(libraryDir, item.Path);
            if (item.Url != null)
            {
                if (!_fileBase.VerifyHashAsync(libPath, item.Sha1, _sha1).Result)
                {
                    var download = new RestoreDownload() { Source = item.Url, Target = libPath };
                    if (item.IsNative)
                    {
                        download.PostAction = _ =>
                            ZipFile.ExtractToDirectory(
                                _fileBase.Locate(libPath),
                                _fileBase.Locate(nativesDir),
                                true
                            );
                    }
                    context.Tasks.Add(download);
                }
                else
                {
                    if (item.IsNative)
                        ZipFile.ExtractToDirectory(
                            _fileBase.Locate(libPath),
                            _fileBase.Locate(nativesDir),
                            true
                        );
                }
            }
        }
        return context;
    }

    private Result<RestoreContext, RestoreError> CompleteAttachments(
        GameInstance instance,
        RestoreContext context
    )
    {
        var merge = new List<(PolylockAttachment, Uri)>();
        foreach (var attachment in context.Polylock.Attachments)
        {
            if (attachment.Source.Scheme == "poly-file")
            {
                if (!_fileBase.VerifyHashAsync(attachment.Target, attachment.Sha1, _sha1).Result)
                {
                    var path = _fileBase.Locate(attachment.Source);
                    var target = _fileBase.Locate(attachment.Target);
                    context.Tasks.Add(
                        new RestoreDownload()
                        {
                            Source = attachment.Source,
                            Target = attachment.Target
                        }
                    );
                }
            }
            else
            {
                var pooled = new Uri(
                    ConstPath.CACHE_OBJECTS_FILE.Replace("{0}", attachment.CachedObjectPath)
                );
                if (!_fileBase.VerifyHashAsync(pooled, attachment.Sha1, _sha1).Result)
                    context.Tasks.Add(
                        new RestoreDownload() { Source = attachment.Source, Target = pooled }
                    );

                merge.Add((attachment, pooled));
                context.MergedStates.Add(
                    new RenewableAssetState() { Source = pooled, Target = attachment.Target }
                );
            }
        }
        return context;
    }
}
