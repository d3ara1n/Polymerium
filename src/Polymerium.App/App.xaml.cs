using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Polly;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Provider = services.BuildServiceProvider();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Provider { get; }

    public Window Window { get; private set; }

    public static T ViewModel<T>()
        where T : ViewModelBase
    {
        return Current.Provider.GetRequiredService<T>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // App Services
        services
            .AddSerializationOptions(options => { options.WriteIndented = true; })
            .AddLogging(builder =>
            {
                builder
                    .AddDebug()
                    .AddConsole();
            })
            .AddMemoryCache()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(clientBuilder => clientBuilder.ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent",
                        $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                })
                .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync()));

        services
            .AddSingleton<NavigationService>()
            .AddSingleton<TaskService>()
            .AddSingleton<NotificationService>();

        // Trident Services
        services
            .AddSingleton(new PolymeriumContext(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".polymerium")));
        services
            .AddSingleton<ProfileManager>()
            .AddSingleton<RepositoryAgent>()
            .AddSingleton<DownloadManager>();

        // ViewModels
        services
            .AddViewModel<HomeViewModel>()
            .AddViewModel<DesktopViewModel>()
            .AddViewModel<InstanceViewModel>()
            .AddViewModel<AccountViewModel>()
            .AddViewModel<MarketViewModel>()
            .AddViewModel<SettingViewModel>()
            .AddViewModel<ToolboxViewModel>()
            .AddViewModel<ModpackViewModel>()
            .AddViewModel<WorkbenchViewModel>();

        // Repositories
        services
            .AddRepository<CurseForgeRepository>()
            .AddRepository<ModrinthRepository>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = Spawn(Provider.GetRequiredService<NavigationService>());
        Window.Activate();
    }

    private Window Spawn(NavigationService navigation)
    {
        var layout = new Layout();
        var window = new Window
        {
            Title = "Polymerium: Powered by Trident",
            ExtendsContentIntoTitleBar = true,
            Content = layout,
            SystemBackdrop = new MicaBackdrop()
        };
        window.SetTitleBar(layout.Titlebar);
        window.Activated += (_, args) =>
            layout.OnActivate(args.WindowActivationState != WindowActivationState.Deactivated);
        window.Closed += (_, args) => ((IDisposable)Provider).Dispose();
        navigation.SetHandler(layout.OnNavigate);
        layout.SetMainMenu(navigation.MainNavMenu);
        layout.SetSideMenu(navigation.SideNavMenu);
        layout.SetHandler((view, parameter, info) => navigation.Navigate(view, parameter, info, true));
        //var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
        //if (settings.Values.ContainsKey("Window.Height")
        //    && settings.Values["Window.Height"] is int height
        //    && settings.Values.ContainsKey("Window.Width")
        //    && settings.Values["Window.Width"] is int width)
        //{
        //    window.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        //}
        //else
        //{
        //    window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1128, 660));
        //}
        return window;
    }
}