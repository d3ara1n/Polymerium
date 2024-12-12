using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Polymerium.App.Facilities;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class ExhibitionViewModel(ViewBag bag, ProfileService profileService) : ViewModelBase
{
    public string Title { get; } = $"Exhibition({bag.Parameter ?? "None"})";

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        return Task.Delay(TimeSpan.FromSeconds(5), token);
    }
}