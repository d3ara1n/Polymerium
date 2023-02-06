using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class DownloadLibrariesStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly GameInstance _instance;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;

    public DownloadLibrariesStage(GameInstance instance, PolylockData polylock, SHA1 sha1, IFileBaseService fileBase,
        DownloadEngine downloader)
    {
        _instance = instance;
        _polylock = polylock;
        _sha1 = sha1;
        _fileBase = fileBase;
        _downloader = downloader;
    }

    public override string StageName => "补全游戏依赖库文件";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested) return Cancel();
        var libraryDir = new Uri("poly-file:///libraries/");
        var nativesDir = new Uri($"poly-file://{_instance.Id}/natives/");
        var group = new DownloadTaskGroup { Token = Token };
        _fileBase.RemoveDirectory(nativesDir);
        foreach (var item in _polylock.Libraries)
        {
            if (Token.IsCancellationRequested) return Cancel();
            var libPath = new Uri(libraryDir, item.Path);
            if (!await _fileBase.VerifyHashAsync(libPath, item.Sha1, _sha1))
            {
                if (group.TryAdd(item.Url.AbsoluteUri, _fileBase.Locate(libPath), out var task))
                    if (item.IsNative)
                        task.CompletedCallback = async (task, s) =>
                        {
                            if (s)
                                await UnzipFileAsync(task.Destination, _fileBase.Locate(nativesDir),
                                    Token);
                        };
            }
            else
            {
                if (item.IsNative)
                    await UnzipFileAsync(_fileBase.Locate(libPath), _fileBase.Locate(nativesDir),
                        Token);
            }
        }

        group.CompletedDelegate = (_, task, downloaded, success) =>
        {
            Report($"已下载 {downloaded} 个文件，共 {group.TotalCount} 个", Path.GetFileName(task.Destination));
        };
        _downloader.Enqueue(group);
        if (group.Wait())
            return Next(new CompleteAttachmentsStage(_instance, _polylock, _sha1, _fileBase, _downloader));
        return Error($"{group.TotalCount - group.DownloadedCount} 个文件下载次数超过限定");
    }

    private async Task<bool> UnzipFileAsync(string from, string to, CancellationToken token)
    {
        try
        {
            ZipFile.ExtractToDirectory(from, to, true);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}