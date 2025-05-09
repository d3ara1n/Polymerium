using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class CheckArtifactStage(ILogger<CheckArtifactStage> logger) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var artifactPath = PathDef.Default.FileOfLockData(Context.Key);
        if (File.Exists(artifactPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(artifactPath, token).ConfigureAwait(false);
                var artifact = JsonSerializer.Deserialize<DataLock>(content, JsonSerializerOptions.Web);
                if (artifact != null && artifact.Verify(Context.Key, Context.Setup, Context.VerificationWatermark))
                {
                    Context.Artifact = artifact;
                    logger.LogInformation("Using artifact: {path}", Path.GetFileName(artifactPath));
                }
                else
                {
                    Context.ArtifactBuilder = new DataLockBuilder();
                    logger.LogInformation("Bad artifact");
                }
            }
            catch (Exception e)
            {
                Context.ArtifactBuilder = new DataLockBuilder();
                logger.LogWarning("Load artifact in disk failed: {message}", e.Message);
            }
        }
        else
        {
            Context.ArtifactBuilder = new DataLockBuilder();
            logger.LogInformation("Create empty artifact");
        }
    }
}