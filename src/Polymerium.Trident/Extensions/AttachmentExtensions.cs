using Polymerium.Trident.Helpers;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Extensions;

public static class AttachmentExtensions
{
    public static string ToAurl(this Attachment self) =>
        AurlHelper.MakeAurl(self);
}