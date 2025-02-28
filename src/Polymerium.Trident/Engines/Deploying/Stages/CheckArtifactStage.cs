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
                if (artifact != null && Verify(artifact.Viability, Context.Setup))
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
            catch (JsonException e)
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

    private bool Verify(DataLock.ViabilityData data, Profile.Rice setup)
    {
        if (data.Version != setup.Version || data.Loader != setup.Loader)
            return false;

        if (data.Packages.Count != setup.Stage.Count + setup.Stash.Count)
            return false;

        var map = data.Packages.Distinct().ToDictionary(x => x, _ => 0);

        foreach (var check in setup.Stage.Concat(setup.Stash))
        {
            if (!map.TryGetValue(check, out var value))
                return false;

            map[check] = ++value;
        }

        return map.Values.All(x => x == 1);
    }
}