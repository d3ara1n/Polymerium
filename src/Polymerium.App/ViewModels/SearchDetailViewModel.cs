using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    public SearchDetailViewModel(ViewModelContext context)
    {
        Context = context;
    }

    public ViewModelContext Context { get; }
    public ResourceBase? Resource { get; private set; }

    public void GotResource(ResourceBase resource)
    {
        Resource = resource;
    }
}