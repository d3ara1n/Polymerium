using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Extensions;
using System.Windows.Input;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record MetadataModel
    {
        public MetadataModel(string key, Profile inner, ICommand rename, ICommand unlock, ICommand update,
            ICommand delete)
        {
            Key = key;
            Inner = inner;
            Layers = inner.Metadata.Layers.ToReactiveCollection(
                ToModel, x => x.Inner);

            RenameCommand = rename;
            UnlockCommand = unlock;
            UpdateCommand = update;
            DeleteCommand = delete;
            MoveUpCommand = new RelayCommand<LayerModel>(MoveUpLayer, CanMoveUpLayer);
            MoveDownCommand = new RelayCommand<LayerModel>(MoveDownLayer, CanMoveDownLayer);
            MoveToTopCommand = new RelayCommand<LayerModel>(MoveToTopLayer, CanMoveToTopLayer);
            MoveToBottomCommand = new RelayCommand<LayerModel>(MoveToBottomLayer, CanMoveToBottomLayer);
        }

        public ICommand RenameCommand { get; }
        public ICommand UnlockCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public IRelayCommand<LayerModel> MoveUpCommand { get; }
        public IRelayCommand<LayerModel> MoveDownCommand { get; }
        public IRelayCommand<LayerModel> MoveToTopCommand { get; }
        public IRelayCommand<LayerModel> MoveToBottomCommand { get; }

        public string Key { get; }
        public Profile Inner { get; }
        public ReactiveCollection<Metadata.Layer, LayerModel> Layers { get; }

        public string ModLoaderLabel
        {
            get
            {
                string? loader = Inner.Metadata.ExtractModLoader();
                if (loader != null)
                {
                    return Loader.MODLOADER_NAME_MAPPINGS[loader];
                }

                return "Vanilla";
            }
        }

        private LayerModel ToModel(Metadata.Layer layer)
        {
            return new LayerModel(layer, this);
        }

        public void AddLayer(Metadata.Layer layer)
        {
            Layers.Add(ToModel(layer));
        }

        private bool CanMoveUpLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int found = Layers.IndexOf(layer);
                return found > 0;
            }

            return false;
        }

        private void MoveUpLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int index = Layers.IndexOf(layer);
                if (index > 0 && Layers.Remove(layer))
                {
                    Layers.Insert(index - 1, layer);
                    NotifyPositionChange();
                }
            }
        }

        private bool CanMoveDownLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int found = Layers.IndexOf(layer);
                return found < Layers.Count - 1;
            }

            return false;
        }

        private void MoveDownLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int index = Layers.IndexOf(layer);
                if (index < Layers.Count - 1 && Layers.Remove(layer))
                {
                    Layers.Insert(index + 1, layer);
                    NotifyPositionChange();
                }
            }
        }

        private bool CanMoveToTopLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int found = Layers.IndexOf(layer);
                return found != 0;
            }

            return false;
        }

        private void MoveToTopLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int index = Layers.IndexOf(layer);
                if (index > 0 && Layers.Remove(layer))
                {
                    Layers.Insert(0, layer);
                    NotifyPositionChange();
                }
            }
        }

        private bool CanMoveToBottomLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int found = Layers.IndexOf(layer);
                return found != Layers.Count - 1;
            }

            return false;
        }

        private void MoveToBottomLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                int index = Layers.IndexOf(layer);
                if (index != Layers.Count - 1 && Layers.Remove(layer))
                {
                    Layers.Add(layer);
                    NotifyPositionChange();
                }
            }
        }

        public void NotifyPositionChange()
        {
            MoveUpCommand.NotifyCanExecuteChanged();
            MoveDownCommand.NotifyCanExecuteChanged();
            MoveToTopCommand.NotifyCanExecuteChanged();
            MoveToBottomCommand.NotifyCanExecuteChanged();
        }
    }
}