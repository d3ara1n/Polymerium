using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record MetadataModel
{
    private readonly DialogService _dialog;

    public MetadataModel(string key, Profile inner, DialogService dialog)
    {
        _dialog = dialog;

        Key = key;
        Inner = inner;
        Layers = inner.Metadata.Layers.ToReactiveCollection(
            ToModel, x => x.Inner);


        RenameCommand = new RelayCommand<LayerModel>(RenameLayer, CanRenameLayer);
        UnlockCommand = new RelayCommand<LayerModel>(UnlockLayer, CanUnlockLayer);
        DeleteCommand = new RelayCommand<LayerModel>(DeleteLayer, CanDeleteLayer);
        MoveUpCommand = new RelayCommand<LayerModel>(MoveUpLayer, CanMoveUpLayer);
        MoveDownCommand = new RelayCommand<LayerModel>(MoveDownLayer, CanMoveDownLayer);
        MoveToTopCommand = new RelayCommand<LayerModel>(MoveToTopLayer, CanMoveToTopLayer);
        MoveToBottomCommand = new RelayCommand<LayerModel>(MoveToBottomLayer, CanMoveToBottomLayer);
    }

    public IRelayCommand<LayerModel> RenameCommand { get; }
    public IRelayCommand<LayerModel> UnlockCommand { get; }
    public IRelayCommand<LayerModel> DeleteCommand { get; }
    public IRelayCommand<LayerModel> MoveUpCommand { get; }
    public IRelayCommand<LayerModel> MoveDownCommand { get; }
    public IRelayCommand<LayerModel> MoveToTopCommand { get; }
    public IRelayCommand<LayerModel> MoveToBottomCommand { get; }

    public string Key { get; }
    public Profile Inner { get; }
    public ReactiveCollection<Metadata.Layer, LayerModel> Layers { get; }

    private LayerModel ToModel(Metadata.Layer layer)
    {
        return new LayerModel(layer, this);
    }

    public void AddLayer(Metadata.Layer layer)
    {
        Layers.Add(ToModel(layer));
    }

    private bool CanRenameLayer(LayerModel? layer)
    {
        return layer != null;
    }

    private async void RenameLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var summary = await _dialog.RequestTextAsync("Summarize usage of your new layer", layer.Summary.Value);
            if (summary != null) layer.Summary.Value = summary;
        }
    }

    private bool CanUnlockLayer(LayerModel? layer)
    {
        if (layer != null) return layer.IsLocked.Value;
        return false;
    }

    private async void UnlockLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var confirmation = await _dialog.RequestConfirmationAsync(
                "Unlocking a tagged layer will remove its tag and losing the ability to update metadata. Continue?");
            if (confirmation)
                layer.IsLocked.Value = false;
        }
    }


    private bool CanDeleteLayer(LayerModel? layer)
    {
        return layer != null;
    }

    private async void DeleteLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var confirmation = await _dialog.RequestConfirmationAsync(
                "This operation cannot be revoked. Continue?");
            if (confirmation)
            {
                Layers.Remove(layer);
                NotifyPositionChange();
            }
        }
    }

    private bool CanMoveUpLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var found = Layers.IndexOf(layer);
            return found > 0;
        }

        return false;
    }

    private void MoveUpLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var index = Layers.IndexOf(layer);
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
            var found = Layers.IndexOf(layer);
            return found < Layers.Count - 1;
        }

        return false;
    }

    private void MoveDownLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var index = Layers.IndexOf(layer);
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
            var found = Layers.IndexOf(layer);
            return found != 0;
        }

        return false;
    }

    private void MoveToTopLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var index = Layers.IndexOf(layer);
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
            var found = Layers.IndexOf(layer);
            return found != Layers.Count - 1;
        }

        return false;
    }

    private void MoveToBottomLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var index = Layers.IndexOf(layer);
            if (index != Layers.Count - 1 && Layers.Remove(layer))
            {
                Layers.Add(layer);
                NotifyPositionChange();
            }
        }
    }

    private void NotifyPositionChange()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        MoveToTopCommand.NotifyCanExecuteChanged();
        MoveToBottomCommand.NotifyCanExecuteChanged();
    }
}