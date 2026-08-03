using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class RecipeEditorDialog : Dialog
{
    public static readonly StyledProperty<string> RecipeNameProperty =
        AvaloniaProperty.Register<RecipeEditorDialog, string>(nameof(RecipeName));

    public static readonly StyledProperty<string?> RecipeDescriptionProperty =
        AvaloniaProperty.Register<RecipeEditorDialog, string?>(nameof(RecipeDescription));

    public RecipeEditorDialog() => InitializeComponent();

    public string RecipeName
    {
        get => GetValue(RecipeNameProperty);
        set => SetValue(RecipeNameProperty, value);
    }

    public string? RecipeDescription
    {
        get => GetValue(RecipeDescriptionProperty);
        set => SetValue(RecipeDescriptionProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RecipeNameProperty || change.Property == RecipeDescriptionProperty)
        {
            Result = new RecipeEditorResultModel(RecipeName, RecipeDescription);
        }
    }

    protected override bool ValidateResult(object? result) =>
        result is RecipeEditorResultModel r && !string.IsNullOrWhiteSpace(r.Name);
}
