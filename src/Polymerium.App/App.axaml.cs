using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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

        AppDomain.CurrentDomain.UnhandledException += (_, e) => Dump(e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => Dump(e.Exception);
        Dispatcher.UIThread.UnhandledException += (_, e) => Dump(e.Exception);

        base.OnFrameworkInitializationCompleted();
    }


    private static void Dump(object core)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "dumps", $"Exception-{DateTimeOffset.Now.ToFileTime()}.log");
        var sb = new StringBuilder(
            $"""
             // {DateTimeOffset.Now.ToString()}
             // {Assembly.GetEntryAssembly()?.GetName().Version}

             """
        );
        sb.AppendLine();
        DumpInternal(sb, core, 0);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, sb.ToString());
    }

    private static void DumpInternal(StringBuilder builder, object core, int level)
    {
        switch (core)
        {
            case AggregateException ae:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Exception: {ae.GetType().Name}
                                    Message: {ae.Message}
                                    StackTrace: {ae.StackTrace}

                                    """);
                foreach (var inner in ae.InnerExceptions) DumpInternal(builder, inner, level + 1);

                if (ae.InnerException is not null) DumpInternal(builder, ae.InnerException, level + 1);
                break;
            case Exception e:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Exception: {e.GetType().Name}
                                    Message: {e.Message}
                                    StackTrace: {e.StackTrace}

                                    """);
                if (e.InnerException is not null) DumpInternal(builder, e.InnerException, level + 1);
                break;
            default:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Content: {core.ToString()}

                                    """);
                break;
        }
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

                var page = Activator.CreateInstance(view) as Page;

                if (type is not null)
                {
                    if (!type.IsAssignableTo(typeof(ObservableObject)))
                        throw new ArgumentOutOfRangeException(nameof(type), type,
                            $"{view.Name} was bound to a view model which is not derived from ObservableObject");

                    var viewModel =
                        type.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType == typeof(ViewBag)))
                            ? ActivatorUtilities.CreateInstance(Program.AppHost.Services, type,
                                parameter is not null ? new ViewBag(parameter) : ViewBag.Empty)
                            : ActivatorUtilities.CreateInstance(Program.AppHost.Services, type);

                    if (page is not null)
                    {
                        page.DataContext = viewModel;

                        if (viewModel is IPageModel pageModel) page.Model = pageModel;
                    }
                }

                return page;
            });

            navigation.SetHandler(window.Navigate);

            return window;
        }

        return new MainWindow();
    }
}