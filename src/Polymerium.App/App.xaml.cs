using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.Trident.Managers;
using Polymerium.Trident;
using System;
using System.Collections.Generic;
using System.IO;
using Polymerium.App.ViewModels;
using Polymerium.App.Views;
using Microsoft.Extensions.Logging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App
{
    public partial class App : Application
    {

        public static new App Current => (App)Application.Current;

        public static T ViewModel<T>()
            where T : ViewModelBase
            => Current.Provider.GetRequiredService<T>();

        public IServiceProvider Provider { get; }

        public App()
        {
            this.InitializeComponent();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Provider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSerializationOptions(options =>
                {
                    options.WriteIndented = true;
                })
                .AddLogging(builder =>
                {
                    builder
                    .AddDebug()
                    .AddConsole();
                })
                .AddNavigation();

            services
                .AddSingleton(new PolymeriumContext(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".polymerium")));

            services
                .AddSingleton<EntryManager>();

            services
                .AddViewModel<HomeViewModel>()
                .AddViewModel<InstanceListViewModel>()
                .AddViewModel<InstanceDetailViewModel>()
                .AddViewModel<AccountViewModel>()
                .AddViewModel<MarketViewModel>()
                .AddViewModel<SettingViewModel>()
                .AddViewModel<InformationViewModel>();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Spawn(Provider.GetRequiredService<NavigationService>()).Activate();
        }

        private Window Spawn(NavigationService navigation)
        {
            var layout = new Layout();
            var window = new Window()
            {
                Title = "Polymerium: Powered by Trident",
                ExtendsContentIntoTitleBar = true,
                Content = layout,
                SystemBackdrop = new MicaBackdrop()
            };
            window.SetTitleBar(layout.Titlebar);
            window.Activated += (_, args) => layout.OnActivate(args.WindowActivationState != WindowActivationState.Deactivated);
            window.Closed += (_, args) => ((IDisposable)Provider).Dispose();
            navigation.SetHandler(layout.OnNavigate);
            layout.SetMainMenu(navigation.MainNavMenu);
            layout.SetSideMenu(navigation.SideNavMenu);
            layout.SetHandler((view, info) => navigation.Navigate(view, null, info, true));
            return window;
        }
    }
}
