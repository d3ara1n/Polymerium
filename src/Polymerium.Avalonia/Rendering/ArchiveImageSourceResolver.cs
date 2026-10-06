using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Core.Sources;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Rendering;

public sealed class ArchiveImageSourceResolver : IImageSourceResolver
{
    public async Task<ResolvedImageSource?> ResolveAsync(
        ImageLoadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ImageSourceHelper.IsScheme(request.Source, ImageSourceHelper.ARCHIVE_SCHEME))
        {
            return null;
        }

        if (!ImageSourceHelper.TryParseArchive(request.Source, out var archiveSource, out var entryName))
        {
            throw new FormatException("An archive URI requires a local file source and an entry.");
        }

        await using var archive = await ZipFile.OpenReadAsync(archiveSource.LocalPath, cancellationToken);
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            return null;
        }

        await using var input = await entry.OpenAsync(cancellationToken);
        var output = new MemoryStream();
        try
        {
            await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            output.Position = 0;
            return new ResolvedImageSource(output);
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }
}
