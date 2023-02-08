using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CompleteAttachmentsStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly GameInstance _instance;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;

    public CompleteAttachmentsStage(GameInstance instance, PolylockData polylock, SHA1 sha1, IFileBaseService fileBase,
        DownloadEngine downloader)
    {
        _instance = instance;
        _polylock = polylock;
        _sha1 = sha1;
        _fileBase = fileBase;
        _downloader = downloader;
    }

    public override string StageName => "补全实例元数据所要求的附件";

    public override async Task<Option<StageBase>> StartAsync()
    {
        var attachments = _polylock.Attachments;
        var group = new DownloadTaskGroup
        {
            Token = Token
        };
        foreach (var attachment in attachments)
        {
            if (Token.IsCancellationRequested) return Cancel();
            if (!await _fileBase.VerifyHashAsync(attachment.Target, attachment.Sha1, _sha1))
            {
                if (attachment.Source.Scheme == "poly-file")
                {
                    // TODO: Just move the file and report
                }
                else
                {
                    group.TryAdd(attachment.Source.AbsoluteUri, _fileBase.Locate(attachment.Target), out var _);
                }
            }
        }

        group.CompletedDelegate = (_, task, downloaded, success) =>
        {
            Report($"已下载 {downloaded} 个文件，共 {group.TotalCount} 个", Path.GetFileName(task.Destination));
        };
        _downloader.Enqueue(group);
        if (group.Wait())
            return Finish();
        return Error($"{group.TotalCount - group.DownloadedCount} 个文件下载次数超过限定");
    }
}