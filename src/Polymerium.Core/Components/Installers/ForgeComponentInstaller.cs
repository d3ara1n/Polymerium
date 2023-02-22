using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        // Note: 早些版本的 forge-client.jar 是随 installer.jar 附带的，需要用 local repository 服务来保证 Library.Url
        //       PolylockData 中可以包含 build tasks 来产生缺失但又无法下载的 forge libraries
        //       例如 minecraftforge-client.jar 的 url 为 poly-build://{build}[/{task}]
        //       PolylockData.Builds[name={path_of_library}].Tasks[name={task}]
        //       指定 task 构建会按照 task.DependsOn 依次先从上向下顺序构建
        //       不指定 task 构建会不断扫描序列中 DependsOn 为 null 或 DependsOn.IsCompleted 的 task
        //       高版本的 Forge 的 processors 就用 build tasks 实现
        // Note: Library.Url 可以改名为 Source

        if (Token.IsCancellationRequested)
            return Canceled();
        var mcVersion = Context.Instance.GetCoreVersion();
        if (mcVersion == null)
            return Failed("Forge depends on net.minecraft which is not found");

        var installerUrl =
            $"https://bmclapi2.bangbang93.com/forge/download?mcversion={mcVersion}&version={component.Version}&category=installer&format=jar";
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(installerUrl);
        var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        if (Token.IsCancellationRequested)
            return Canceled();
        var versionJson = await GetArchiveJsonAsync<InstallerVersion>(archive, "version.json");
        var profileJson = await GetArchiveJsonAsync<InstallerProfile>(archive, "install_profile.json");

        if (versionJson.HasValue && profileJson.HasValue && profileJson.Value.Spec != null)
        {
            // above 1.12
            foreach (var library in versionJson.Value.Libraries)
                if (library.Downloads.Artifact.Url != null)
                    Context.AddLibrary(new Library(library.Name, library.Downloads.Artifact.Path,
                        library.Downloads.Artifact.Sha1, library.Downloads.Artifact.Url));

            foreach (var library in profileJson.Value.Libraries)
                if (library.Downloads.Artifact.Url != null)
                    Context.AddLibrary(new Library(library.Name, library.Downloads.Artifact.Path,
                        library.Downloads.Artifact.Sha1, library.Downloads.Artifact.Url, presentInClassPath: false));

            if (!string.IsNullOrEmpty(versionJson.Value.MinecraftArguments))
                foreach (var argument in versionJson.Value.MinecraftArguments.Split(' '))
                    Context.AppendGameArgument(argument);

            if (versionJson.Value.Arguments.HasValue)
            {
                foreach (var argument in versionJson.Value.Arguments.Value.Game ?? Enumerable.Empty<string>())
                    Context.AppendGameArgument(argument);

                foreach (var argument in versionJson.Value.Arguments.Value.Jvm ?? Enumerable.Empty<string>())
                    Context.AppendJvmArguments(argument);
            }

            // add forge client, launcher from maven url
            var libs = archive.Entries.Where(x =>
                x.FullName.StartsWith("maven/net/minecraftforge/forge") && x.Name.EndsWith(".jar"));
            foreach (var entry in libs)
            {
                var path = entry.FullName[6..];
                var local = new Uri(new Uri($"poly-file:///local/instances/{Context.Instance.Id}/libraries/"), path);
                var localPath = _fileBase.Locate(local);
                if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                entry.ExtractToFile(localPath, true);

                Context.AddLibrary(
                    new Library(
                        $"net.minecraft.forge:{(entry.Name.Contains("universal") ? "universal" : "forge")}:{component.Version}",
                        path, null, local));
            }
            GoAheadWithWrapper(installerUrl, mcVersion, component.Version);
        }
        else
        {
            //var install = profileJson.Value.Install.Value;
            //var filePath = install.FilePath;
            //var entry = archive.GetEntry(filePath)!;
            //var path = $"forge-{mcVersion}-{component.Version}-universal.jar";
            //var local = new Uri(new Uri($"poly-file:///local/instances/{Context.Instance.Id}/libraries/"), path);
            //var localPath = _fileBase.Locate(local);
            //if (!Directory.Exists(Path.GetDirectoryName(localPath)))
            //    Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            //entry.ExtractToFile(localPath, true);

            //Context.AddLibrary(
            //    new Library(
            //        $"net.minecraft.forge:{(entry.Name.Contains("universal") ? "universal" : "forge")}:{component.Version}",
            //        path, null, local));

            throw new NotImplementedException();

        }

        Context.AddCrate("library_directory", _fileBase.Locate(new Uri("poly-file:///libraries")));
        return Finished();
    }

    private void GoAheadByExtracting(ZipArchiveEntry entry, string mainClass, string componentVersion)
    {
        var path = $"net/minecraftforge/{componentVersion}/forge-{componentVersion}-client.jar";
        var local = new Uri(new Uri($"poly-file:///local/instances/{Context.Instance.Id}/"), path);
        var localPath = _fileBase.Locate(local);

        if (!Directory.Exists(Path.GetDirectoryName(localPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        entry.ExtractToFile(localPath, true);
        Context.AddLibrary(new Library($"net.minecraftforge:forge:{componentVersion}", path, null, local));
        Context.SetMainClass(mainClass);
    }

    private void GoAheadWithProcessors(string mainClass)
    {
        Context.SetMainClass(mainClass);
        throw new NotImplementedException();
    }

    private void GoAheadWithWrapper(string installerUrl, string coreVersion, string componentVersion)
    {
        Context.AddLibrary(new Library($"net.minecraftforge:installer:{componentVersion}",
            $"net/minecraftforge/installer/{componentVersion}/forge-installer-{componentVersion}.jar",
            null,
            new Uri(installerUrl), presentInClassPath: false));
        Context.AddLibrary(new Library("com.github.zekerzhayard:ForgeWrapper:1.5.5",
            "com/github/zekerzhayard/ForgeWrapper/1.5.5/ForgeWrapper-1.5.5.jar",
            "4ee5f25cc9c7efbf54aff4c695da1054c1a1d7a3",
            new Uri(
                "https://github.com/ZekerZhayard/ForgeWrapper/releases/download/1.5.5/ForgeWrapper-1.5.5.jar")));

        Context.AppendJvmArguments("-Dforgewrapper.librariesDir=${library_directory}");
        Context.AppendJvmArguments(
            $"-Dforgewrapper.installer=${{library_directory}}\\net\\minecraftforge\\installer\\{componentVersion}\\forge-installer-{componentVersion}.jar");
        Context.AppendJvmArguments(
            $"-Dforgewrapper.minecraft=${{library_directory}}\\net\\minecraft\\minecraft\\{coreVersion}\\minecraft-{coreVersion}.jar");

        Context.SetMainClass("io.github.zekerzhayard.forgewrapper.installer.Main");
    }

    private async Task<TJson?> GetArchiveJsonAsync<TJson>(ZipArchive archive, string fileName)
        where TJson : struct
    {
        var entry = archive.GetEntry(fileName);
        if (entry == null)
            return null;

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<TJson>(content);
    }
}