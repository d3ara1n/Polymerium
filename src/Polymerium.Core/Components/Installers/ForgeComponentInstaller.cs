using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models.Game;
using Polymerium.Core.Extensions;
using Polymerium.Core.Models.Forge;

namespace Polymerium.Core.Components.Installers;

public sealed class ForgeComponentInstaller : ComponentInstallerBase
{
    private readonly IFileBaseService _fileBase;

    public ForgeComponentInstaller(IFileBaseService fileBase)
    {
        _fileBase = fileBase;
    }

    public override async Task<Result<string>> StartAsync(Component component)
    {
        if (Token.IsCancellationRequested) return Canceled();
        var mcVersion = Context.Instance.GetCoreVersion();
        if (mcVersion == null) return Failed("Forge depends on net.minecraft which is not found");

        var installerUrl =
            $"https://bmclapi2.bangbang93.com/forge/download?mcversion={mcVersion}&version={component.Version}&category=installer&format=jar";
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(installerUrl);
        var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        if (Token.IsCancellationRequested) return Canceled();
        var versionJson = await GetArchiveJsonAsync<InstallerVersion>(archive, "version.json");
        if (!versionJson.HasValue) return Failed("Legacy forge installer is not supported");

        if (Token.IsCancellationRequested) return Canceled();
        var profileJson = await GetArchiveJsonAsync<InstallerProfile>(archive, "version.json");
        if (!profileJson.HasValue) return Failed("Legacy forge installer is not supported");

        foreach (var library in versionJson.Value.Libraries)
            Context.AddLibrary(new Library(library.Name, library.Downloads.Artifact.Path,
                library.Downloads.Artifact.Sha1, library.Downloads.Artifact.Url));
        // Note: 早些版本的 forge-client.jar 是随 installer.jar 附带的，需要用 local repository 服务来保证 Library.Url
        //       PolylockData 中可以包含 build tasks 来产生缺失但又无法下载的 forge libraries
        //       例如 minecraftforge-client.jar 的 url 为 poly-build://{build}[/{task}]
        //       PolylockData.Builds[name={path_of_library}].Tasks[name={task}]
        //       指定 task 构建会按照 task.DependsOn 依次先从上向下顺序构建
        //       不指定 task 构建会不断扫描序列中 DependsOn 为 null 或 DependsOn.IsCompleted 的 task
        //       高版本的 Forge 的 processors 就用 build tasks 实现
        // Note: Library.Url 可以改名为 Source
        if (versionJson.Value.Arguments.HasValue)
        {
            foreach (var argument in versionJson.Value.Arguments.Value.Game) Context.AppendGameArgument(argument);

            foreach (var argument in versionJson.Value.Arguments.Value.Jvm) Context.AppendJvmArguments(argument);
        }
        else if (!string.IsNullOrEmpty(versionJson.Value.MinecraftArguments))
        {
            Context.OverrideGameArguments();
            foreach (var argument in versionJson.Value.MinecraftArguments.Split(' '))
                Context.AppendGameArgument(argument);
        }

        Context.AddCrate("library_directory", _fileBase.Locate(new Uri("poly-file:///libraries")));

        // TODO: processor!

        Context.SetMainClass(versionJson.Value.MainClass);
        return Finished();
    }

    private async Task<TJson?> GetArchiveJsonAsync<TJson>(ZipArchive archive, string fileName)
        where TJson : struct
    {
        var entry = archive.GetEntry(fileName);
        if (entry == null) return null;

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<TJson>(content);
    }
}