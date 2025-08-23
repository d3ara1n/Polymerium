using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs
{
    public partial class ReferenceVersionPickerDialog : Dialog
    {
        public static readonly
            DirectProperty<ReferenceVersionPickerDialog, IReadOnlyList<InstanceReferenceVersionModel>?>
            VersionsProperty = AvaloniaProperty
               .RegisterDirect<ReferenceVersionPickerDialog, IReadOnlyList<InstanceReferenceVersionModel>
                    ?>(nameof(Versions), o => o.Versions, (o, v) => o.Versions = v);

        public ReferenceVersionPickerDialog() => InitializeComponent();

        public IReadOnlyList<InstanceReferenceVersionModel>? Versions
        {
            get;
            set => SetAndRaise(VersionsProperty, ref field, value);
        }


        protected override bool ValidateResult(object? result) => result is InstanceReferenceVersionModel;
    }
}
