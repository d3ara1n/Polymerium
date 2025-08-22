using System;
using Avalonia;
using Polymerium.App.Controls;
using Polymerium.Trident.Services;

namespace Polymerium.App.Components;

public partial class AccountCreationPortal : AccountCreationStep
{
    public static readonly DirectProperty<AccountCreationPortal, bool> IsOfflineAvailableProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationPortal, bool>(nameof(IsOfflineAvailable),
            o => o.IsOfflineAvailable,
            (o, v) => o.IsOfflineAvailable = v);

    public AccountCreationPortal() => InitializeComponent();

    public required MicrosoftService MicrosoftService { get; init; }
    public required XboxLiveService XboxLiveService { get; init; }
    public required MinecraftService MinecraftService { get; init; }

    public bool IsOfflineAvailable
    {
        get;
        set => SetAndRaise(IsOfflineAvailableProperty, ref field, value);
    }

    public override object NextStep() =>
        AccountTypeSelectBox.SelectedIndex switch
        {
            0 => new AccountCreationMicrosoft
            {
                MicrosoftService = MicrosoftService,
                XboxLiveService = XboxLiveService,
                MinecraftService = MinecraftService
            },
            1 => new AccountCreationTrial(),
            2 => new AccountCreationOffline(),
            _ => throw new ArgumentOutOfRangeException()
        };
}