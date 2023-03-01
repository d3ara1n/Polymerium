using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    public ViewModelContext Context { get; }

    public SearchDetailViewModel(ViewModelContext context)
    {
        Context = context;
    }
}