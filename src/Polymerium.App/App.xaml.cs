using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Polly;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Extractors;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Windows.Graphics;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App
{
    public partial class App
    {
        private readonly CancellationTokenSource tokenSource = new();

        public App()
        {
            InitializeComponent();

            ServiceCollection services = new();
            ConfigureServices(services);
            Provider = services.BuildServiceProvider();
        }

        public static App Current => (App)Application.Current;

        public IServiceProvider Provider { get; }

        public Window Window { get; private set; } = null!;
        public DispatcherQueue Dispatcher => Window.DispatcherQueue;
        public CancellationToken Token => tokenSource.Token;

        public static T ViewModel<T>()
            where T : ObservableObject
        {
            return Current.Provider.GetRequiredService<T>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            TridentContext context = new(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trident"));

            // App Services
            services
                .AddSerializationOptions(options =>
                {
                    options.WriteIndented = true;
                    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                })
                .AddMemoryCache()
                .AddHttpClient()
                .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers())
                .AddLogging(builder =>
                {
                    builder
                        .AddDebug()
                        .AddConsole();
                })
                .ConfigureHttpClientDefaults(clientBuilder => clientBuilder.ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Add("User-Agent",
                            $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                    })
                    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync()));

            // UI interaction
            services
                .AddSingleton<NavigationService>()
                .AddSingleton<TaskService>()
                .AddSingleton<NotificationService>()
                .AddSingleton<MessageService>()
                .AddSingleton<DialogService>()
                .AddSingleton<InstanceStatusService>()
                .AddSingleton<ModalService>()
                .AddSingleton<InstanceService>();

            // Trident Services
            services
                .AddSingleton(context);
            services
                .AddSingleton<ProfileManager>()
                .AddSingleton<RepositoryAgent>()
                .AddSingleton<DownloadManager>()
                .AddSingleton<ModpackExtractor>()
                .AddSingleton<StorageManager>()
                .AddSingleton<ThumbnailSaver>()
                .AddSingleton<InstanceManager>()
                .AddSingleton<AccountManager>();

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
                .AddViewModel<MetadataViewModel>()
                .AddViewModel<ConfigurationViewModel>()
                .AddViewModel<WorkbenchViewModel>()
                .AddViewModel<DashboardViewModel>();

            // Repositories
            services
                .AddRepository<CurseForgeRepository>()
                .AddRepository<ModrinthRepository>();

            // Extractors
            services
                .AddExtractor<CurseForgeExtractor>();

            // Engines
            services
                .AddEngine<DeployEngine>()
                .AddEngine<ResolveEngine>()
                .AddEngine<DownloadEngine>()
                .AddEngine<LaunchEngine>();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Spawn(Provider.GetRequiredService<NavigationService>(), Provider.GetRequiredService<NotificationService>(),
                Provider.GetRequiredService<TaskService>(), Provider.GetRequiredService<MessageService>(),
                Provider.GetRequiredService<DialogService>(), Provider.GetRequiredService<ModalService>());
        }

        private void Spawn(NavigationService navigation, NotificationService notification, TaskService task,
            MessageService message, DialogService dialog, ModalService modal)
        {
            const string KEY_HEIGHT = "Window.Height";
            const string KEY_WIDTH = "Window.Width";
            Layout layout = new();
            Window window = new()
            {
                Title = "Polymerium: Powered by Trident",
                ExtendsContentIntoTitleBar = true,
                Content = layout,
                SystemBackdrop = new MicaBackdrop()
            };
            ApplicationDataContainer? settings = ApplicationData.Current.LocalSettings;
            window.SetTitleBar(layout.Titlebar);
            window.Activated += (_, args) =>
                layout.OnActivate(args.WindowActivationState != WindowActivationState.Deactivated);
            window.Closed += (_, _) =>
            {
                if (window.AppWindow.Presenter.Kind is AppWindowPresenterKind.Default
                    or AppWindowPresenterKind.Overlapped)
                {
                    SizeInt32 size = window.AppWindow.Size;
                    settings.Values[KEY_HEIGHT] = size.Height;
                    settings.Values[KEY_WIDTH] = size.Width;
                }

                tokenSource.Cancel();
                ((IDisposable)Provider).Dispose();
            };
            navigation.SetHandler(layout.OnNavigate);
            notification.SetHandler(layout.OnEnqueueNotification);
            modal.SetPopHandler(layout.OnPopModal);
            modal.SetDismissHandler(layout.OnDismissModal);
            task.SetHandler(layout.OnEnqueueTask);
            layout.SetMainMenu(navigation.MainNavMenu);
            layout.SetSideMenu(navigation.SideNavMenu);
            layout.SetHandler((view, parameter, info) => navigation.Navigate(view, parameter, info, true));
            if (settings.Values.TryGetValue(KEY_HEIGHT, out object? h) && h is int height
                                                                       && settings.Values.TryGetValue(KEY_WIDTH,
                                                                           out object? w) &&
                                                                       w is int width)
            {
                window.AppWindow.Resize(new SizeInt32(width, height));
            }

            Window = window;
            window.Activate();
        }
    }
}