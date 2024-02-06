using Polymerium.App.Extensions;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record LayerModel
{
    public LayerModel(Metadata.Layer inner)
    {
        Inner = inner;
        LoadersRaw = Inner.Loaders.ToBindableCollection();
        Loaders = LoadersRaw.ToReactiveCollection(x => new LoaderModel(x), x => x.Inner);
    }

    public Metadata.Layer Inner { get; }

    public BindableCollection<Loader> LoadersRaw { get; }
    public ReactiveCollection<Loader, LoaderModel> Loaders { get; }
}