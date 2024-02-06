using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class CheckArtifactStage(string artifactPath) : StageBase
{
    public override async Task<bool> ProcessAsync()
    {
        if (File.Exists(artifactPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(artifactPath);
                var artifact = JsonSerializer.Deserialize<Artifact>(content);
                if (artifact != null && artifact.Verify(Context.Key, Context.Watermark, Context.Context.HomeDir))
                {
                    Context.Artifact = artifact;
                    Logger.LogInformation("Using artifact: {path}", Path.GetFileName(artifactPath));
                }
                else
                {
                    Context.ArtifactBuilder = Artifact.Builder();
                    Logger.LogInformation("Bad artifact");
                }
            }
            catch (JsonException e)
            {
                Context.ArtifactBuilder = Artifact.Builder();
                Logger.LogWarning("Load artifact in disk failed: {message}", e.Message);
            }
        }
        else
        {
            Context.ArtifactBuilder = Artifact.Builder();
            Logger.LogInformation("Create empty artifact");
        }

        return true;
    }
}