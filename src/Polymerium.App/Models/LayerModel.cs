using Microsoft.UI.Dispatching;
using Polymerium.App.Extensions;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

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
        IsLocked = Root.Reference.Observe(x => x == Inner.Source);
        Loaders = new BindableCollection<Loader>(Inner.Loaders).Observe(x =>
            new LoaderModel(x));
        Attachments = new BindableCollection<string>(Inner.Attachments).Observe(x =>
            new AttachmentModel(Agent, Dispatcher, x, Root));
        EditMode = new Bindable<LayerModel, bool>(this, x => x.editMode, (x, v) => x.editMode = v);
    }

    public RepositoryAgent Agent { get; }
    public DispatcherQueue Dispatcher { get; }
    public Metadata.Layer Inner { get; }
    public WorkpieceModel Root { get; }

    public Bindable<Metadata.Layer, bool> Enabled { get; }
    public Bindable<Metadata.Layer, string> Summary { get; }
    public Reactive<Profile, string?, bool> IsLocked { get; }
    public ReactiveCollection<Loader, LoaderModel> Loaders { get; }
    public ReactiveCollection<string, AttachmentModel> Attachments { get; }
    public Bindable<LayerModel, bool> EditMode { get; }
}