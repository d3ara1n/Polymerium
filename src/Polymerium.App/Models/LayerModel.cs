using Microsoft.UI.Dispatching;
using Polymerium.Trident.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.TinyLinq;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record LayerModel
{
    private bool editMode;

    public LayerModel(RepositoryAgent agent, DispatcherQueue dispatcher, Metadata.Layer inner,
        WorkpieceModel root)
    {
        Agent = agent;
        Dispatcher = dispatcher;
        Inner = inner;
        Root = root;

        Enabled = new Bindable<Metadata.Layer, bool>(Inner, x => x.Enabled, (x, v) => x.Enabled = v);
        Summary = new Bindable<Metadata.Layer, string>(Inner, x => x.Summary, (x, v) => x.Summary = v);
        IsLocked = Root.Reference.ObserveProperty(x => x.Value).Select(x => x == Inner.Source)
            .ToReadOnlyReactivePropertySlim();
        Loaders = new BindableCollection<Metadata.Layer.Loader>(Inner.Loaders).ToReadOnlyReactiveCollection(x =>
            new LoaderModel(x));
        Attachments = new BindableCollection<string>(Inner.Attachments).ToReadOnlyReactiveCollection(x =>
            new AttachmentModel(Agent, Dispatcher, x, Root));
        EditMode = new Bindable<LayerModel, bool>(this, x => x.editMode, (x, v) => x.editMode = v);
    }

    public RepositoryAgent Agent { get; }
    public DispatcherQueue Dispatcher { get; }
    public Metadata.Layer Inner { get; }
    public WorkpieceModel Root { get; }

    public Bindable<Metadata.Layer, bool> Enabled { get; }
    public Bindable<Metadata.Layer, string> Summary { get; }
    public ReadOnlyReactivePropertySlim<bool> IsLocked { get; }
    public ReadOnlyReactiveCollection<LoaderModel> Loaders { get; }
    public ReadOnlyReactiveCollection<AttachmentModel> Attachments { get; }
    public Bindable<LayerModel, bool> EditMode { get; }
}