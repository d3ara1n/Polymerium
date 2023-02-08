using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Components;
using Polymerium.Core.Components.Installers;
using Polymerium.Core.Extensions;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class BuildStructureStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly IDictionary<string, Type> _installers;
    private readonly GameInstance _instance;
    private readonly IEnumerable<ComponentMeta> _metas;
    private readonly Uri _polylockDataFile;
    private readonly Uri _polylockHashFile;
    private readonly IServiceProvider _provider;
    private readonly SHA1 _sha1;

    public BuildStructureStage(GameInstance instance, SHA1 sha1, IEnumerable<ComponentMeta> metas, Uri polylockDataFile,
        Uri polylockHashFile,
        DownloadEngine downloader,
        IServiceProvider provider)
    {
        _instance = instance;
        _sha1 = sha1;
        _metas = metas;
        _polylockDataFile = polylockDataFile;
        _polylockHashFile = polylockHashFile;
        _downloader = downloader;
        _provider = provider;
        _fileBase = _provider.GetRequiredService<IFileBaseService>();
        _installers = new Dictionary<string, Type>
        {
            { "net.minecraft", typeof(MinecraftComponentInstaller) },
            { "net.minecraftforge", typeof(ForgeComponentInstaller) },
            { "net.fabricmc.fabric-loader", typeof(FabricComponentInstaller) },
            { "org.quiltmc.quilt-loader", typeof(QuiltComponentInstaller) }
        };
    }

    public override string StageName => "解析元数据中的组件信息";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested) return Cancel();
        try
        {
            var queue = BuildQueue(_instance.Metadata.Components);
            var installerTypes = queue.Select(x => (x.Item1, _installers.First(y => y.Key == x.Item2.Identity).Value));
            var context = new ComponentInstallerContext(_instance);
            foreach (var (component, type) in installerTypes)
            {
                Report($"解析 {component.Identity}({component.Version}) 元数据");
                var installer = (ComponentInstallerBase)ActivatorUtilities.CreateInstance(_provider, type);
                installer.Context = context;
                installer.Token = Token;
                var result = await installer.StartAsync(component);
                if (result.IsErr(out var message)) return Error(message!);
            }

            var polylock = context.Build();
            var polylockData = JsonConvert.SerializeObject(polylock);
            _fileBase.WriteAllText(_polylockDataFile, polylockData);
            var md5 = _instance.ComputeMetadataHash();
            _fileBase.WriteAllText(_polylockHashFile, md5);

            return Next(new LoadAssetIndexStage(_instance, _sha1, polylock, _fileBase, _downloader));
        }
        catch (Exception e)
        {
            return Error(e.Message, e);
        }
    }

    private IEnumerable<(Component, ComponentMeta)> BuildQueue(IEnumerable<Component> components)
    {
        return components.Select(x => (x, _metas.First(y => y.Identity == x.Identity)))
            .Select(x => (x, MeasureDependencyDepth(x.Item2))).OrderBy(x => x.Item2).Select(y => y.x);
    }

    private uint MeasureDependencyDepth(ComponentMeta meta)
    {
        return meta.Dependencies != null && meta.Dependencies.Any()
            ? meta.Dependencies.Max(x => MeasureDependencyDepth(_metas.First(y => y.Identity == x)) + 1)
            : 0;
    }
}