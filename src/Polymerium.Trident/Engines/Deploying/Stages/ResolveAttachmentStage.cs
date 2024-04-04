using Microsoft.Extensions.Logging;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class ResolveAttachmentStage(ResolveEngine resolver) : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            var attachments =
                Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Attachments);
            foreach (var attachment in attachments)
            {
                resolver.AddAttachment(attachment);
            }

            resolver.SetFilter(Context.Metadata.ExtractFilter());

            await foreach (var result in resolver.ConfigureAwait(false))
            {
                if (result is { IsResolvedSuccessfully: true, Result: not null })
                {
                    var package = result.Result;
                    Context.ArtifactBuilder!.AddParcel($"{package.Label}/{package.ProjectId}/{package.VersionId}.obj",
                        $"{FileNameHelper.GetAssetFolderName(package.Kind)}/{package.FileName}", package.Download,
                        package.Hash);
                }
                else
                {
                    throw (Exception?)result.Exception ?? new NotSupportedException();
                }
            }

            // TODO: 解析依赖，并扁平化

            Logger.LogInformation("All attachments resolved, refer to artifact file for details");

            Context.IsAttachmentResolved = true;
        }
    }
}