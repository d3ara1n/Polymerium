using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition.SystemBackdrops;
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

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ServiceCollection services = new();
            ConfigureServices(services);
            Provider = services.BuildServiceProvider();
            this.UseSegoeMetrics();
        }

        public static new App Current => (App)Application.Current;

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
            TridentContext trident = new(
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
                .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers().ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent",
                        $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                }).AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync()))
                .AddLogging(builder => builder.AddDebug().AddConsole());

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
                .AddSingleton(trident);
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
                .AddExtractor<CurseForgeExtractor>()
                .AddExtractor<ModrinthExtractor>();

            // Engines
            services
                .AddEngine<DeployEngine>()
                .AddEngine<ResolveEngine>()
                .AddEngine<DownloadEngine>()
                .AddEngine<LaunchEngine>();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            //this.UseSegoeMetrics();
            Spawn(Provider.GetRequiredService<NavigationService>(), Provider.GetRequiredService<NotificationService>(),
                Provider.GetRequiredService<TaskService>(), Provider.GetRequiredService<MessageService>(),
                Provider.GetRequiredService<DialogService>(), Provider.GetRequiredService<ModalService>());
        }

        private void Spawn(NavigationService navigation, NotificationService notification, TaskService task,
            MessageService message, DialogService dialog, ModalService modal)
        {
            const string KEY_HEIGHT = "Window.Height";
            const string KEY_WIDTH = "Window.Width";
            var settings = ApplicationData.Current.LocalSettings;
            Layout layout = new();
            Window window = new()
            {
                Title = "Polymerium: Powered by Trident",
                ExtendsContentIntoTitleBar = true,
                Content = layout,
                SystemBackdrop = Settings.Style switch { 0 => null, 1 => new DesktopAcrylicBackdrop(), 2 => new MicaBackdrop(), 3 => new MicaBackdrop() { Kind = MicaKind.BaseAlt }, _ => throw new NotImplementedException() }
            };
            window.SetTitleBar(layout.Titlebar);
            window.Activated += (_, args) =>
                layout.OnActivate(args.WindowActivationState != WindowActivationState.Deactivated);
            window.Closed += (_, _) =>
            {
                if (window.AppWindow.Presenter.Kind is AppWindowPresenterKind.Default
                    or AppWindowPresenterKind.Overlapped)
                {
                    var size = window.AppWindow.Size;
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
            layout.SetHandler((view, parameter, info) => navigation.Navigate(view, parameter, info, true));
            if (settings.Values.TryGetValue(KEY_HEIGHT, out var h) && h is int height
                                                                   && settings.Values.TryGetValue(KEY_WIDTH,
                                                                       out var w) &&
                                                                   w is int width)
            {
                window.AppWindow.Resize(new SizeInt32(width, height));
            }

            Window = window;
            window.Activate();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => Dump(e.Exception);

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e) => Dump(e.ExceptionObject);

        private void Dump(object core)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trident", ".polymerium", "dump", $"{DateTimeOffset.Now.ToFileTime()}.txt");
            var dir = Path.GetDirectoryName(path);
            try
            {
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, core.ToString());
            }
            catch
            {
                Console.WriteLine(core.ToString());
            }
        }
    }
}