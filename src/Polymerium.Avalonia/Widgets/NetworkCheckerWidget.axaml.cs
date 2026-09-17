using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Widgets;

public partial class NetworkCheckerWidget : WidgetBase
{
    private readonly IReadOnlyList<(string Display, string Url)> _websites = BuildWebsites();

    private CancellationTokenSource? _cts;

    public NetworkCheckerWidget() => AvaloniaXamlLoader.Load(this);

    private static IReadOnlyList<(string Display, string Url)> BuildWebsites() =>
    [
        (LanguageManager.Instance.NetworkCheckerWidget_SiteMicrosoftLoginText.Current(),
         "https://login.microsoftonline.com"),
        (LanguageManager.Instance.NetworkCheckerWidget_SiteXboxLiveText.Current(), "https://user.auth.xboxlive.com"),
        (LanguageManager.Instance.NetworkCheckerWidget_SiteGithubAssetsText.Current(),
         "https://release-assets.githubusercontent.com"),
        (LanguageManager.Instance.NetworkCheckerWidget_SiteMojangMetaText.Current(),
         "https://launchermeta.mojang.com"),
        (LanguageManager.Instance.NetworkCheckerWidget_SiteCommunityMetaText.Current(),
         "https://meta.prismlauncher.org"),
        ("CurseForge API", "https://api.curseforge.com"),
        ("Modrinth API", "https://api.modrinth.com")
    ];

    #region Direct

    public ObservableCollection<ConnectionTestSiteModel> Sites { get; } = [];

    #endregion

    protected override Task OnInitializeAsync()
    {
        Title = LanguageManager.Instance.NetworkCheckerWidget_Title.Current();
        ButtonText = LanguageManager.Instance.NetworkCheckerWidget_StartButtonText.Current();

        _cts = new();
        foreach (var (display, url) in _websites)
        {
            Sites.Add(new(display, new(url)));
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        return Task.CompletedTask;
    }

    #region Reactive

    public static readonly DirectProperty<NetworkCheckerWidget, bool> IsTestingProperty =
        AvaloniaProperty.RegisterDirect<NetworkCheckerWidget, bool>(nameof(IsTesting),
                                                                    o => o.IsTesting,
                                                                    (o, v) => o.IsTesting = v);

    public static readonly DirectProperty<NetworkCheckerWidget, bool> HasTestedProperty =
        AvaloniaProperty.RegisterDirect<NetworkCheckerWidget, bool>(nameof(HasTested),
                                                                    o => o.HasTested,
                                                                    (o, v) => o.HasTested = v);

    public static readonly DirectProperty<NetworkCheckerWidget, string> ButtonTextProperty =
        AvaloniaProperty.RegisterDirect<NetworkCheckerWidget, string>(nameof(ButtonText),
                                                                      o => o.ButtonText,
                                                                      (o, v) => o.ButtonText = v);

    public bool IsTesting
    {
        get;
        set => SetAndRaise(IsTestingProperty, ref field, value);
    }

    public bool HasTested
    {
        get;
        set => SetAndRaise(HasTestedProperty, ref field, value);
    }

    public string ButtonText
    {
        get;
        set => SetAndRaise(ButtonTextProperty, ref field, value);
    } = string.Empty;

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanPerform))]
    private async Task PerformAsync()
    {
        if (IsTesting)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new();
            return;
        }

        IsTesting = true;
        HasTested = false;
        ButtonText = LanguageManager.Instance.NetworkCheckerWidget_CancelButtonText.Current();

        try
        {
            NetworkCheckHelper.ResetAll(Sites);

            var httpClientFactory = Program.Services?.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();

            await NetworkCheckHelper.TestConnectionsAsync(Sites, httpClient, _cts?.Token ?? CancellationToken.None);

            HasTested = true;
            ButtonText = LanguageManager.Instance.NetworkCheckerWidget_RetryButtonText.Current();
        }
        catch (OperationCanceledException)
        {
            ButtonText = LanguageManager.Instance.NetworkCheckerWidget_StartButtonText.Current();
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool CanPerform() => true;

    #endregion
}
