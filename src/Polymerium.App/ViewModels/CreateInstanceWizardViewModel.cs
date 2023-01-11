using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.DownloadSources;
using Polymerium.Abstractions.DownloadSources.Models;

namespace Polymerium.App.ViewModels;

public class CreateInstanceWizardViewModel : ObservableObject
{
    private readonly IEnumerable<DownloadSourceProviderBase> _providers;

    public CreateInstanceWizardViewModel(IEnumerable<DownloadSourceProviderBase> providers)
    {
        _providers = providers;
    }

    private string instanceName = string.Empty;
    public string InstanceName { get => instanceName; set => SetProperty(ref instanceName, value); }

    private GameVersion? selectedVersion;
    public GameVersion? SelectedVersion { get => selectedVersion; set => SetProperty(ref selectedVersion, value); }

    public async Task FillDataAsync(Func<IEnumerable<GameVersion>, Task> callback)
    {
        var versions = Enumerable.Empty<GameVersion>();
        foreach (var provider in _providers)
        {
            var versions_option = await provider.GetGameVersionsAsync();
            if (versions_option.TryUnwrap(out var data))
            {
                versions = data;
                break;
            }
        }
        await callback(versions.ToList());
    }
}
