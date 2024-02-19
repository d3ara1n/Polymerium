using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trident.Abstractions.Building;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class BuildArtifactStage : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            Context.ArtifactBuilder!.SetViability(Context.Key, Context.Watermark, Context.Trident.HomeDir);
            Artifact artifact = Context.ArtifactBuilder!.Build();
            string path = Context.ArtifactPath;
            string content = JsonSerializer.Serialize(artifact, Context.SerializerOptions);
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(path, content);
            Logger.LogInformation("Wrote artifact to path {path}", path);
            Context.Artifact = artifact;
        }
    }
}