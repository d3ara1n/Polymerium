using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    public SearchDetailViewModel(ViewModelContext context)
    {
        Context = context;
    }

    public ViewModelContext Context { get; }
}