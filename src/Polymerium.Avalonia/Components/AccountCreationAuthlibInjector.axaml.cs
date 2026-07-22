using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Controls;
using TridentCore.Abstractions.Accounts;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.Components;

public partial class AccountCreationAuthlibInjector : AccountCreationStep
{
    private const string AUTHLIB_INJECTOR_PREFIX = "authlib-injector:yggdrasil-server:";

    public static readonly DirectProperty<AccountCreationAuthlibInjector, string> ServerUrlProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, string>(nameof(ServerUrl),
                                                                                    o => o.ServerUrl,
                                                                                    (o, v) => o.ServerUrl = v);

    public static readonly DirectProperty<AccountCreationAuthlibInjector, string> UsernameProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, string>(nameof(Username),
                                                                                    o => o.Username,
                                                                                    (o, v) => o.Username = v);

    public static readonly DirectProperty<AccountCreationAuthlibInjector, string> PasswordProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, string>(nameof(Password),
                                                                                    o => o.Password,
                                                                                    (o, v) => o.Password = v);

    public static readonly DirectProperty<AccountCreationAuthlibInjector, string?> ErrorMessageProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, string?>(nameof(ErrorMessage),
            o => o.ErrorMessage,
            (o, v) => o.ErrorMessage = v);

    public static readonly DirectProperty<AccountCreationAuthlibInjector, IAccount?> AccountProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, IAccount?>(nameof(Account),
            o => o.Account,
            (o, v) => o.Account = v);

    public static readonly DirectProperty<AccountCreationAuthlibInjector, string?> SuccessMessageProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationAuthlibInjector, string?>(nameof(SuccessMessage),
            o => o.SuccessMessage,
            (o, v) => o.SuccessMessage = v);

    private CancellationTokenSource _cts = new();

    public AccountCreationAuthlibInjector() => InitializeComponent();

    public string ServerUrl
    {
        get;
        set => SetAndRaise(ServerUrlProperty, ref field, value);
    } = string.Empty;

    public string Username
    {
        get;
        set => SetAndRaise(UsernameProperty, ref field, value);
    } = string.Empty;

    public string Password
    {
        get;
        set => SetAndRaise(PasswordProperty, ref field, value);
    } = string.Empty;

    public string? ErrorMessage
    {
        get;
        set => SetAndRaise(ErrorMessageProperty, ref field, value);
    }

    public IAccount? Account
    {
        get;
        set => SetAndRaise(AccountProperty, ref field, value);
    }

    public string? SuccessMessage
    {
        get;
        set => SetAndRaise(SuccessMessageProperty, ref field, value);
    }

    public required YggdrasilService YggdrasilService { get; init; }

    public override object NextStep()
    {
        if (Account != null)
        {
            return new AccountCreationPreview { Account = Account };
        }

        throw new InvalidOperationException("Authenticate first before proceeding");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _cts.Cancel();
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var old = _cts;
        _cts = new();
        await old.CancelAsync();
        old.Dispose();

        try
        {
            var result = await YggdrasilService.AuthenticateAsync(ServerUrl, Username, Password, _cts.Token);

            Account = result.Account;
            SuccessMessage = Properties.Resources.AccountCreationAuthlib_DoneSubtitle;
            IsNextAvailable = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void SetServerUrl(string url) => ServerUrl = url;

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
        if (e.Data.Contains(DataFormat.Text))
        {
            e.Accepted = true;
        }
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
        var raw = e.Data.TryGetText()?.Trim();
        if (raw is null)
        {
            return;
        }

        string? url = null;
        if (raw.StartsWith(AUTHLIB_INJECTOR_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            url = Uri.UnescapeDataString(raw[AUTHLIB_INJECTOR_PREFIX.Length..]);
        }
        else if (Uri.TryCreate(raw, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https")
        {
            url = raw;
        }

        if (!string.IsNullOrEmpty(url))
        {
            ServerUrl = url;
        }
    }
}
