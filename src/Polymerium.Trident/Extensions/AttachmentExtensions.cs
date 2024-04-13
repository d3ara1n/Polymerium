using Polymerium.Trident.Helpers;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extensions;

public static class AttachmentExtensions
{
    public static string ToPurl(this Attachment self) =>
        PurlHelper.MakePurl(self.Label, self.ProjectId, self.VersionId);
}