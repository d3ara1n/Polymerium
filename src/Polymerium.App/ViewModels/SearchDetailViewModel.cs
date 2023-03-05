using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;
using Polymerium.Core.Resources;

namespace Polymerium.App.ViewModels;

public class SearchDetailViewModel : ObservableObject
{
    public SearchDetailViewModel(ViewModelContext context)
    {
        Context = context;
    }

    public ViewModelContext Context { get; }
    public RepositoryAssetMeta? Resource { get; private set; }

    public void GotResource(RepositoryAssetMeta resource)
    {
        Resource = resource;
    }
}