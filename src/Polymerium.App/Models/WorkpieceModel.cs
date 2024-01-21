using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Extensions;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record WorkpieceModel
{
    public WorkpieceModel(RepositoryAgent agent, DispatcherQueue dispatcher, string key, Profile inner,
        ICommand fetchAttachmentCommand)
    {
        Agent = agent;
        Dispatcher = dispatcher;
        Key = key;
        Inner = inner;
        FetchAttachmentCommand = fetchAttachmentCommand;

        Reference = new Bindable<Profile, string?>(Inner, x => x.Reference, (x, v) => x.Reference = v);
        Layers = new BindableCollection<Metadata.Layer>(Inner.Metadata.Layers).Observe(x =>
            new LayerModel(Agent, Dispatcher, x, this));
    }

    public RepositoryAgent Agent { get; }
    public DispatcherQueue Dispatcher { get; }
    public string Key { get; }
    public Profile Inner { get; }
    public ICommand FetchAttachmentCommand { get; }

    public Bindable<Profile, string?> Reference { get; }
    public ReactiveCollection<Metadata.Layer, LayerModel> Layers { get; }
}