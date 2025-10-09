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
using Polymerium.App.Models;
using Polymerium.App.Utilities;

namespace Polymerium.App.Widgets;

public partial class NetworkCheckerWidget : WidgetBase
{
    private readonly IReadOnlyList<(string Display, string Url)> _websites =
    [
        ("GitHub Assets", "https://release-assets.githubusercontent.com"),
        ("Mojang Meta", "https://launchermeta.mojang.com"),
        ("PrismLauncher Meta", "https://meta.prismlauncher.org"),
        ("Starlight Skins", "https://starlightskins.lunareclipse.studio"),
        ("CurseForge API", "https://api.curseforge.com"),
        ("Modrinth API", "https://api.modrinth.com")
    ];

    private CancellationTokenSource? _cts;

    public NetworkCheckerWidget() => AvaloniaXamlLoader.Load(this);

    #region Direct

    public ObservableCollection<ConnectionTestSiteModel> Sites { get; } = [];

    #endregion

    protected override Task OnInitializeAsync()
    {
        // 初始化网站列表
        _cts = new();
        foreach (var (display, url) in _websites)
        {
            Sites.Add(new(display, new(url)));
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        // 取消正在进行的测试
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
    } = "Start Test";

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanPerform))]
    private async Task PerformAsync()
    {
        if (IsTesting)
        {
            // 取消当前测试
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new();
            return;
        }

        IsTesting = true;
        HasTested = false;
        ButtonText = "Cancel";

        try
        {
            NetworkCheckHelper.ResetAll(Sites);

            // 从服务容器获取 HttpClientFactory
            var httpClientFactory = Program.AppHost?.Services.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();

            await NetworkCheckHelper.TestConnectionsAsync(Sites, httpClient, _cts?.Token ?? CancellationToken.None);

            HasTested = true;
            ButtonText = "Test Again";
        }
        catch (OperationCanceledException)
        {
            ButtonText = "Start Test";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool CanPerform() => true;

    #endregion
}
