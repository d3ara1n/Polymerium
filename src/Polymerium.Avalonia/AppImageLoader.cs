using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Core.Sources;
using AsyncImageLoader.Core.Transport;
using Avalonia;
using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Rendering;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia;

public sealed class AppImageLoader : IAsyncImageLoader
{
    private const int MAX_REDIRECTS = 16;
    private readonly SkinRenderService _skinRenderer;
    private readonly CompositeImageSourceResolver _sources;
    private readonly HttpImageTransport _transport;
    private readonly ImageLoaderPipeline _pipeline;
    private bool _disposed;

    public AppImageLoader(HttpClient httpClient, SkinRenderService skinRenderer)
    {
        _skinRenderer = skinRenderer;
        _sources = new(new DataImageSourceResolver(), new ArchiveImageSourceResolver(),
            new FileImageSourceResolver(), new StorageImageSourceResolver(), new AvaloniaAssetSourceResolver());
        _transport = new(httpClient);
        _pipeline = ImageLoaderPipelineBuilder.Uncached()
            .UseSourceResolver(_sources)
            .UseTransport(_transport)
            .Build();
    }

    public async Task<IImageLease?> LoadAsync(ImageLoadRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        var source = request.Source;
        int? width = null;
        for (var redirects = 0; ImageSourceHelper.IsScheme(source, ImageSourceHelper.THUMBNAIL_SCHEME); redirects++)
        {
            if (redirects == MAX_REDIRECTS)
            {
                throw new InvalidOperationException($"The image source exceeded {MAX_REDIRECTS} redirects.");
            }

            if (!ImageSourceHelper.TryParseThumbnail(source, out var inner, out var dimension))
            {
                throw new FormatException("A thumbnail URI requires a source and a supported width.");
            }

            width ??= dimension;
            source = inner;
        }

        Bitmap? image;
        if (ImageSourceHelper.IsScheme(source, SkinHelper.Scheme))
        {
            if (!SkinHelper.TryParse(source, out var view, out var skinSource))
            {
                throw new FormatException("A skin URI requires a view type and a skin source.");
            }

            image = await _skinRenderer.RenderAsync(view, skinSource, cancellationToken).ConfigureAwait(false);
            if (image is not null && width is { } targetWidth && image.PixelSize.Width != targetWidth)
            {
                using var original = image;
                cancellationToken.ThrowIfCancellationRequested();
                var height = Math.Max(1, checked((int)Math.Round(image.PixelSize.Height * (double)targetWidth / image.PixelSize.Width)));
                image = image.CreateScaledBitmap(new PixelSize(targetWidth, height), BitmapInterpolationMode.LowQuality);
            }
        }
        else if (width is { } thumbnailWidth)
        {
            var innerRequest = new ImageLoadRequest(source, request.BaseUri, request.StorageProvider);
            image = await LoadThumbnailAsync(innerRequest, thumbnailWidth, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await _pipeline.LoadAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (image is null)
        {
            return null;
        }

        // The lease takes ownership only after cancellation has been checked.
        if (cancellationToken.IsCancellationRequested)
        {
            image.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return ImageLease.Owned(image);
    }

    private async Task<Bitmap?> LoadThumbnailAsync(ImageLoadRequest request, int width, CancellationToken token)
    {
        using var source = await _sources.ResolveAsync(request, token).ConfigureAwait(false);
        if (source is not null)
        {
            return await DecodeThumbnailAsync(source.Stream, width, token).ConfigureAwait(false);
        }

        await using var stream = await _transport.GetAsync(request, token).ConfigureAwait(false);
        return stream is null ? null : await DecodeThumbnailAsync(stream, width, token).ConfigureAwait(false);
    }

    private static async Task<Bitmap> DecodeThumbnailAsync(Stream stream, int width, CancellationToken token)
    {
        using var buffer = stream.CanSeek ? null : new MemoryStream();
        if (buffer is not null)
        {
            await stream.CopyToAsync(buffer, token).ConfigureAwait(false);
            buffer.Position = 0;
        }

        return await Task.Run(() => Bitmap.DecodeToWidth(buffer ?? stream, width, BitmapInterpolationMode.LowQuality), token)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pipeline.Dispose();
    }
}
