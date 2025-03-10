using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class BuildArtifactStage : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var builder = Context.ArtifactBuilder!;

        builder.SetViability(new DataLock.ViabilityData(DataLock.FORMAT,
                                                        PathDef.Default.Home,
                                                        Context.Key,
                                                        Context.Setup.Version,
                                                        Context.Setup.Loader,
                                                        Context.Setup.Stage.Concat(Context.Setup.Stash).ToList()));
        var artifact = builder.Build();

        var path = PathDef.Default.FileOfLockData(Context.Key);
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(artifact, JsonSerializerOptions.Web), token);

        Context.Artifact = artifact;
    }
}