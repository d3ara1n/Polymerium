using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.ViewModels;

public class CreateInstanceWizardViewModel : ObservableObject
{
    private bool isLoading = true;

    public bool IsLoading
    {
        get => isLoading;
        set => SetProperty(ref isLoading, value);
    }

    private bool isInfiniteLoading = true;

    public bool IsInfiniteLoading
    {
        get => isInfiniteLoading;
        set => SetProperty(ref isInfiniteLoading, value);
    }

    public async Task FillDataAsync(Func<Task> callback)
    {
        Thread.Sleep(2000);
        await callback();
    }
}
