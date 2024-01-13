using Microsoft.UI.Dispatching;
using Polymerium.Trident.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.TinyLinq;
using Trident.Abstractions;

namespace Polymerium.App.Models
{
    public record LayerModel(RepositoryService Service, DispatcherQueue Dispatcher, Metadata.Layer Inner, WorkpieceModel Root)
    {
        public Bindable<Metadata.Layer, bool> Enabled { get; } = new(Inner, (x) => x.Enabled, (x, v) => x.Enabled = v);
        public Bindable<Metadata.Layer, string> Summary { get; } = new(Inner, (x) => x.Summary, (x, v) => x.Summary = v);

        public ReadOnlyReactivePropertySlim<bool> IsLocked { get; } = Root.Reference.ObserveProperty(x => x.Value).Select(x => x == Inner.Source)
            .ToReadOnlyReactivePropertySlim();

        public ReadOnlyReactiveCollection<LoaderModel> Loaders { get; }
            = new BindableCollection<Metadata.Layer.Loader>(Inner.Loaders).ToReadOnlyReactiveCollection(x => new LoaderModel(x));

        public ReadOnlyReactiveCollection<AttachmentModel> Attachments { get; }
            = new BindableCollection<string>(Inner.Attachments).ToReadOnlyReactiveCollection(x =>
                new AttachmentModel(Service, Dispatcher, x));
    }
}
