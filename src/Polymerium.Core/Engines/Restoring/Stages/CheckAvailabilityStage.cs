using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Components;
using Polymerium.Core.Extensions;
using Polymerium.Core.Managers;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CheckAvailabilityStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;

    private readonly GameInstance _instance;
    private readonly IEnumerable<ComponentMeta> _metas;
    private readonly IServiceProvider _provider;
    private readonly ResolveEngine _resolver;
    private readonly SHA1 _sha1;
    private readonly AssetManager _assetManager;

    public CheckAvailabilityStage(
        GameInstance instance,
        SHA1 sha1,
        IEnumerable<ComponentMeta> metas,
        DownloadEngine downloader,
        ResolveEngine resolver,
        IFileBaseService fileBase,
        IServiceProvider provider,
        AssetManager assetManager
    )
    {
        _instance = instance;
        _sha1 = sha1;
        _metas = metas;
        _resolver = resolver;
        _downloader = downloader;
        _fileBase = fileBase;
        _provider = provider;
        _assetManager = assetManager;
    }

    public override string StageName => "检查资源可用性";

    public override Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested)
            return Task.FromResult(Cancel());
        var polylockDataFile = _instance.GetPolylockDataUrl();
        var polylockHashFile = _instance.GetPolylockHashUrl();
        if (_instance.CheckIfRestored(_fileBase, out var content))
        {
            var polylock = JsonConvert.DeserializeObject<PolylockData>(content!);
            return Task.FromResult(
                Next(
                    new LoadAssetIndexStage(
                        _instance,
                        _sha1,
                        polylock,
                        _fileBase,
                        _downloader,
                        _assetManager
                    )
                )
            );
        }

        return Task.FromResult(
            Next(
                new BuildPolylockStage(
                    _instance,
                    _sha1,
                    _metas,
                    polylockDataFile,
                    polylockHashFile,
                    _downloader,
                    _resolver,
                    _provider,
                    _assetManager
                )
            )
        );
    }
}
