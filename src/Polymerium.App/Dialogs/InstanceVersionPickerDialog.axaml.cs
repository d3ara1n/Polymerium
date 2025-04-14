using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class InstanceVersionPickerDialog : Dialog
{
    public static readonly DirectProperty<InstanceVersionPickerDialog, IReadOnlyList<InstanceReferenceVersionModel>?>
        VersionsProperty =
            AvaloniaProperty
               .RegisterDirect<InstanceVersionPickerDialog, IReadOnlyList<InstanceReferenceVersionModel>
                    ?>(nameof(Versions), o => o.Versions, (o, v) => o.Versions = v);

    public InstanceVersionPickerDialog() => InitializeComponent();

    public IReadOnlyList<InstanceReferenceVersionModel>? Versions
    {
        get;
        set => SetAndRaise(VersionsProperty, ref field, value);
    }


    protected override bool ValidateResult(object? result) => result is InstanceReferenceVersionModel;
}