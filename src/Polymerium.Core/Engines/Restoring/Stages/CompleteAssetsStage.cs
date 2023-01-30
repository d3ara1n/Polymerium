using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.Models.Mojang;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CompleteAssetsStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly AssetsIndex _index;
    private readonly GameInstance _instance;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;

    public CompleteAssetsStage(GameInstance instance, PolylockData polylock, AssetsIndex index, SHA1 sha1,
        IFileBaseService fileBase,
        DownloadEngine downloader)
    {
        _instance = instance;
        _polylock = polylock;
        _index = index;
        _sha1 = sha1;
        _fileBase = fileBase;
        _downloader = downloader;
    }

    public override string StageName => "补全游戏资产文件";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested) return Cancel();
        var group = new DownloadTaskGroup
        {
            Token = Token
        };
        foreach (var item in _index.Objects.Select(x => x.Hash))
        {
            if (Token.IsCancellationRequested) return Cancel();
            var path = new Uri($"poly-file:///assets/objects/{item[..2]}/{item}", UriKind.Absolute);
            if (!await _fileBase.VerfyHashAsync(path, item, _sha1))
                group.TryAdd($"https://resources.download.minecraft.net/{item[..2]}/{item}",
                    _fileBase.Locate(path), out var _);
        }

        group.CompletedDelegate = (_, task, downloaded, success) =>
            Report($"已下载 {downloaded} 个文件，共 {group.TotalCount} 个", Path.GetFileName(task.Destination));
        _downloader.Enqueue(group);
        if (group.Wait())
            return Next(new CompleteLibrariesStage(_instance, _polylock, _sha1, _fileBase, _downloader));
        return Error($"{group.TotalCount - group.DownloadedCount} 个文件下载次数超过限定");
    }
}