using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Trident.Core.Accounts;
using Trident.Core.Services;
using Trident.Abstractions.Accounts;

namespace Polymerium.App.Components;

public partial class AccountCreationMicrosoft : AccountCreationStep
{
    public static readonly DirectProperty<AccountCreationMicrosoft, MicrosoftUserCodeModel?> ModelProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationMicrosoft, MicrosoftUserCodeModel?>(nameof(Model),
            o => o.Model,
            (o, v) => o.Model = v);

    public static readonly DirectProperty<AccountCreationMicrosoft, string?> ErrorMessageProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationMicrosoft, string?>(nameof(ErrorMessage),
                                                                           o => o.ErrorMessage,
                                                                           (o, v) => o.ErrorMessage = v);

    public static readonly DirectProperty<AccountCreationMicrosoft, IAccount?> AccountProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationMicrosoft, IAccount?>(nameof(Account),
                                                                             o => o.Account,
                                                                             (o, v) => o.Account = v);

    private CancellationTokenSource _cts = new();


    public AccountCreationMicrosoft() => InitializeComponent();

    public IAccount? Account
    {
        get;
        set => SetAndRaise(AccountProperty, ref field, value);
    }

    public MicrosoftUserCodeModel? Model
    {
        get;
        set => SetAndRaise(ModelProperty, ref field, value);
    }

    public string? ErrorMessage
    {
        get;
        set => SetAndRaise(ErrorMessageProperty, ref field, value);
    }


    public required MicrosoftService MicrosoftService { get; init; }
    public required XboxLiveService XboxLiveService { get; init; }
    public required MinecraftService MinecraftService { get; init; }

    public override object NextStep() => new AccountCreationPreview { Account = Account };

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _ = LoadModelAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _cts.Cancel();
    }

    private async Task LoadModelAsync()
    {
        ErrorMessage = null;
        Model = null;
        await _cts.CancelAsync();
        _cts = new();

        try
        {
            var model = await MicrosoftService.AcquireUserCodeAsync();
            Model = new(model.DeviceCode,
                        model.UserCode,
                        model.VerificationUri ?? new Uri("https://aka.ms/devicelogin"));


            var microsoft = await MicrosoftService.AuthenticateAsync(model.DeviceCode, model.Interval, _cts.Token);
            var xbox =
                await XboxLiveService.AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(microsoft.AccessToken);
            var xsts = await XboxLiveService.AuthorizeForServiceTokenByXboxLiveTokenAsync(xbox.Token);
            var minecraft = await MinecraftService.AuthenticateByXboxLiveServiceTokenAsync(xsts.Token,
                                xsts.DisplayClaims.Xui.First().Uhs);
            var profile = await MinecraftService.AcquireAccountProfileByMinecraftTokenAsync(minecraft.AccessToken);
            Account = new MicrosoftAccount
            {
                AccessToken = minecraft.AccessToken,
                RefreshToken = microsoft.RefreshToken,
                Uuid = profile.Id,
                Username = profile.Name
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    #region Commands

    [RelayCommand]
    private void Retry() => _ = LoadModelAsync();

    [RelayCommand]
    private void Copy()
    {
        if (Model is not null)
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Model.UserCode);
        }
    }

    #endregion
}