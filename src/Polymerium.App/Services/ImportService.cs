using DotNext;
using DotNext.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Importers;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core;
using Polymerium.Core.Engines;
using Polymerium.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Services;

public class ImportService
{
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;
    private readonly ImportServiceOptions _options;
    private readonly IServiceProvider _provider;
    private readonly ResolveEngine _resolver;

    public ImportService(
        IOptions<ImportServiceOptions> options,
        IFileBaseService fileBase,
        InstanceManager instanceManager,
        ResolveEngine resolver,
        IServiceProvider provider
    )
    {
        _options = options.Value;
        _fileBase = fileBase;
        _instanceManager = instanceManager;
        _resolver = resolver;
        _provider = provider;
    }

    public async Task<Result<ImportResult, GameImportError>> ExtractMetadataFromFileAsync(
        string filePath,
        Uri? reference,
        bool forceOffline,
        CancellationToken token = default
    )
    {
        if (File.Exists(filePath))
            try
            {
                var archive = ZipFile.OpenRead(filePath);
                var rawFileList = archive.Entries
                    .Where(x => !x.FullName.EndsWith('/'))
                    .Select(x => x.FullName);
                var importerOption = _options.ImporterTypes.FirstOrNone(
                    x => rawFileList.Contains(x.Key)
                );
                if (importerOption)
                {
                    var (indexFileName, importerType) = importerOption.Value;
                    if (token.IsCancellationRequested)
                        return new Result<ImportResult, GameImportError>(GameImportError.Cancelled);
                    var importer = (ImporterBase)
                        ActivatorUtilities.CreateInstance(_provider, importerType);
                    importer.Token = token;
                    using var indexFileStream = archive.GetEntry(indexFileName)?.Open();
                    if (indexFileStream != null)
                    {
                        using var reader = new StreamReader(indexFileStream);
                        var indexFileContent = await reader.ReadToEndAsync();
                        var result = await importer.ExtractMetadataAsync(
                            filePath,
                            indexFileContent,
                            rawFileList,
                            reference,
                            forceOffline
                        );

                        return result.IsSuccessful
                            ? new Result<ImportResult, GameImportError>(
                                new ImportResult(archive, result.Value)
                            )
                            : new Result<ImportResult, GameImportError>(result.Error);
                    }

                    return new Result<ImportResult, GameImportError>(
                        GameImportError.ResourceNotFound
                    );
                }

                return new Result<ImportResult, GameImportError>(GameImportError.Unsupported);
            }
            catch(Exception ex)
            {
                return new Result<ImportResult, GameImportError>(GameImportError.FileSystemError);
            }

        return new Result<ImportResult, GameImportError>(GameImportError.FileSystemError);
    }

    public async Task<GameImportError?> SolidifyAsync(ImportResult product, GameInstance? instance)
    {
        return await Task.Run(
            new Func<GameImportError?>(() =>
            {
                var isGenerated = instance == null;
                instance ??= new GameInstance(
                    new GameMetadata(),
                    string.Empty,
                    new FileBasedLaunchConfiguration(),
                    string.Empty,
                    string.Empty
                );
                var allocateds = new List<Uri>();
                var localDir = _fileBase.Locate(
                    new Uri(ConstPath.LOCAL_INSTANCE_BASE.Replace("{0}", instance.Id))
                );
                try
                {
                    if (Directory.Exists(localDir))
                        Directory.Delete(localDir, true);
                    foreach (var file in product.Content.Files)
                    {
                        var path = _fileBase.Locate(
                            new Uri(
                                new Uri(ConstPath.LOCAL_INSTANCE_BASE.Replace("{0}", instance.Id)),
                                file.Path
                            )
                        );
                        var entry = product.Archive.GetEntry(file.FileName)!;
                        var dir = Path.GetDirectoryName(path)!;
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        entry.ExtractToFile(path);
                        allocateds.Add(new Uri(new Uri("poly-res://local@file/"), file.Path));
                    }
                }
                catch(Exception ex)
                {
                    return GameImportError.FileSystemError;
                }

                // update instance info and remain FolderName untouched
                var old = instance.ReferenceSource;
                instance.Version = product.Content.Version;
                instance.ReferenceSource = product.Content.ReferenceSource;
                instance.ThumbnailFile = product.Content.ThumbnailFile;
                instance.Metadata.Components.Clear();
                foreach (var component in product.Content.Metadata.Components)
                    instance.Metadata.Components.Add(component);
                var removings = instance.Metadata.Attachments.Where(x => x.From == old).ToList();
                foreach (var removing in removings)
                    instance.Metadata.Attachments.Remove(removing);
                foreach (var attachment in product.Content.Metadata.Attachments)
                    instance.Metadata.Attachments.Add(attachment);
                foreach (var allocated in allocateds)
                    instance.Metadata.Attachments.Add(
                        new Attachment(allocated, instance.ReferenceSource)
                    );
                if (isGenerated)
                {
                    instance.Name = product.Content.Name;
                    instance.Author = product.Content.Author;
                    instance.FolderName = PathHelper.RemoveInvalidCharacters(instance.Name);
                    _instanceManager.AddInstance(instance);
                }

                return null;
            })
        );
    }
}
