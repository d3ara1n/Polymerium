using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.Trident.Services;

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

    public AccountCreationMicrosoft()
    {
        InitializeComponent();
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

    public override object NextStep() => throw new NotImplementedException();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _ = LoadModelAsync();
    }

    private async Task LoadModelAsync()
    {
        ErrorMessage = null;
        Model = null;

        try
        {
            var model = await MicrosoftService.AcquireUserCodeAsync();
            Model = new MicrosoftUserCodeModel(model.DeviceCode,
                                               model.UserCode,
                                               model.VerificationUri ?? new Uri("https://aka.ms/devicelogin"));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    #region Commands

    [RelayCommand]
    private void Retry()
    {
        _ = LoadModelAsync();
    }

    [RelayCommand]
    private void Copy()
    {
        if (Model is not null)
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Model.UserCode);
    }

    #endregion
}