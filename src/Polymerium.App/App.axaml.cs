using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = ConstructWindow();

        base.OnFrameworkInitializationCompleted();
    }

    private static Window ConstructWindow()
    {
        if (Program.AppHost is not null)
        {
            var window = new MainWindow();

            // Link navigation service
            var navigation = Program.AppHost.Services.GetRequiredService<NavigationService>();

            // Closure captures Program.AppHost.Services
            window.BindNavigation(navigation.Navigate, (view, parameter) =>
            {
                if (!view.IsAssignableTo(typeof(Page)))
                    throw new ArgumentOutOfRangeException(nameof(view), view,
                        "Parameter view must be derived from Page");

                var name = view.FullName!.Replace("View", "ViewModel", StringComparison.Ordinal);
                var type = Type.GetType(name);
                ArgumentNullException.ThrowIfNull(type);
                if (!type.IsAssignableTo(typeof(ObservableObject)))
                    throw new ArgumentOutOfRangeException(nameof(type), type,
                        $"{view.Name} was bound to a view model which is not derived from ObservableObject");

                var viewModel =
                    type.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType == typeof(ViewBag)))
                        ? ActivatorUtilities.CreateInstance(Program.AppHost.Services, type,
                            parameter is not null ? new ViewBag(parameter) : ViewBag.Empty)
                        : ActivatorUtilities.CreateInstance(Program.AppHost.Services, type);

                var page = Activator.CreateInstance(view) as Page;

                if (page is not null)
                {
                    page.DataContext = viewModel;

                    if (viewModel is IPageModel pageModel) page.Model = pageModel;
                }

                return page;
            });

            navigation.SetHandler(window.Navigate);

            return window;
        }

        return new MainWindow();
    }
}