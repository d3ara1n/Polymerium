using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class ProxySettingsDialog : Dialog
{
    #region Avalonia Properties

    public static readonly DirectProperty<ProxySettingsDialog, int> SelectedModeIndexProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, int>(nameof(SelectedModeIndex),
                                                                  o => o.SelectedModeIndex,
                                                                  (o, v) => o.SelectedModeIndex = v);

    public static readonly DirectProperty<ProxySettingsDialog, int> SelectedProtocolIndexProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, int>(nameof(SelectedProtocolIndex),
                                                                  o => o.SelectedProtocolIndex,
                                                                  (o, v) => o.SelectedProtocolIndex = v);

    public static readonly DirectProperty<ProxySettingsDialog, string> ProxyAddressProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, string>(nameof(ProxyAddress),
                                                                     o => o.ProxyAddress,
                                                                     (o, v) => o.ProxyAddress = v);

    public static readonly DirectProperty<ProxySettingsDialog, string> ProxyPortProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, string>(nameof(ProxyPort),
                                                                     o => o.ProxyPort,
                                                                     (o, v) => o.ProxyPort = v);

    public static readonly DirectProperty<ProxySettingsDialog, string> ProxyUsernameProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, string>(nameof(ProxyUsername),
                                                                     o => o.ProxyUsername,
                                                                     (o, v) => o.ProxyUsername = v);

    public static readonly DirectProperty<ProxySettingsDialog, string> ProxyPasswordProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, string>(nameof(ProxyPassword),
                                                                     o => o.ProxyPassword,
                                                                     (o, v) => o.ProxyPassword = v);

    public static readonly DirectProperty<ProxySettingsDialog, bool> IsTestingProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, bool>(nameof(IsTesting),
                                                                   o => o.IsTesting,
                                                                   (o, v) => o.IsTesting = v);

    public static readonly DirectProperty<ProxySettingsDialog, int> TestStateProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, int>(nameof(TestState),
                                                                  o => o.TestState,
                                                                  (o, v) => o.TestState = v);

    public static readonly DirectProperty<ProxySettingsDialog, string> TestResultMessageProperty =
        AvaloniaProperty.RegisterDirect<ProxySettingsDialog, string>(nameof(TestResultMessage),
                                                                     o => o.TestResultMessage,
                                                                     (o, v) => o.TestResultMessage = v);


    public int SelectedModeIndex
    {
        get;
        set
        {
            if (SetAndRaise(SelectedModeIndexProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    }

    public int SelectedProtocolIndex
    {
        get;
        set
        {
            if (SetAndRaise(SelectedProtocolIndexProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    }

    public string ProxyAddress
    {
        get;
        set
        {
            if (SetAndRaise(ProxyAddressProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    } = "127.0.0.1";

    public string ProxyPort
    {
        get;
        set
        {
            if (SetAndRaise(ProxyPortProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    } = "7890";

    public string ProxyUsername
    {
        get;
        set
        {
            if (SetAndRaise(ProxyUsernameProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    } = string.Empty;

    public string ProxyPassword
    {
        get;
        set
        {
            if (SetAndRaise(ProxyPasswordProperty, ref field, value))
            {
                UpdateResult();
            }
        }
    } = string.Empty;

    public bool IsTesting
    {
        get;
        set => SetAndRaise(IsTestingProperty, ref field, value);
    }

    public int TestState
    {
        get;
        set => SetAndRaise(TestStateProperty, ref field, value);
    }

    public string TestResultMessage
    {
        get;
        set => SetAndRaise(TestResultMessageProperty, ref field, value);
    } = string.Empty;

    #endregion

    #region Overrides

    protected override bool ValidateResult(object? result) => true;

    #endregion

    public ProxySettingsDialog()
    {
        InitializeComponent();
    }

    #region Exposed Methods

    /// <summary>
    /// Initializes the dialog with the current proxy settings.
    /// </summary>
    public void Initialize(ProxySettingsModel settings)
    {
        SelectedModeIndex = (int)settings.Mode;
        SelectedProtocolIndex = (int)settings.Protocol;
        ProxyAddress = settings.Address;
        ProxyPort = settings.Port.ToString();
        ProxyUsername = settings.Username;
        ProxyPassword = settings.Password;
        Result = GenerateSettings();
    }

    /// <summary>
    /// Gets the current settings from the dialog.
    /// </summary>
    public ProxySettingsModel GenerateSettings()
    {
        var port = uint.TryParse(ProxyPort, out var parsedPort) ? parsedPort : 7890u;
        return new()
        {
            Mode = (ProxyMode)SelectedModeIndex,
            Protocol = (ProxyProtocol)SelectedProtocolIndex,
            Address = ProxyAddress,
            Port = port,
            Username = ProxyUsername,
            Password = ProxyPassword
        };
    }

    #endregion

    #region Commands

    private void UpdateResult()
    {
        Result = GenerateSettings();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestState = 1; // Testing

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var handler = CreateHttpClientHandler();
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            // Use httpbin.org for testing, or a more reliable endpoint
            var response = await client.GetAsync("https://httpbin.org/get");
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                TestState = 2; // Success
                TestResultMessage =
                    $"{Properties.Resources.ProxySettingsDialog_TestSuccessLabelText} ({stopwatch.ElapsedMilliseconds}ms)";
            }
            else
            {
                TestState = 3; // Failed
                TestResultMessage = $"HTTP {(int)response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            TestState = 3; // Failed
            TestResultMessage = "Timeout";
        }
        catch (HttpRequestException ex)
        {
            TestState = 3; // Failed
            TestResultMessage = ex.InnerException?.Message ?? ex.Message;
        }
        catch (Exception ex)
        {
            TestState = 3; // Failed
            TestResultMessage = ex.Message;
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Other

    private HttpClientHandler CreateHttpClientHandler()
    {
        var handler = new HttpClientHandler();

        if (!uint.TryParse(ProxyPort, out var port))
        {
            port = 7890;
        }

        var protocol = (ProxyProtocol)SelectedProtocolIndex;
        var proxyUri = protocol switch
        {
            ProxyProtocol.Socks4 => new Uri($"socks4://{ProxyAddress}:{port}"),
            ProxyProtocol.Socks5 => new Uri($"socks5://{ProxyAddress}:{port}"),
            _ => new Uri($"http://{ProxyAddress}:{port}")
        };

        var proxy = new WebProxy(proxyUri);

        // Set credentials if provided
        if (!string.IsNullOrEmpty(ProxyUsername))
        {
            proxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
        }

        handler.Proxy = proxy;
        handler.UseProxy = true;

        return handler;
    }

    #endregion
}
