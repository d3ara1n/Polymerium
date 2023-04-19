using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Configurations;
using Polymerium.Core;
using Polymerium.Core.Engines;
using Polymerium.Core.Importers;

namespace Polymerium.App.Services;

public class ImportService
{
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;
    private readonly ResolveEngine _resolver;
    private readonly IServiceProvider _provider;
    private readonly AppSettings _settings;

    public ImportService(
        IFileBaseService fileBase,
        InstanceManager instanceManager,
        ResolveEngine resolver,
        IServiceProvider provider,
        AppSettings settings
    )
    {
        _fileBase = fileBase;
        _instanceManager = instanceManager;
        _resolver = resolver;
        _provider = provider;
        _settings = settings;
    }

    public async Task<Result<ImportResult, GameImportError>> ImportAsync(
        string filePath,
        Uri? source,
        CancellationToken? token = default
    )
    {
        if (File.Exists(filePath))
        {
            try
            {
                var forceOffline = _settings.ForceImportOffline;
                var archive = ZipFile.OpenRead(filePath);
                var files = archive.Entries.Select(x => x.FullName);
                if (files.Any(x => x == "modrinth.index.json"))
                {
                    var importer = ActivatorUtilities.CreateInstance<ModrinthImporter>(_provider);
                    importer.Token = token ?? CancellationToken.None;
                    return await importer.ProcessAsync(archive, source, forceOffline);
                }

                if (files.Any(x => x == "manifest.json"))
                {
                    var importer = ActivatorUtilities.CreateInstance<CurseForgeImporter>(_provider);
                    importer.Token = token ?? CancellationToken.None;
                    return await importer.ProcessAsync(archive, source, forceOffline);
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
                            new Uri(
                                new Uri(
                                    ConstPath.LOCAL_INSTANCE_BASE.Replace(
                                        "{0}",
                                        product.Instance.Id
                                    )
                                ),
                                file.Path
                            )
                        );
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
                    product.Instance.Metadata.Attachments.Add(
                        new Attachment { Source = file, From = product.Instance.ReferenceSource }
                    );
                _instanceManager.AddInstance(product.Instance);
                return null;
            })
        );
    }
}
