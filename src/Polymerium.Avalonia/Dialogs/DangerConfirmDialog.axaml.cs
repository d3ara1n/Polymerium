using System;
using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Dialogs;

public partial class DangerConfirmDialog : Dialog
{
    public DangerConfirmDialog()
    {
        InitializeComponent();
        ChallengeCode = Random.Shared.Next(1000, 10000).ToString();
    }

    // The primary button only lights up once the typed code matches the challenge,
    // because Dialog.CanConfirm delegates to ValidateResult(Result) and TypedCode
    // pushes every keystroke into Result.
    protected override bool ValidateResult(object? result) =>
        result is string s && s == ChallengeCode;

    #region Avalonia Properties

    public static readonly DirectProperty<DangerConfirmDialog, string> ChallengeCodeProperty =
        AvaloniaProperty.RegisterDirect<DangerConfirmDialog, string>(
            nameof(ChallengeCode),
            o => o.ChallengeCode);

    public static readonly DirectProperty<DangerConfirmDialog, string> TypedCodeProperty =
        AvaloniaProperty.RegisterDirect<DangerConfirmDialog, string>(
            nameof(TypedCode),
            o => o.TypedCode,
            (o, v) => o.TypedCode = v);

    #endregion

    #region Direct Properties

    public string ChallengeCode {
        get;
        private init => SetAndRaise(ChallengeCodeProperty, ref field, value);
    }

    public string TypedCode
    {
        get;
        set => SetAndRaise(TypedCodeProperty, ref field, value);
    } = string.Empty;

    #endregion
}
