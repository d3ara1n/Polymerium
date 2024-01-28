using Microsoft.Extensions.Logging;
using Polymerium.Trident.Exceptions;
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
    IHttpClientFactory factory) : TaskBase(key)
{
    public Project Project => project;
    public Project.Version Version => version;

    protected override async Task OnThreadAsync()
    {
        using var client = factory.CreateClient();
        var stream = await client.GetStreamAsync(version.Download);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;
        var result = await extractor.ExtractAsync(memory, (project, version), Token);
        if (result.IsSuccessful)
        {
            var container = result.Value;
            logger.LogInformation("Downloaded extracted modpack {name} ready to solidify", container.Original.Name);
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