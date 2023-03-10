using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.Importers;
using Polymerium.Core;
using Polymerium.Core.Importers;

namespace Polymerium.App.Services;

public class ImportService
{
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;

    public ImportService(
        IFileBaseService fileBase,
        InstanceManager instanceManager
    )
    {
        _fileBase = fileBase;
        _instanceManager = instanceManager;
    }

    public async Task<Result<ImportResult, GameImportError>> ImportAsync(string filePath,
        CancellationToken? token = default)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var archive = ZipFile.OpenRead(filePath);
                var files = archive.Entries.Select(x => x.FullName);
                if (files.Any(x => x == "modrinth.index.json"))
                {
                    var importer = new ModrinthImporter { Token = token ?? CancellationToken.None };
                    return await importer.ProcessAsync(archive);
                }

                if (files.Any(x => x == "manifest.json"))
                {
                    var importer = new CurseForgeImporter { Token = token ?? CancellationToken.None };
                    return await importer.ProcessAsync(archive);
                }
            }
            catch
            {
                return new Result<ImportResult, GameImportError>(GameImportError.WrongPackType);
            }

            return new Result<ImportResult, GameImportError>(GameImportError.Unsupported);
        }

        return new Result<ImportResult, GameImportError>(GameImportError.FileSystemError);
    }

    public async Task<GameImportError?> PostImportAsync(ImportResult product)
    {
        return await Task.Run(
            new Func<GameImportError?>(() =>
            {
                var allocated = new List<Uri>();
                foreach (var file in product.Files)
                    try
                    {
                        var path = _fileBase.Locate(
                            new Uri(new Uri($"poly-file:///local/instances/{product.Instance.Id}/"), file.Path));
                        var entry = product.Archive.GetEntry(file.FileName)!;
                        var dir = Path.GetDirectoryName(path);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir!);

                        entry.ExtractToFile(path);
                        allocated.Add(new Uri(new Uri("poly-res://local@file/"), file.Path));
                    }
                    catch
                    {
                        return GameImportError.FileSystemError;
                    }

                foreach (var file in allocated)
                    product.Instance.Metadata.Attachments.Add(file);

                _instanceManager.AddInstance(product.Instance);
                return null;
            })
        );
    }
}