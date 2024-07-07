using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class BuildArtifactStage : StageBase
{
    protected override async Task OnProcessAsync()
    {
        Context.ArtifactBuilder!.SetViability(Context.Key, Context.Watermark, Context.Trident.HomeDir);
        var artifact = Context.ArtifactBuilder!.Build();
        var path = Context.ArtifactPath;
        var content = JsonSerializer.Serialize(artifact, Context.SerializerOptions);
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content);
        Logger.LogInformation("Wrote artifact to path {path}", path);
        Context.Artifact = artifact;
    }
}