using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class LoaderPickerDialog : Dialog
{
    public static readonly DirectProperty<LoaderPickerDialog, IReadOnlyList<LoaderCandidateModel>?> CandidatesProperty =
        AvaloniaProperty.RegisterDirect<LoaderPickerDialog, IReadOnlyList<LoaderCandidateModel>?>(nameof(Candidates),
            o => o.Candidates,
            (o, v) => o.Candidates = v);

    public LoaderPickerDialog()
    {
        InitializeComponent();
    }

    public IReadOnlyList<LoaderCandidateModel>? Candidates
    {
        get;
        set => SetAndRaise(CandidatesProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => result is LoaderCandidateModel;
}