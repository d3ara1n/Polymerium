using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Components;
using Polymerium.Core.Extensions;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CheckAvailabilityStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;

    private readonly GameInstance _instance;
    private readonly IEnumerable<ComponentMeta> _metas;
    private readonly IServiceProvider _provider;
    private readonly SHA1 _sha1;

    public CheckAvailabilityStage(GameInstance instance, SHA1 sha1, IEnumerable<ComponentMeta> metas,
        DownloadEngine downloader, IFileBaseService fileBase, IServiceProvider provider)
    {
        _instance = instance;
        _sha1 = sha1;
        _metas = metas;
        _downloader = downloader;
        _fileBase = fileBase;
        _provider = provider;
    }

    public override string StageName => "检查资源可用性";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested) return Error("还原过程取消");
        var polylockDataFile = new Uri($"poly-file://{_instance.Id}/polymerium.lock.json");
        // 不是简单的 md5, 所以文件名也不应该是 .md5
        var polylockHashFile = new Uri($"poly-file://{_instance.Id}/polymerium.lock.json.hash");
        if (_fileBase.TryReadAllText(polylockHashFile, out var hash) && hash == _instance.ComputeMetadataHash() &&
            _fileBase.TryReadAllText(polylockDataFile, out var content))

        {
            var polylock = JsonConvert.DeserializeObject<PolylockData>(content);
            return Next(new LoadAssetIndexStage(_instance, _sha1, polylock, _fileBase, _downloader));
        }

        return Next(new BuildStructureStage(_instance, _sha1, _metas, polylockDataFile, polylockHashFile, _downloader,
            _provider));
    }
}