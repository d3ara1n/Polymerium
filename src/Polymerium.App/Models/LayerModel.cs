using System.Threading;
using Polymerium.App.Extensions;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record LayerModel
{
    private CancellationTokenSource tokenSource = new();

    public LayerModel(Metadata.Layer inner, MetadataModel root)
    {
        Inner = inner;
        Root = root;
        Loaders = Inner.Loaders.ToReactiveCollection(x => new LoaderModel(x), x => x.Inner);
        Attachments = Inner.Attachments.ToBindableCollection();

        Summary = inner.ToBindable(x => x.Summary, (x, v) => x.Summary = v);
        IsLocked = this.ToBindable(x => x.Root.Inner.Reference != null && x.Root.Inner.Reference == x.Inner.Source,
            (x, v) => x.Inner.Source = v ? x.Root.Inner.Reference : null);
    }

    public Metadata.Layer Inner { get; }
    public MetadataModel Root { get; }


    public ReactiveCollection<Loader, LoaderModel> Loaders { get; }
    public BindableCollection<string> Attachments { get; }

    public Bindable<Metadata.Layer, string> Summary { get; }

    public Bindable<LayerModel, bool> IsLocked { get; }
    public CancellationToken Token => tokenSource.Token;

    public void Discard()
    {
        tokenSource.Cancel();
        tokenSource = new CancellationTokenSource();
    }
}