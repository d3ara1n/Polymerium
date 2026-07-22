using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;
using AppResources = Polymerium.Avalonia.Properties.Resources;

namespace Polymerium.Avalonia.DialogModels;

// The project's single online-import entry. Trident's library-level Import only consumes
// local zip files, but Polymerium's user-facing Import spans both online references and local
// files; this dialog unifies that notion. Classification is local and network-free — pref://
// parses straight to a PackageIdentifier, http(s) URLs surface as Uri for the consumer to
// resolve via Trident's RepositoryAgent.RecognizeAsync.
public partial class ModpackImporterDialogModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    public partial string? Input { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedText))]
    public partial ModpackImporterResult? Result { get; private set; }

    [ObservableProperty]
    public partial string? Hint { get; private set; }

    public string? ResolvedText =>
        Result switch
        {
            ModpackImporterResult.File f => f.Path,
            ModpackImporterResult.Pref p => p.Identifier.ToString(),
            ModpackImporterResult.Uri u => u.Value.ToString(),
            _ => null
        };

    public bool CanValidate => !string.IsNullOrWhiteSpace(Input);

    // Editing the input invalidates any previously classified result; the user must re-validate.
    partial void OnInputChanged(string? value)
    {
        Result = null;
        Hint = null;
    }

    [RelayCommand(CanExecute = nameof(CanValidate))]
    private void Validate()
    {
        var input = Input?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        Result = ModpackUrlDetectionHelper.Detect(input);
        Hint = Result is null ? AppResources.ModpackImporterDialog_UnrecognizedHint : null;
    }
}
