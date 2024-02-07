using Microsoft.Extensions.Logging;
using Polymerium.Trident.Extractors;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using Trident.Abstractions.Resources;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Tasks;

public class InstallModpackTask(
    Project project,
    Project.Version version,
    string key,
    ILogger<InstallModpackTask> logger,
    ModpackExtractor extractor,
    IHttpClientFactory factory) : TaskBase(key, $"Install {project.Name}", "Preparing...")
{
    protected override async Task OnThreadAsync()
    {
        ReportProgress(status: "Downloading pack file...");
        using var client = factory.CreateClient();
        var stream = await client.GetStreamAsync(version.Download);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;
        ReportProgress(status: "Extracting metadata...");
        var result = await extractor.ExtractAsync(memory, (project, version), Token);
        if (result.IsSuccessful)
        {
            var container = result.Value;
            logger.LogInformation("Downloaded extracted modpack {name} ready to solidify", container.Original.Name);
            ReportProgress(status: "Exporting data & files...");
            await extractor.SolidifyAsync(container, null);
            logger.LogInformation("Solidified {name} as an managed instance", container.Original.Name);
        }
        else
        {
            logger.LogError("Install modpack failed for {error}", result.Error);
            throw new ExtractException(result.Error, PurlHelper.MakePurl(project.Label, project.Id, version.Id));
        }
    }
}