using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class TagPickerDialog : Dialog
{
    public static readonly DirectProperty<TagPickerDialog, IReadOnlyList<string>?> ExistingTagsProperty =
        AvaloniaProperty.RegisterDirect<TagPickerDialog, IReadOnlyList<string>?>(nameof(ExistingTags),
            o => o.ExistingTags,
            (o, v) => o.ExistingTags = v);

    public static readonly DirectProperty<TagPickerDialog, bool> HasExistingTagsProperty =
        AvaloniaProperty.RegisterDirect<TagPickerDialog, bool>(nameof(HasExistingTags), o => o.HasExistingTags);

    public TagPickerDialog()
    {
        InitializeComponent();
        SelectTagCommand = new RelayCommand<string>(SelectTag);
    }

    public IReadOnlyList<string>? ExistingTags
    {
        get;
        set
        {
            SetAndRaise(ExistingTagsProperty, ref field, value);
            RaisePropertyChanged(HasExistingTagsProperty, !HasExistingTags, HasExistingTags);
        }
    }

    public bool HasExistingTags => ExistingTags is { Count: > 0 };

    public ICommand SelectTagCommand { get; }

    private void SelectTag(string? tag)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            Result = tag;
        }
    }

    protected override bool ValidateResult(object? result) => result is string str && !string.IsNullOrEmpty(str);
}
