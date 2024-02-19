using PackageUrl;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Extensions
{
    public static class AttachmentExtensions
    {
        public static string ToPurl(this Attachment self)
        {
            return new PackageURL(self.Label, null, self.ProjectId,
                self.VersionId, null,
                null).ToString();
        }
    }
}