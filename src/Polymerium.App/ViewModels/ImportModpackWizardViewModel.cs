using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DotNext;
using Polymerium.Abstractions.Importers;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class ImportModpackWizardViewModel : ObservableObject
{
    private readonly ImportService _importer;

    private readonly CancellationTokenSource source = new();
    private string? _fileName;
    private ImportResult? _importResult;

    private ModpackPreviewModel? exposed;

    private string? instanceName = string.Empty;

    public ImportModpackWizardViewModel(ImportService importer)
    {
        _importer = importer;
    }

    public ModpackPreviewModel? Exposed
    {
        get => exposed;
        set => SetProperty(ref exposed, value);
    }

    public string? InstanceName
    {
        get => instanceName;
        set => SetProperty(ref instanceName, value);
    }

    public void GotFileName(string fileName)
    {
        _fileName = fileName;
    }

    public void RequestCancel()
    {
        source.Cancel();
    }

    public async Task ExtractInformationAsync(
        Action<Result<ImportResult, GameImportError>, bool> callback
    )
    {
        var result = await _importer.ExtractMetadataFromFileAsync(
            _fileName!,
            null,
            true,
            source.Token
        );
        if (result.IsSuccessful)
            _importResult = result.Value;
        if (!source.Token.IsCancellationRequested)
            callback(result, false);
    }

    public async Task ApplyExtractionAsync(
        Action<Result<ImportResult, GameImportError>, bool> callback
    )
    {
        var result = await _importer.SolidifyAsync(_importResult!, null);
        if (result.HasValue)
            callback(new Result<ImportResult, GameImportError>(result.Value), true);
        else
            callback(_importResult!, true);
    }
}