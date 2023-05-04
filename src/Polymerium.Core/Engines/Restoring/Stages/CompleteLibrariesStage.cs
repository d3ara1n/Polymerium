using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Models;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.Managers;
using Polymerium.Core.StageModels;

namespace Polymerium.Core.Engines.Restoring.Stages;

public class CompleteLibrariesStage : StageBase
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly GameInstance _instance;
    private readonly PolylockData _polylock;
    private readonly SHA1 _sha1;
    private readonly AssetManager _assetManager;

    public CompleteLibrariesStage(
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

    public override string StageNameKey => "补全游戏依赖库文件";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (Token.IsCancellationRequested)
            return Cancel();
        var libraryDir = new Uri(ConstPath.CACHE_LIBRARIES_DIR);
        var nativesDir = new Uri(ConstPath.INSTANCE_NATIVES_DIR.Replace("{0}", _instance.Id));
        var group = new DownloadTaskGroup { Token = Token };
        _fileBase.RemoveDirectory(nativesDir);
        var local = 0;
        foreach (var item in _polylock.Libraries)
        {
            if (Token.IsCancellationRequested)
                return Cancel();
            var libPath = new Uri(libraryDir, item.Path);
            if (item.Url != null)
            {
                if (!await _fileBase.VerifyHashAsync(libPath, item.Sha1, _sha1))
                {
                    if (item.Url.Scheme == "poly-file")
                    {
                        var target = _fileBase.Locate(libPath);
                        if (!Directory.Exists(Path.GetDirectoryName(target)))
                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                        File.Copy(_fileBase.Locate(item.Url), target, true);
                        Report($"使用本地仓库补全({++local})", Path.GetFileName(target));
                    }
                    else
                    {
                        if (
                            group.TryAdd(
                                item.Url.AbsoluteUri,
                                _fileBase.Locate(libPath),
                                out var task
                            ) && item.IsNative
                        )
                            task!.CompletedCallback = (t, s) =>
                            {
                                if (s)
                                    UnzipFile(t.Destination, _fileBase.Locate(nativesDir));
                            };
                    }
                }
                else
                {
                    if (item.IsNative)
                        if (!UnzipFile(_fileBase.Locate(libPath), _fileBase.Locate(nativesDir)))
                            return Error("解压 natives 文件时出现异常");
                }
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
            return Next(
                new CompleteAttachmentsStage(
                    _instance,
                    _polylock,
                    _sha1,
                    _fileBase,
                    _downloader,
                    _assetManager
                )
            );
        return Error($"{group.TotalCount - group.DownloadedCount} 个文件下载次数超过限定");
    }

    private bool UnzipFile(string from, string to)
    {
        try
        {
            ZipFile.ExtractToDirectory(from, to, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
