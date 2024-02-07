using Microsoft.Extensions.Logging;
using Polymerium.Trident.Extensions;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ResolveAttachmentStage(ResolveEngine resolver) : StageBase
{
    protected override async Task OnProcessAsync()
    {
        var attachments = Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Attachments);
        foreach (var attachment in attachments) resolver.AddAttachment(attachment);

        resolver.SetFilter(Context.Metadata.ExtractFilter());

        await foreach (var package in resolver.ConfigureAwait(false))
            Context.ArtifactBuilder!.AddParcel($"{package.Label}/{package.ProjectId}/{package.VersionId}.obj",
                $"mods/{package.FileName}", package.Download, package.Hash);

        Logger.LogInformation("All attachments resolved, refer to artifact file for details");

        Context.IsAttachmentResolved = true;
    }
}