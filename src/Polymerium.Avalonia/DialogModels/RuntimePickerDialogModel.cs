using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.DialogModels;

public partial class RuntimePickerDialogModel : ViewModelBase
{
    private static readonly SemaphoreSlim ScanLock = new(1, 1);
    private static RuntimePickerDialogCandidateCollection? cachedCandidates;

    [ObservableProperty]
    public partial RuntimePickerDialogCandidateCollection? Candidates { get; private set; }

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        await base.OnInitializeAsync(token);
        await ScanAsync(token);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Candidates = null;
        await ScanAsync(cancellationToken, true);
    }

    private async Task ScanAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        await ScanLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && cachedCandidates != null)
            {
                Candidates = cachedCandidates;
                return;
            }

            var candidates = await JavaHelper.ScanJavaRuntimesAsync(cancellationToken);
            cachedCandidates = new(candidates);
            Candidates = cachedCandidates;
        }
        finally
        {
            ScanLock.Release();
        }
    }
}
