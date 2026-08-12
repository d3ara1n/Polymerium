using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class CollectionPickerDialog : Dialog
{
    public static readonly DirectProperty<CollectionPickerDialog, IReadOnlyList<CollectionModel>>
        ExistingCollectionsProperty =
        AvaloniaProperty.RegisterDirect<CollectionPickerDialog, IReadOnlyList<CollectionModel>>(
            nameof(ExistingCollections),
            o => o.ExistingCollections, (o, v) => o.ExistingCollections = v);

    public CollectionPickerDialog() => InitializeComponent();

    public required IReadOnlyList<CollectionModel> ExistingCollections
    {
        get;
        set => SetAndRaise(ExistingCollectionsProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => result is CollectionModel;
}
