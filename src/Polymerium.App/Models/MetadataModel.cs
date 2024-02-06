using Polymerium.App.Extensions;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record MetadataModel
{
    public MetadataModel(string key, Profile inner)
    {
        Key = key;
        Inner = inner;
        LayersRaw = inner.Metadata.Layers.ToBindableCollection();
        Layers = LayersRaw.ToReactiveCollection(x => new LayerModel(x), x => x.Inner);
    }

    public string Key { get; }
    public Profile Inner { get; }
    public BindableCollection<Metadata.Layer> LayersRaw { get; }
    public ReactiveCollection<Metadata.Layer, LayerModel> Layers { get; }
}