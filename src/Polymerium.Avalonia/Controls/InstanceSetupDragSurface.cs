using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentIcons.Common;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Controls;

[TemplatePart(PART_TargetItems, typeof(ItemsControl))]
[TemplatePart(PART_TargetScrollViewer, typeof(ScrollViewer))]
[TemplatePart(PART_DragPreview, typeof(Border))]
public class InstanceSetupDragSurface : ContentControl
{
    public const string PART_TargetItems = nameof(PART_TargetItems);
    public const string PART_TargetScrollViewer = nameof(PART_TargetScrollViewer);
    public const string PART_DragPreview = nameof(PART_DragPreview);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<InstanceSetupDragSurface, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ICommand?> ReorderGroupsCommandProperty =
        AvaloniaProperty.Register<InstanceSetupDragSurface, ICommand?>(nameof(ReorderGroupsCommand));

    public static readonly StyledProperty<ICommand?> MovePackageToCollectionCommandProperty =
        AvaloniaProperty.Register<InstanceSetupDragSurface, ICommand?>(nameof(MovePackageToCollectionCommand));

    public static readonly AttachedProperty<object?> DragItemProperty =
        AvaloniaProperty.RegisterAttached<InstanceSetupDragSurface, Control, object?>("DragItem");

    public static readonly DirectProperty<InstanceSetupDragSurface, DragMode> ModeProperty =
        AvaloniaProperty.RegisterDirect<InstanceSetupDragSurface, DragMode>(nameof(Mode), o => o.Mode);

    public static readonly DirectProperty<InstanceSetupDragSurface, string?> DragLabelProperty =
        AvaloniaProperty.RegisterDirect<InstanceSetupDragSurface, string?>(nameof(DragLabel), o => o.DragLabel);

    public static readonly DirectProperty<InstanceSetupDragSurface, Symbol> DragIconProperty =
        AvaloniaProperty.RegisterDirect<InstanceSetupDragSurface, Symbol>(nameof(DragIcon), o => o.DragIcon);

    private const double DRAG_THRESHOLD = 6;
    private const double AUTO_SCROLL_EDGE = 40;
    private const double AUTO_SCROLL_STEP = 8;

    private readonly DispatcherTimer _autoScrollTimer;
    private object? _armedItem;
    private Border? _dragPreview;
    private GroupModel? _draggedGroup;
    private InstancePackageModel? _draggedPackage;
    private TargetItem? _draggedTarget;
    private TargetItem? _hoveredTarget;
    private Point _lastPoint;
    private Point _pressPoint;
    private IPointer? _pointer;
    private double _scrollDirection;
    private ItemsControl? _targetItems;
    private ScrollViewer? _targetScrollViewer;

    public InstanceSetupDragSurface()
    {
        _autoScrollTimer = new() { Interval = TimeSpan.FromMilliseconds(52) };
        _autoScrollTimer.Tick += OnAutoScrollTick;

        AddHandler(PointerPressedEvent, OnDragPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, OnDragPointerMoved, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, OnDragPointerReleased, RoutingStrategies.Tunnel, true);
        AddHandler(KeyDownEvent, OnDragKeyDown, RoutingStrategies.Tunnel, true);
    }

    public enum DragMode
    {
        None,
        Group,
        Package
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? ReorderGroupsCommand
    {
        get => GetValue(ReorderGroupsCommandProperty);
        set => SetValue(ReorderGroupsCommandProperty, value);
    }

    public ICommand? MovePackageToCollectionCommand
    {
        get => GetValue(MovePackageToCollectionCommandProperty);
        set => SetValue(MovePackageToCollectionCommandProperty, value);
    }

    public DragMode Mode
    {
        get;
        private set => SetAndRaise(ModeProperty, ref field, value);
    }

    public string? DragLabel
    {
        get;
        private set => SetAndRaise(DragLabelProperty, ref field, value);
    }

    public Symbol DragIcon
    {
        get;
        private set => SetAndRaise(DragIconProperty, ref field, value);
    } = Symbol.ArrowMove;

    public ObservableCollection<TargetItem> Targets { get; } = [];

    public static object? GetDragItem(Control control) => control.GetValue(DragItemProperty);

    public static void SetDragItem(Control control, object? value) => control.SetValue(DragItemProperty, value);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _targetItems = e.NameScope.Find<ItemsControl>(PART_TargetItems);
        _targetScrollViewer = e.NameScope.Find<ScrollViewer>(PART_TargetScrollViewer);
        _dragPreview = e.NameScope.Find<Border>(PART_DragPreview);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        CancelDrag(false);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelDrag();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Mode != DragMode.None
         || !e.Pointer.IsPrimary
         || e.Pointer.Type != PointerType.Mouse
         || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
         || FindDragSource(e.Source as Visual) is not { } item)
        {
            return;
        }

