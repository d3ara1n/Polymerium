using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":dragover", ":drop")]
public class DropZone : ContentControl
{
    public static readonly RoutedEvent<DragOverEventArgs> DragOverEvent = RoutedEvent.Register<DropZone, DragOverEventArgs>(nameof(DragOver), RoutingStrategies.Direct);
    public static readonly RoutedEvent<DropEventArgs> DropEvent = RoutedEvent.Register<DropZone, DropEventArgs>(nameof(Drop), RoutingStrategies.Direct);
    public static readonly DirectProperty<DropZone, object?> ModelProperty = AvaloniaProperty.RegisterDirect<DropZone, object?>(nameof(Model), o => o.Model, (o, v) => o.Model = v);

    private object? _model;

    public object? Model
    {
        get => _model;
        set => SetAndRaise(ModelProperty, ref _model, value);
    }


    public event EventHandler<DragOverEventArgs> DragOver
    {
        add => AddHandler(DragOverEvent, value);
        remove => RemoveHandler(DragOverEvent, value);
    }

    public event EventHandler<DropEventArgs> Drop
    {
        add => AddHandler(DragOverEvent, value);
        remove => RemoveHandler(DragOverEvent, value);
    }

    public DropZone()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter, handledEventsToo: true);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnDrop, handledEventsToo: true);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var args = new DragOverEventArgs(e.Data) { RoutedEvent = DragOverEvent };
        RaiseEvent(args);
        PseudoClasses.Set(":drop", false);
        if (args.Accepted)
        {
            e.DragEffects = DragDropEffects.Copy;
            PseudoClasses.Set(":dragover", true);
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            PseudoClasses.Set(":dragover", false);
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;
        PseudoClasses.Set(":dragover", false);
        PseudoClasses.Set(":drop", false);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;
        PseudoClasses.Set(":dragover", false);
        var validation = new DragOverEventArgs(e.Data) { RoutedEvent = DragOverEvent };
        RaiseEvent(validation);
        if (validation.Accepted)
        {
            var args = new DropEventArgs(e.Data) { RoutedEvent = DropEvent };
            RaiseEvent(args);
            if (args.Model != null)
            {
                Model = args.Model;
                PseudoClasses.Set(":drop", true);
                return;
            }
        }

        Model = null;
        PseudoClasses.Set(":drop", false);
    }

    public class DragOverEventArgs(IDataObject data) : RoutedEventArgs
    {
        public IDataObject Data => data;
        public bool Accepted { get; set; }
    }

    public class DropEventArgs(IDataObject data) : RoutedEventArgs
    {
        public IDataObject Data => data;
        public object? Model { get; set; }
    }
}