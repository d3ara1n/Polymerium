using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

    public override async Task<ComponentInstallerError?> StartAsync(Component component)
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
            return Failed(ComponentInstallerError.DependencyNotMet);

        var installerUrl =
            $"https://bmclapi2.bangbang93.com/forge/download?mcversion={mcVersion}&version={component.Version}&category=installer&format=jar";
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(installerUrl);
        var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        if (Token.IsCancellationRequested)
            return Canceled();
        var versionJson = await GetArchiveJsonAsync<InstallerVersion>(archive, "version.json");
        var profileJson = await GetArchiveJsonAsync<InstallerProfile>(
            archive,
            "install_profile.json"
        );

        if (versionJson.HasValue && profileJson.HasValue && profileJson.Value.Spec != null)
        {
            // 1.12 and above
            foreach (var library in versionJson.Value.Libraries)
                if (library.Downloads.Artifact.Url != null)
                    Context.AddLibrary(
                        new Library(
                            library.Name,
                            library.Downloads.Artifact.Path,
                            library.Downloads.Artifact.Sha1,
                            library.Downloads.Artifact.Url
                        )
                    );

            foreach (var library in profileJson.Value.Libraries)
                if (library.Downloads.Artifact.Url != null)
                    Context.AddLibrary(
                        new Library(
                            library.Name,
                            library.Downloads.Artifact.Path,
                            library.Downloads.Artifact.Sha1,
                            library.Downloads.Artifact.Url,
                            presentInClassPath: false
                        )
                    );

            if (!string.IsNullOrEmpty(versionJson.Value.MinecraftArguments))
                foreach (var argument in versionJson.Value.MinecraftArguments.Split(' '))
                    Context.AppendGameArgument(argument);

            if (versionJson.Value.Arguments.HasValue)
            {
                foreach (
                    var argument in versionJson.Value.Arguments.Value.Game
                                    ?? Enumerable.Empty<string>()
                )
                    Context.AppendGameArgument(argument);

                foreach (
                    var argument in versionJson.Value.Arguments.Value.Jvm
                                    ?? Enumerable.Empty<string>()
                )
                    Context.AppendJvmArguments(argument);
            }

            // add forge jar from archive
            var libs = archive.Entries.Where(
                x =>
                    x.FullName.StartsWith("maven/net/minecraftforge/forge")
                    && x.Name.EndsWith(".jar")
            );
            foreach (var entry in libs)
            {
                var path = entry.FullName[6..];
                var local = new Uri(
                    new Uri(
                        ConstPath.LOCAL_INSTANCE_LIBRARIES_DIR.Replace("{0}", Context.Instance.Id)
                    ),
                    path
                );
                var localPath = _fileBase.Locate(local);
                if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

                entry.ExtractToFile(localPath, true);

                Context.AddLibrary(
                    new Library(
                        $"net.minecraft.forge:{(entry.Name.Contains("universal") ? "universal" : "forge")}:{component.Version}",
                        path,
                        null,
                        local
                    )
                );
            }

            if (versionJson.Value.MainClass != "net.minecraft.launchwrapper.Launch")
                // 1.13+
                GoAheadWithWrapper(installerUrl, mcVersion, component.Version);
            else
                // 1.12
                Context.SetMainClass(versionJson.Value.MainClass);
        }
        else if (profileJson.HasValue)
        {
            // below 1.12
            Context.OverrideGameArguments();
            foreach (
                var argument in profileJson.Value.VersionInfo!.Value.MinecraftArguments.Split(' ')
            )
                Context.AppendGameArgument(argument);
            foreach (
                var library in profileJson.Value.VersionInfo!.Value.Libraries.Where(
                    x => x.ClientRequired.HasValue && x.ClientRequired.Value
                )
            )
            {
                // 库只给个 name 不给 url = =
                // 自杀吧
                // 直接报错得了
            }

            Context.SetMainClass(profileJson.Value.VersionInfo!.Value.MainClass);
            throw new NotImplementedException();
        }
        else
        {
            throw new NotImplementedException();
        }

        Context.AddCrate(
            "library_directory",
            _fileBase.Locate(new Uri(ConstPath.CACHE_LIBRARIES_DIR[..^1]))
        );
        return Finished();
    }

    private void GoAheadWithWrapper(
        string installerUrl,
        string coreVersion,
        string componentVersion
    )
    {
        Context.AddLibrary(
            new Library(
                $"net.minecraftforge:installer:{componentVersion}",
                $"net/minecraftforge/forge/{coreVersion}-{componentVersion}/forge-{coreVersion}-{componentVersion}-installer.jar",
                null,
                new Uri(installerUrl),
                presentInClassPath: false
            )
        );
        Context.AddLibrary(
            new Library(
                "io.github.zekerzhayard:ForgeWrapper:1.5.5",
                "io/github/zekerzhayard/ForgeWrapper/1.5.5/ForgeWrapper-1.5.5.jar",
                "4ee5f25cc9c7efbf54aff4c695da1054c1a1d7a3",
                new Uri(
                    "https://github.com/ZekerZhayard/ForgeWrapper/releases/download/1.5.5/ForgeWrapper-1.5.5.jar"
                )
            )
        );

        Context.AppendJvmArguments("-Dforgewrapper.librariesDir=${library_directory}");
        Context.AppendJvmArguments(
            $"-Dforgewrapper.installer=${{library_directory}}\\net\\minecraftforge\\forge\\{coreVersion}-{componentVersion}\\forge-{coreVersion}-{componentVersion}-installer.jar"
        );
        Context.AppendJvmArguments(
            $"-Dforgewrapper.minecraft=${{library_directory}}\\net\\minecraft\\minecraft\\{coreVersion}\\minecraft-{coreVersion}.jar"
        );

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