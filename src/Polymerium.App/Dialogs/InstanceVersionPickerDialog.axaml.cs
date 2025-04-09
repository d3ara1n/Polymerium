using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class InstanceVersionPickerDialog : Dialog
{
    public InstanceVersionPickerDialog() => InitializeComponent();

    public static readonly DirectProperty<InstanceVersionPickerDialog, IReadOnlyList<InstanceReferenceVersionModel>?>
        VersionsProperty =
            AvaloniaProperty
               .RegisterDirect<InstanceVersionPickerDialog,
                    IReadOnlyList<InstanceReferenceVersionModel>?>(nameof(Versions),
                                                                  o => o.Versions,
                                                                  (o, v) => o.Versions = v);

    public IReadOnlyList<InstanceReferenceVersionModel>? Versions
    {
        get;
        set => SetAndRaise(VersionsProperty, ref field, value);
    }


    protected override bool ValidateResult(object? result) => result is InstanceReferenceVersionModel;
}