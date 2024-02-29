using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines.Resolving;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class ResolveAttachmentStage(ResolveEngine resolver) : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            IEnumerable<Attachment> attachments =
                Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Attachments);
            foreach (Attachment attachment in attachments)
            {
                resolver.AddAttachment(attachment);
            }

            resolver.SetFilter(Context.Metadata.ExtractFilter());

            await foreach (ResolveResult result in resolver.ConfigureAwait(false))
            {
                if (result is { IsResolvedSuccessfully: true, Result: not null })
                {
                    Package? package = result.Result;
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