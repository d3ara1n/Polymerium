using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class RecipePickerDialog : Dialog
{
    public static readonly DirectProperty<RecipePickerDialog, IReadOnlyList<RecipeCardModel>> RecipesSourceProperty =
        AvaloniaProperty.RegisterDirect<RecipePickerDialog, IReadOnlyList<RecipeCardModel>>(nameof(RecipesSource),
            o => o.RecipesSource,
            (o, v) => o.RecipesSource = v);

    public RecipePickerDialog() => InitializeComponent();

    public required IReadOnlyList<RecipeCardModel> RecipesSource
    {
        get;
        set => SetAndRaise(RecipesSourceProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => result is RecipeCardModel;
}
