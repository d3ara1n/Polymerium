using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class LoaderPickerDialog : Dialog
{
    public LoaderPickerDialog()
    {
        InitializeComponent();
    }

    public static readonly DirectProperty<LoaderPickerDialog, AvaloniaList<LoaderCandidateModel>> CandidatesProperty =
        AvaloniaProperty.RegisterDirect<LoaderPickerDialog, AvaloniaList<LoaderCandidateModel>>(nameof(Candidates),
            o => o.Candidates,
            (o, v) => o.Candidates = v);

    public AvaloniaList<LoaderCandidateModel> Candidates
    {
        get;
        set => SetAndRaise(CandidatesProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => result is LoaderCandidateModel;
}