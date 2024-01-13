using Microsoft.UI.Dispatching;
using Polymerium.Trident.Services;
using Reactive.Bindings;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record WorkpieceModel
{
    public WorkpieceModel(RepositoryService service, DispatcherQueue dispatcher, string key, Profile inner)
    {
        Service = service;
        Dispatcher = dispatcher;
        Key = key;
        Inner = inner;

        Reference = new Bindable<Profile, string?>(Inner, x => x.Reference, (x, v) => x.Reference = v);
        Layers = new BindableCollection<Metadata.Layer>(Inner.Metadata.Layers).ToReadOnlyReactiveCollection(x =>
            new LayerModel(Service, Dispatcher, x, this));
    }

    public RepositoryService Service { get; }
    public DispatcherQueue Dispatcher { get; }
    public string Key { get; }
    public Profile Inner { get; }

    public Bindable<Profile, string?> Reference { get; }
    public ReadOnlyReactiveCollection<LayerModel> Layers { get; }
}