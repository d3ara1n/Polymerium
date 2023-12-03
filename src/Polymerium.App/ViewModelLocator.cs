using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.Views;

namespace Polymerium.App;

public class ViewModelLocator
{
    public static ViewModelLocator Current { get; private set; } = null!;
    public IServiceProvider Provider { get; }

    private readonly PageLink[] _mainNavItems =
    {
        new("主页", "Home", typeof(HomeView)),
        new("实例", "Apps", typeof(InstanceListView))
    };

    private readonly PageLink[] _footerNavItems =
    {
        new("设置", "Settings", typeof(SettingsView))
    };

    public IEnumerable<PageLink> MainNavItems => _mainNavItems;

    public IEnumerable<PageLink> FooterNavItems => _footerNavItems;

    public ViewModelLocator()
    {
        var container = new ServiceCollection();
        ConfigureServices(container);
        Provider = container.BuildServiceProvider();

        Current = this;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<DialogService>();

        services.AddScoped<HomeViewModel>();
        services.AddScoped<InstanceListViewModel>();
        services.AddScoped<SettingsViewModel>();
    }

    public HomeViewModel Home => Provider.GetRequiredService<HomeViewModel>();
    public InstanceListViewModel InstanceList => Provider.GetRequiredService<InstanceListViewModel>();
    public SettingsViewModel Settings => Provider.GetRequiredService<SettingsViewModel>();
}