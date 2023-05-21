using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Components.Installers;
using Polymerium.Core.Extensions;
using Polymerium.Core.Managers;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class BuildPolylockStage : StageBase
{
    private readonly AssetManager _assetManager;
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly IDictionary<string, Type> _installers;
    private readonly GameInstance _instance;
    private readonly IEnumerable<ComponentMeta> _metas;
    private readonly Uri _polylockDataFile;
    private readonly Uri _polylockHashFile;
    private readonly IServiceProvider _provider;
    private readonly ResolveEngine _resolver;
    private readonly SHA1 _sha1;

    public BuildPolylockStage(
        GameInstance instance,
        SHA1 sha1,
        IEnumerable<ComponentMeta> metas,
        Uri polylockDataFile,
        Uri polylockHashFile,
        DownloadEngine downloader,
        ResolveEngine resolver,
        IServiceProvider provider,
        AssetManager assetManager
    )
    {
        _instance = instance;
        _sha1 = sha1;
        _metas = metas;
        _polylockDataFile = polylockDataFile;
        _polylockHashFile = polylockHashFile;
        _resolver = resolver;
        _downloader = downloader;
        _provider = provider;
        _assetManager = assetManager;
        _fileBase = _provider.GetRequiredService<IFileBaseService>();
        _installers = new Dictionary<string, Type>
        {
            { ComponentMeta.MINECRAFT, typeof(MinecraftComponentInstaller) },
            { ComponentMeta.FORGE, typeof(ForgeComponentInstaller) },
            { ComponentMeta.FABRIC, typeof(FabricComponentInstaller) },
            { ComponentMeta.QUILT, typeof(QuiltComponentInstaller) }
        };
    }

    public override string StageNameKey => "解析元数据中的组件信息";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested)
            return Cancel();
        try
        {
            var queue = BuildQueue(_instance.Metadata.Components);
            var installerTypes = queue.Select(
                x => (x.Item1, _installers.First(y => y.Key == x.Item2.Identity).Value)
            );
            var context = new ComponentInstallerContext(_instance);
            foreach (var (component, type) in installerTypes)
            {
                Report($"解析 {component.Identity}({component.Version}) 元数据");
                var installer = (ComponentInstallerBase)
                    ActivatorUtilities.CreateInstance(_provider, type);
                installer.Context = context;
                installer.Token = Token;
                var result = await installer.StartAsync(component);
                if (result.HasValue)
                    return Error(result.Value.ToString()!);
            }

            var polylock = context.Build();
            var tasks = new List<Task<Result<ResolveResult, ResolveResultError>>>();
            Report("解析元数据中的附件资源信息");
            var resolverContext = new ResolverContext(_instance);
            foreach (var attachment in _instance.Metadata.Attachments)
                tasks.Add(_resolver.ResolveToFileAsync(attachment.Source, resolverContext));
            await Task.WhenAll(tasks);
            var errors = tasks.Count(
                x => !x.Result.IsSuccessful || x.Result.Value.Type != ResourceType.File
            );
            if (errors > 0)
                return Error($"{errors}/{tasks.Count} 条附件资源解析错误");

            var product = new List<PolylockAttachment>(polylock.Attachments);
            product.AddRange(
                tasks.Select(x =>
                {
                    var file = (File)x.Result.Value.Resource;
                    return new PolylockAttachment
                    {
                        CachedObjectPath = $"{file.Source.Host}/{file.Id}/{file.VersionId}",
                        Source = file.Source,
                        Sha1 = file.Hash,
                        Target = new Uri(
                            new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", _instance.Id)),
                            file.FileName
                        )
                    };
                })
            );
            polylock.Attachments = product;

            var polylockData = JsonConvert.SerializeObject(polylock);
            _fileBase.WriteAllText(_polylockDataFile, polylockData);
            var md5 = _instance.ComputeMetadataHash();
            _fileBase.WriteAllText(_polylockHashFile, md5);

            return Next(
                new LoadAssetIndexStage(
                    _instance,
                    _sha1,
                    polylock,
                    _fileBase,
                    _downloader,
                    _assetManager
                )
            );
        }
        catch (Exception e)
        {
            return Error(e.Message, e);
        }
    }

    private IEnumerable<(Component, ComponentMeta)> BuildQueue(IEnumerable<Component> components)
    {
        return components
            .Select(x => (x, _metas.First(y => y.Identity == x.Identity)))
            .Select(x => (x, MeasureDependencyDepth(x.Item2)))
            .OrderBy(x => x.Item2)
            .Select(y => y.x);
    }

    private uint MeasureDependencyDepth(ComponentMeta meta)
    {
        return meta.Dependencies.Any()
            ? meta.Dependencies.Max(
                x => MeasureDependencyDepth(_metas.First(y => y.Identity == x)) + 1
            )
            : 0;
    }
}