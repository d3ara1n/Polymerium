using CommunityToolkit.Mvvm.ComponentModel;
using DotNext;
using Polymerium.Abstractions.Importers;
using Polymerium.App.Models;
using Polymerium.App.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        if (!string.IsNullOrEmpty(InstanceName))
        {
            _importResult!.Content = _importResult.Content with { Name = InstanceName };
        }
        var result = await _importer.SolidifyAsync(_importResult!, null);
        callback(
            result.HasValue
                ? new Result<ImportResult, GameImportError>(result.Value)
                : _importResult!,
            true
        );
    }
}
