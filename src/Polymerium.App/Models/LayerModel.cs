using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Extensions;
using System.Threading;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record LayerModel
{
    private CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(App.Current.Token);

    public LayerModel(Metadata.Layer inner, MetadataModel root)
    {
        Inner = inner;
        Root = root;

        RemoveLoaderCommand = new RelayCommand<LoaderModel>(RemoveLoader, CanRemoveLoader);

        Loaders = Inner.Loaders.ToReactiveCollection(x => new LoaderModel(x, RemoveLoaderCommand), x => x.Inner);
        Attachments = Inner.Attachments.ToBindableCollection();

        Summary = inner.ToBindable(x => x.Summary, (x, v) => x.Summary = v);
        IsLocked = this.ToBindable(x => x.Root.Inner.Reference != null && x.Root.Inner.Reference == x.Inner.Source,
            (x, v) =>
            {
                x.Inner.Source = v ? x.Root.Inner.Reference : null;
                RemoveLoaderCommand.NotifyCanExecuteChanged();
            });
    }

    public Metadata.Layer Inner { get; }
    public MetadataModel Root { get; }


    public ReactiveCollection<Loader, LoaderModel> Loaders { get; }
    public BindableCollection<Attachment> Attachments { get; }

    public Bindable<Metadata.Layer, string> Summary { get; }

    public Bindable<LayerModel, bool> IsLocked { get; }
    public CancellationToken Token => tokenSource.Token;

    public IRelayCommand<LoaderModel> RemoveLoaderCommand { get; }

    public void Discard()
    {
        tokenSource.Cancel();
        tokenSource = new CancellationTokenSource();
    }

    private bool CanRemoveLoader(LoaderModel? loader)
    {
        return loader != null && !IsLocked.Value;
    }

    private void RemoveLoader(LoaderModel? loader)
    {
        if (loader != null) Loaders.Remove(loader);
    }
}