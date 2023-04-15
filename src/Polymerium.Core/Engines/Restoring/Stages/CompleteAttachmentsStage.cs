using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Managers;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CompleteAttachmentsStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly GameInstance _instance;
    private readonly AssetManager _assetManager;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;

    public CompleteAttachmentsStage(
        GameInstance instance,
        PolylockData polylock,
        SHA1 sha1,
        IFileBaseService fileBase,
        DownloadEngine downloader,
        AssetManager assetManager
    )
    {
        _instance = instance;
        _polylock = polylock;
        _sha1 = sha1;
        _fileBase = fileBase;
        _downloader = downloader;
        _assetManager = assetManager;
    }

    public override string StageName => "补全实例元数据中的附件";


    public override async Task<Option<StageBase>> StartAsync()
    {
        var attachments = _polylock.Attachments;
        var group = new DownloadTaskGroup { Token = Token };
        var local = 0;
        var merge = new List<(PolylockAttachment, Uri)>();
        foreach (var attachment in attachments)
        {
            if (Token.IsCancellationRequested)
                return Cancel();

            if (attachment.Source.Scheme == "poly-file")
            {
                if (!await _fileBase.VerifyHashAsync(attachment.Target, attachment.Sha1, _sha1))
                {
                    var path = _fileBase.Locate(attachment.Source);
                    var target = _fileBase.Locate(attachment.Target);
                    var dir = Path.GetDirectoryName(target);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir!);
                    File.Copy(path, target, true);
                    Report($"使用本地仓库补全({++local})", Path.GetFileName(target));
                }
            }
            else
            {
                var pooled = new Uri(ConstPath.CACHE_OBJECTS_FILE.Replace("{0}", attachment.CachedObjectPath));
                // 可再生资源，可再生资源被视作只读资源
                if (!await _fileBase.VerifyHashAsync(pooled, attachment.Sha1, _sha1))
                {
                    group.TryAdd(
                        attachment.Source.AbsoluteUri,
                        _fileBase.Locate(pooled),
                        out var _
                    );
                }

                merge.Add((attachment, pooled));
            }
        }

        group.CompletedDelegate = (_, task, downloaded, success) =>
        {
            Report(
                $"已下载 {downloaded} 个文件，共 {group.TotalCount} 个",
                Path.GetFileName(task.Destination)
            );
        };
        _downloader.Enqueue(group);
        if (group.Wait())
        {
            var deployment = merge.Select(x => new RenewableAssetState
            {
                Source = x.Item2,
                Target = x.Item1.Target
            });
            var error = _assetManager.DeployRenewableAssets(_instance, deployment);
            return error.HasValue ? Error("部署最终可再生资源出错: 即将部署的文件路径被另一个的文件占用。没有更详细信息，因为传递不出来更多参数Orz") : Finish();
        }
        return Error($"{group.TotalCount - group.DownloadedCount} 个文件下载次数超过限定");
    }
}