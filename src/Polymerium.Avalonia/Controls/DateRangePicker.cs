using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Polymerium.Avalonia.Controls;

public class DateRangePicker : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> StartProperty = AvaloniaProperty.Register<
        DateRangePicker,
        DateTime?
    >(nameof(Start));

    public static readonly StyledProperty<DateTime?> EndProperty = AvaloniaProperty.Register<
        DateRangePicker,
        DateTime?
    >(nameof(End));

    public static readonly StyledProperty<string> StartPlaceholderTextProperty = AvaloniaProperty.Register<
        DateRangePicker,
        string
    >(nameof(StartPlaceholderText), "Start");

    public static readonly StyledProperty<string> EndPlaceholderTextProperty = AvaloniaProperty.Register<
        DateRangePicker,
        string
    >(nameof(EndPlaceholderText), "End");

    public static readonly StyledProperty<DateTime> StartDisplayDateProperty = AvaloniaProperty.Register<
        DateRangePicker,
        DateTime
    >(nameof(StartDisplayDate), DateTime.Today);

    public static readonly StyledProperty<DateTime> EndDisplayDateProperty = AvaloniaProperty.Register<
        DateRangePicker,
        DateTime
    >(nameof(EndDisplayDate), DateTime.Today);

    public DateTime? Start
    {
        get => GetValue(StartProperty);
        set => SetValue(StartProperty, value);
    }

    public DateTime? End
    {
        get => GetValue(EndProperty);
        set => SetValue(EndProperty, value);
    }

    public string StartPlaceholderText
    {
        get => GetValue(StartPlaceholderTextProperty);
        set => SetValue(StartPlaceholderTextProperty, value);
    }

    public string EndPlaceholderText
    {
        get => GetValue(EndPlaceholderTextProperty);
        set => SetValue(EndPlaceholderTextProperty, value);
    }

    public DateTime StartDisplayDate
    {
        get => GetValue(StartDisplayDateProperty);
        private set => SetValue(StartDisplayDateProperty, value);
    }

    public DateTime EndDisplayDate
    {
        get => GetValue(EndDisplayDateProperty);
        private set => SetValue(EndDisplayDateProperty, value);
    }

    private Button? _startButton;
    private Button? _endButton;
    private Popup? _startPopup;
    private Popup? _endPopup;
    private Calendar? _startCalendar;
    private Calendar? _endCalendar;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_startButton != null)
            _startButton.Click -= OnStartButtonClick;
        if (_endButton != null)
            _endButton.Click -= OnEndButtonClick;
        if (_startCalendar != null)
            _startCalendar.SelectedDatesChanged -= OnStartSelectedDatesChanged;
        if (_endCalendar != null)
            _endCalendar.SelectedDatesChanged -= OnEndSelectedDatesChanged;

        _startButton = e.NameScope.Find<Button>("PART_StartButton");
        _endButton = e.NameScope.Find<Button>("PART_EndButton");
        _startPopup = e.NameScope.Find<Popup>("PART_StartPopup");
        _endPopup = e.NameScope.Find<Popup>("PART_EndPopup");
        _startCalendar = e.NameScope.Find<Calendar>("PART_StartCalendar");
        _endCalendar = e.NameScope.Find<Calendar>("PART_EndCalendar");

        if (_startButton != null)
            _startButton.Click += OnStartButtonClick;
        if (_endButton != null)
            _endButton.Click += OnEndButtonClick;
        if (_startCalendar != null)
            _startCalendar.SelectedDatesChanged += OnStartSelectedDatesChanged;
        if (_endCalendar != null)
            _endCalendar.SelectedDatesChanged += OnEndSelectedDatesChanged;

        UpdateDisplayDates();
        UpdateCalendars();
        UpdateRangeState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StartProperty || change.Property == EndProperty)
        {
            UpdateDisplayDates();
            UpdateCalendars();
            UpdateRangeState();
        }
    }

    private void OnStartButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_startPopup != null)
            _startPopup.IsOpen = true;
    }

    private void OnEndButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_endPopup != null)
            _endPopup.IsOpen = true;
    }

    private void OnStartSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        SetCurrentValue(StartProperty, _startCalendar?.SelectedDate);
        if (_startPopup != null)
            _startPopup.IsOpen = false;
    }

    private void OnEndSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        SetCurrentValue(EndProperty, _endCalendar?.SelectedDate);
        if (_endPopup != null)
            _endPopup.IsOpen = false;
    }

    private void UpdateCalendars()
    {
        if (_startCalendar != null && _startCalendar.SelectedDate != Start)
        {
            _startCalendar.DisplayDate = StartDisplayDate;
            _startCalendar.SelectedDate = Start;
        }
        if (_endCalendar != null && _endCalendar.SelectedDate != End)
        {
            _endCalendar.DisplayDate = EndDisplayDate;
            _endCalendar.SelectedDate = End;
        }
    }

    private void UpdateDisplayDates()
    {
        StartDisplayDate = Start ?? End ?? DateTime.Today;
        EndDisplayDate = End ?? Start ?? DateTime.Today;
    }

    private void UpdateRangeState()
    {
        PseudoClasses.Set(":error", Start is { } start && End is { } end && start > end);
    }
}