        _pointer = e.Pointer;
        _armedItem = item;
        _pressPoint = e.GetPosition(this);
        _lastPoint = _pressPoint;
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _pointer))
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Mode == DragMode.None)
        {
            if (_armedItem is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                ResetArmed();
                return;
            }

            var delta = point - _pressPoint;
            if (Math.Abs(delta.X) < DRAG_THRESHOLD && Math.Abs(delta.Y) < DRAG_THRESHOLD)
            {
                return;
            }

            if (!BeginDrag(_armedItem, e.Pointer))
            {
                ResetArmed();
                return;
            }

            e.PreventGestureRecognition();
        }

        _lastPoint = point;
        UpdateDrag(point);
        e.Handled = true;
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _pointer))
        {
            return;
        }

        if (Mode == DragMode.None)
        {
            ResetArmed();
            return;
        }

        _lastPoint = e.GetPosition(this);
        UpdateDrag(_lastPoint);

        object? request = Mode switch
        {
            DragMode.Group when _draggedGroup is not null && HitTarget(_lastPoint) is not null =>
                new GroupReorderRequest([.. Targets.Select(x => x.Group.Source!)]),
            DragMode.Package when _draggedPackage is not null && _hoveredTarget is not null =>
                new PackageCollectionMoveRequest(_draggedPackage.Entry, _hoveredTarget.Group.Source!),
            _ => null
        };

        var command = Mode == DragMode.Group ? ReorderGroupsCommand : MovePackageToCollectionCommand;
        CancelDrag();
        if (request is not null && command?.CanExecute(request) == true)
        {
            command.Execute(request);
        }

        e.Handled = true;
    }

    private void OnDragKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || Mode == DragMode.None)
        {
            return;
        }

        CancelDrag();
        e.Handled = true;
    }

    private object? FindDragSource(Visual? origin)
    {
        var nestedButton = false;
        for (var current = origin; current is not null && !ReferenceEquals(current, this); current = current.GetVisualParent())
        {
            if (current is not Control control)
            {
                continue;
            }

            if (GetDragItem(control) is { } item)
            {
                return nestedButton ? null : item;
            }

            nestedButton |= control is Button;
        }

        return null;
    }

    private bool BeginDrag(object item, IPointer pointer)
    {
        var groups = GetOrderedGroups();
        Targets.Clear();

        switch (item)
        {
            case GroupModel { Source: not null } group when groups.Count > 1:
                _draggedGroup = group;
                foreach (var candidate in groups)
                {
                    var target = TargetItem.From(candidate);
                    target.IsDragged = ReferenceEquals(candidate, group);
                    Targets.Add(target);
                }

                _draggedTarget = Targets.FirstOrDefault(x => x.IsDragged);
                Mode = DragMode.Group;
                DragLabel = LabelOf(group);
                DragIcon = IconOf(group);
                break;

            case InstancePackageModel { CanMoveToCollection: true } package:
                var targets = groups
                             .Where(x => x.Kind == PackageSourceHelper.Kind.Collection
                                      && x.Source != package.Entry.Source)
                             .ToList();
                if (targets.Count == 0)
                {
                    return false;
                }

                _draggedPackage = package;
                foreach (var candidate in targets)
                {
                    Targets.Add(TargetItem.From(candidate));
                }

                Mode = DragMode.Package;
                DragLabel = package.Info?.ProjectName ?? package.Entry.Pref;
                DragIcon = Symbol.CollectionsAdd;
                break;

            default:
                return false;
        }

        _armedItem = null;
        pointer.Capture(this);
        Focus();
        return true;
    }

    private void UpdateDrag(Point point)
    {
        UpdatePreviewPosition(point);
        UpdateAutoScroll(point);

        var target = HitTarget(point);
        if (Mode == DragMode.Group)
        {
            if (target is not null && _draggedTarget is not null && !ReferenceEquals(target, _draggedTarget))
            {
                var oldIndex = Targets.IndexOf(_draggedTarget);
                var newIndex = Targets.IndexOf(target);
                if (oldIndex >= 0 && newIndex >= 0)
                {
                    Targets.Move(oldIndex, newIndex);
                }
            }

            return;
        }

        if (!ReferenceEquals(target, _hoveredTarget))
        {
            if (_hoveredTarget is not null)
            {
                _hoveredTarget.IsHovered = false;
            }

            _hoveredTarget = target;
            if (_hoveredTarget is not null)
            {
                _hoveredTarget.IsHovered = true;
            }
        }
    }

    private void UpdatePreviewPosition(Point point)
    {
        if (_dragPreview is null)
        {
            return;
        }

        const double offset = 12;
        var width = _dragPreview.Bounds.Width > 0 ? _dragPreview.Bounds.Width : 240;
        var height = _dragPreview.Bounds.Height > 0 ? _dragPreview.Bounds.Height : 48;
        var left = point.X + offset;
        if (left + width > Bounds.Width - 16)
        {
            left = point.X - width - offset;
        }

        Canvas.SetLeft(_dragPreview, Math.Clamp(left, 0, Math.Max(0, Bounds.Width - width)));
        Canvas.SetTop(_dragPreview,
                      Math.Clamp(point.Y + offset, 0, Math.Max(0, Bounds.Height - height)));
    }

    private void UpdateAutoScroll(Point point)
    {
        _scrollDirection = 0;
        if (_targetScrollViewer is null
         || this.TranslatePoint(point, _targetScrollViewer) is not { } relative
         || relative.X < 0
         || relative.X > _targetScrollViewer.Bounds.Width
         || relative.Y < 0
         || relative.Y > _targetScrollViewer.Bounds.Height)
        {
            _autoScrollTimer.Stop();
            return;
        }

        if (relative.Y < AUTO_SCROLL_EDGE)
        {
            _scrollDirection = -1;
        }
        else if (relative.Y > _targetScrollViewer.Bounds.Height - AUTO_SCROLL_EDGE)
        {
            _scrollDirection = 1;
        }

        if (_scrollDirection == 0)
        {
            _autoScrollTimer.Stop();
        }
        else
        {
            _autoScrollTimer.Start();
        }
    }

    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (_targetScrollViewer is null || Mode == DragMode.None || _scrollDirection == 0)
        {
            _autoScrollTimer.Stop();
            return;
        }

        var offset = _targetScrollViewer.Offset;
        _targetScrollViewer.Offset = new(offset.X, offset.Y + _scrollDirection * AUTO_SCROLL_STEP);
        if (_targetScrollViewer.Offset == offset)
        {
            _scrollDirection = 0;
            _autoScrollTimer.Stop();
            return;
        }

        UpdateDrag(_lastPoint);
    }

    private TargetItem? HitTarget(Point point)
    {
        if (_targetItems is null
         || _targetScrollViewer is null
         || this.TranslatePoint(point, _targetScrollViewer) is not { } viewportPoint
         || viewportPoint.X < 0
         || viewportPoint.X > _targetScrollViewer.Bounds.Width
         || viewportPoint.Y < 0
         || viewportPoint.Y > _targetScrollViewer.Bounds.Height)
        {
            return null;
        }

        foreach (var presenter in _targetItems.GetVisualDescendants().OfType<ContentPresenter>())
        {
            if (presenter.DataContext is not TargetItem target
             || presenter.TranslatePoint(default, this) is not { } origin)
            {
                continue;
            }

            if (new Rect(origin, presenter.Bounds.Size).Contains(point))
            {
                return target;
            }
        }

        return null;
    }

    private ObservableCollection<GroupModel> GetOrderedGroups()
    {
        var groups = ItemsSource?
                    .Cast<object>()
                    .OfType<PackageListItemBase.Header>()
                    .Select(x => x.Group)
                    .Distinct()
                    .ToList() ?? [];
        return [.. groups];
    }

    private void ResetArmed()
    {
        _armedItem = null;
        _pointer = null;
    }

    private void CancelDrag(bool releasePointer = true)
    {
        var pointer = _pointer;
        _autoScrollTimer.Stop();
        _scrollDirection = 0;

        if (_hoveredTarget is not null)
        {
            _hoveredTarget.IsHovered = false;
        }

        _armedItem = null;
        _draggedGroup = null;
        _draggedPackage = null;
        _draggedTarget = null;
        _hoveredTarget = null;
        _pointer = null;
        DragLabel = null;
        Mode = DragMode.None;
        Targets.Clear();

        if (releasePointer && pointer?.Captured == this)
        {
            pointer.Capture(null);
        }
    }

    private static string LabelOf(GroupModel group) => group.Info switch
    {
        ModpackGroupInfoModel modpack => modpack.Name,
        RecipeGroupInfoModel recipe => recipe.Name,
        CollectionGroupInfoModel collection => collection.Name,
        _ => group.Source ?? string.Empty
    };

    private static Symbol IconOf(GroupModel group) => group.Kind switch
    {
        PackageSourceHelper.Kind.Collection => Symbol.Collections,
        PackageSourceHelper.Kind.Recipe => Symbol.ListBar,
        _ => Symbol.Box
    };

    public sealed class TargetItem(GroupModel group, string label, Symbol icon) : INotifyPropertyChanged
    {
        public GroupModel Group => group;
        public string Label => label;
        public Symbol Icon => icon;

        public bool IsDragged
        {
            get;
            internal set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                PropertyChanged?.Invoke(this, new(nameof(IsDragged)));
            }
        }

        public bool IsHovered
        {
            get;
            internal set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                PropertyChanged?.Invoke(this, new(nameof(IsHovered)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        internal static TargetItem From(GroupModel group) => new(group, LabelOf(group), IconOf(group));
    }
}
