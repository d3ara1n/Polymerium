using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Core.Sources;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Rendering;

public sealed class DataImageSourceResolver : IImageSourceResolver
{
    public Task<ResolvedImageSource?> ResolveAsync(ImageLoadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!InternalUriHelper.HasScheme(request.Source, ImageSourceHelper.DATA_SCHEME))
        {
            return Task.FromResult<ResolvedImageSource?>(null);
        }

        if (!ImageSourceHelper.TryParseData(request.Source, out var payload))
        {
            throw new FormatException("Only Base64 image data URIs are supported.");
        }

        var bytes = Convert.FromBase64String(payload);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ResolvedImageSource?>(new(new MemoryStream(bytes, writable: false)));
    }
}
