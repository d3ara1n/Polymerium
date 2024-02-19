using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class CheckArtifactStage : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            string artifactPath = Context.ArtifactPath;
            if (File.Exists(artifactPath))
            {
                try
                {
                    string content = await File.ReadAllTextAsync(artifactPath);
                    Artifact? artifact = JsonSerializer.Deserialize<Artifact>(content, Context.SerializerOptions);
                    if (artifact != null && artifact.Verify(Context.Key, Context.Watermark, Context.Trident.HomeDir))
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

            Context.IsAttachmentResolved = false;
            Context.IsLoaderProcessed = false;
            Context.IsGameInstalled = false;
        }
    }
}