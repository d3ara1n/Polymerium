using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models.Game;
using Polymerium.Core.Models.Forge;

namespace Polymerium.Core.Components.Installers;

public class ForgeComponentInstaller : ComponentInstallerBase
{
    private readonly IFileBaseService _fileBase;

    public ForgeComponentInstaller(IFileBaseService fileBase)
    {
        _fileBase = fileBase;
    }

    public override async Task<Result<string>> StartAsync(Component component)
    {
        if (Token.IsCancellationRequested) return Canceled();
        var mcVersion = Context.GetCoreVersion();
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
            Context.AddLibrary(new Library
            {
                Name = library.Name,
                Path = library.Downloads.Artifact.Path,
                Sha1 = library.Downloads.Artifact.Sha1,
                Url = library.Downloads.Artifact.Url,
                IsNative = false
            });

        ;
        foreach (var argument in versionJson.Value.Arguments.Game) Context.AppendGameArgument(argument);

        foreach (var argument in versionJson.Value.Arguments.Jvm) Context.AppendJvmArguments(argument);

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