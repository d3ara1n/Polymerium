using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":dragover", ":drop")]
public class DropZone : ContentControl
{
    public static readonly RoutedEvent<DragOverEventArgs> DragOverEvent =
        RoutedEvent.Register<DropZone, DragOverEventArgs>(nameof(DragOver), RoutingStrategies.Direct);

    public static readonly RoutedEvent<DropEventArgs> DropEvent =
        RoutedEvent.Register<DropZone, DropEventArgs>(nameof(Drop), RoutingStrategies.Direct);

    public static readonly StyledProperty<object?> ModelProperty = AvaloniaProperty.Register<DropZone, object?>(nameof(Model));

    public object? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    

    public DropZone()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter, handledEventsToo: true);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnDrop, handledEventsToo: true);
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

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Handled = true;
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
        e.Handled = true;
        e.DragEffects = DragDropEffects.None;
        PseudoClasses.Set(":dragover", false);
        PseudoClasses.Set(":drop", Model != null);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
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
                return;
            }
        }

        Model = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModelProperty)
        {
            PseudoClasses.Set(":drop", change.NewValue != null);
        }
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