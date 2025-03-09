using System.Text.Json;
using Microsoft.Extensions.Logging;
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
                var content = await File.ReadAllTextAsync(artifactPath, token);
                var artifact = JsonSerializer.Deserialize<DataLock>(content, JsonSerializerOptions.Web);
                if (artifact != null && Verify(artifact.Viability))
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

    private bool Verify(DataLock.ViabilityData data)
    {
        if (data.Format != DataLock.FORMAT)
            return false;
        
        if (data.Home != PathDef.Default.Home
         || data.Key != Context.Key
         || data.Version != Context.Setup.Version
         || data.Loader != Context.Setup.Loader)
            return false;

        if (data.Packages.Count != Context.Setup.Stage.Count + Context.Setup.Stash.Count)
            return false;

        var map = data.Packages.Distinct().ToDictionary(x => x, _ => 0);

        foreach (var check in Context.Setup.Stage.Concat(Context.Setup.Stash))
        {
            if (!map.TryGetValue(check, out var value))
                return false;

            map[check] = ++value;
        }

        return map.Values.All(x => x == 1);
    }
}