using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Dialogs;

public partial class TagsEditorDialog : Dialog
{
    public static readonly DirectProperty<TagsEditorDialog, IReadOnlyList<string>?> InitialTagsProperty =
        AvaloniaProperty.RegisterDirect<TagsEditorDialog, IReadOnlyList<string>?>(
            nameof(InitialTags),
            o => o.InitialTags,
            (o, v) => o.InitialTags = v
        );

    public static readonly DirectProperty<TagsEditorDialog, IReadOnlyList<string>?> SuggestionsProperty =
        AvaloniaProperty.RegisterDirect<TagsEditorDialog, IReadOnlyList<string>?>(
            nameof(Suggestions),
            o => o.Suggestions,
            (o, v) => o.Suggestions = v
        );

    public static readonly DirectProperty<TagsEditorDialog, string> InputTextProperty =
        AvaloniaProperty.RegisterDirect<TagsEditorDialog, string>(
            nameof(InputText),
            o => o.InputText,
            (o, v) => o.InputText = v
        );

    public static readonly DirectProperty<TagsEditorDialog, bool> HasTagsProperty =
        AvaloniaProperty.RegisterDirect<TagsEditorDialog, bool>(nameof(HasTags), o => o.HasTags);

    private IReadOnlyList<string>? _initialTags;
    private IReadOnlyList<string>? _suggestions;
    private string _inputText = string.Empty;

    public TagsEditorDialog()
    {
        InitializeComponent();

        Tags.CollectionChanged += (_, _) =>
        {
            Result = Tags.ToArray();
            RaisePropertyChanged(HasTagsProperty, !HasTags, HasTags);
        };
    }

    public ObservableCollection<string> Tags { get; } = [];

    public required IReadOnlyList<string>? InitialTags
    {
        get => _initialTags;
        set
        {
            SetAndRaise(InitialTagsProperty, ref _initialTags, value);
            Tags.Clear();
            if (value != null)
            {
                foreach (var tag in value)
                {
                    Tags.Add(tag);
                }
            }
        }
    }

    public required IReadOnlyList<string>? Suggestions
    {
        get => _suggestions;
        set => SetAndRaise(SuggestionsProperty, ref _suggestions, value);
    }

    public string InputText
    {
        get => _inputText;
        set => SetAndRaise(InputTextProperty, ref _inputText, value);
    }

    public bool HasTags => Tags.Count > 0;

    [RelayCommand]
    private void AddTag()
    {
        var value = InputText?.Trim();
        if (!string.IsNullOrEmpty(value) && !Tags.Contains(value))
        {
            Tags.Add(value);
        }

        InputText = string.Empty;
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag != null)
        {
            Tags.Remove(tag);
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddTag();
            e.Handled = true;
        }
    }

    protected override bool ValidateResult(object? result) => result is IReadOnlyList<string>;
}
