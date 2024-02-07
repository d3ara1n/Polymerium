using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.PrismLauncher.Minecraft;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class InstallVanillaStage(IHttpClientFactory factory) : StageBase
{
    private const string INDEX_URL = "https://meta.prismlauncher.org/v1/net.minecraft/index.json";
    private const string VERSION_URL = "https://meta.prismlauncher.org/v1/net.minecraft/{version}.json";

    protected override async Task OnProcessAsync()
    {
        // 使用本地创建的 options，和 Polymerium 的文件不共用同一个
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var builder = Context.ArtifactBuilder!;
        using var client = factory.CreateClient();
        var manifest = await client.GetFromJsonAsync<PrismMinecraftIndex>(INDEX_URL, options, Context.Token);
        if (manifest.Equals(default)) throw new BadFormatException($"File({INDEX_URL}) failed to download or parse");
        Logger.LogInformation("Got manifest with {count} entries", manifest.Versions.Length);
        var index = manifest.Versions.FirstOrDefault(x => x.Version == Context.Metadata.Version);
        if (index.Equals(default))
            throw new BadFormatException(INDEX_URL, $"versions[version:{Context.Metadata.Version}]");
        var url = VERSION_URL.Replace("{version}", index.Version);
        var version = await client.GetFromJsonAsync<PrismMinecraftVersion>(url, options, Context.Token);
        if (version.Equals(default)) throw new BadFormatException($"File({url}) failed to download or parse");
        Logger.LogInformation("Got version index {version}({uid})", version.Version, version.Uid);

        // Libraries
        var osString = $"{GetOs()}-{GetArch()}";
        Logger.LogInformation("Current os string: {string}", osString);
        var libraries = version.Libraries.Where(x =>
        {
            if (x.Rules != null && x.Rules.Any())
                return x.Rules.All(y =>
                {
                    var pass = true;
                    if (y.Os != null && y.Os.TryGetValue("name", out var os))
                        // name
                        pass = osString == os;

                    return y.Action == PrismMinecraftVersionLibraryRuleAction.Allow ? pass : !pass;
                });

            return true;
        });
        foreach (var lib in libraries)
        {
            if (lib.Downloads.Artifact.HasValue)
                builder.AddLibrary(lib.Name, lib.Downloads.Artifact.Value.Url, lib.Downloads.Artifact.Value.Sha1);

            if (lib.Natives.HasValue && lib.Natives.Value.Windows != null)
            {
                var classifier = lib.Natives.Value.Windows.Replace(
                    "${arch}",
                    Environment.Is64BitOperatingSystem ? "64" : "32"
                );
                if (lib.Downloads.Classifiers.TryGetValue(classifier, out var download))
                    builder.AddLibrary(lib.Name, download.Url, download.Sha1, true, false);
            }
        }

        Logger.LogInformation("Libraries added, refer to artifact file for details");

        // Main Jar as a Library as well
        if (version.MainJar.Downloads.Artifact.HasValue)
            builder.AddLibrary(version.MainJar.Name, version.MainJar.Downloads.Artifact.Value.Url,
                version.MainJar.Downloads.Artifact.Value.Sha1);
        else
            throw new BadFormatException(url, "mainJar.downloads.artifact");

        Logger.LogInformation("Client jar appended: {name}", version.MainJar.Name);

        // Game Arguments
        var arguments = version.MinecraftArguments.Split(' ').Concat([
            // 额外添加的窗口设定
            "--width",
            "${resolution_width}",
            "--height",
            "${resolution_height}"
        ]);
        foreach (var arg in arguments)
            builder.AddGameArgument(arg);

        Logger.LogInformation("Game arguments added, refer to artifact file for details");

        // Jvm Arguments
        string[] jvmArguments =
        [
            // 由于版本文件不再提供，这里手动生成，还有个 logging，这里就不加了
            "-Djava.library.path=${natives_directory}",
            "-Djna.tmpdir=${natives_directory}",
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
            "-Dio.netty.native.workdir=${natives_directory}",
            "-Dminecraft.launcher.brand=${launcher_name}",
            "-Dminecraft.launcher.version=${launcher_version}",
            // Windows 下的优化，总是 Windows，所以总是添加这一项
            "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump",
            // 操作系统信息
            "-Dos.name=${os_name}",
            "-Dos.arch=${os_arch}",
            "-Dos.version=${os_version}",
            // 最大内存
            "-Xmx${jvm_max_memory}",
            "-cp",
            "${classpath}"
        ];
        foreach (var arg in jvmArguments)
            builder.AddJvmArgument(arg);

        Logger.LogInformation("Jvm arguments generated, refer to artifact file for details");

        // Java Major Version
        var firstJreVersion = version.CompatibleJavaMajors.FirstOrDefault();
        if (!firstJreVersion.Equals(default))
            builder.SetJavaMajorVersion(firstJreVersion);
        else
            throw new BadFormatException(url, "compatibleJavaMajors");

        Logger.LogInformation("Set java major version compatibility to {major} in [{all}]", firstJreVersion,
            string.Join(',', version.CompatibleJavaMajors));

        // AssetIndex
        builder.SetAssetIndex(version.AssetIndex.Id, version.AssetIndex.Url, version.AssetIndex.Sha1);

        Logger.LogInformation("Set asset index to {index}", version.AssetIndex.Id);

        // Main Class Path
        builder.SetMainClass(version.MainClass);

        Logger.LogInformation("Set main class path to {mainClass}", version.MainClass);


        Context.IsGameInstalled = true;
    }

    private static string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";
        throw new NotSupportedException("Unsupported operating system.");
    }

    private static string GetArch()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => throw new NotSupportedException("Unsupported process architecture.")
        };
    }
}