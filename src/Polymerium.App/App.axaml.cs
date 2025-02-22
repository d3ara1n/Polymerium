using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using HotAvalonia;
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public class App : Application
{
    public override void Initialize()
    {
        #if DEBUG
        this.EnableHotReload();
        #endif
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = ConstructWindow();

        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowOrDump(e.ExceptionObject, e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) => ShowOrDump(e.Exception, !e.Observed);
        Dispatcher.UIThread.UnhandledException += (_, e) => ShowOrDump(e.Exception, !e.Handled);

        base.OnFrameworkInitializationCompleted();
    }


    private static void ShowOrDump(object core, bool critical = false)
    {
        if (core is Exception ex && !critical && Program.AppHost?.Services.GetService<NavigationService>() is { } navigation)
            navigation.Navigate<ExceptionView>(ex);
        else
            Dump(core);
    }

    private static void Dump(object core)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "dumps", $"Exception-{DateTimeOffset.Now.ToFileTime()}.log");
        StringBuilder sb = new($"""
                                // {DateTimeOffset.Now.ToString()}
                                // {Assembly.GetEntryAssembly()?.GetName().Version}

                                """);
        sb.AppendLine();
        DumpInternal(sb, core, 0);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

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
                foreach (var inner in ae.InnerExceptions)
                    DumpInternal(builder, inner, level + 1);

                if (ae.InnerException is not null)
                    DumpInternal(builder, ae.InnerException, level + 1);

                break;
            case Exception e:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Exception: {e.GetType().Name}
                                    Message: {e.Message}
                                    StackTrace: {e.StackTrace}

                                    """);
                if (e.InnerException is not null)
                    DumpInternal(builder, e.InnerException, level + 1);

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
        if (Program.AppHost is null)
            return new MainWindow();

        MainWindow window = new();

        #region Navigation

        // Link navigation service
        var navigation = Program.AppHost.Services.GetRequiredService<NavigationService>();
        // Closure captures Program.AppHost.Services
        window.BindNavigation(navigation.Navigate,
                              (view, parameter) =>
                              {
                                  if (!view.IsAssignableTo(typeof(Page)))
                                      throw new ArgumentOutOfRangeException(nameof(view), view, "Parameter view must be derived from Page");

                                  var name = view.FullName!.Replace("View", "ViewModel", StringComparison.Ordinal);
                                  var type = Type.GetType(name);

                                  var page = Activator.CreateInstance(view) as Page;

                                  if (type is not null)
                                  {
                                      if (!type.IsAssignableTo(typeof(ObservableObject)))
                                          throw new ArgumentOutOfRangeException(nameof(type), type, $"{view.Name} was bound to a view model which is not derived from ObservableObject");

                                      using var scope = Program.AppHost.Services.CreateScope();

                                      var factory = scope.ServiceProvider.GetRequiredService<ViewBagFactory>();
                                      factory.Bag = parameter;

                                      var viewModel = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type);

                                      if (page is not null)
                                      {
                                          page.DataContext = viewModel;

                                          if (viewModel is IPageModel pageModel)
                                              page.Model = pageModel;
                                      }
                                  }

                                  return page;
                              });

        navigation.SetHandler(window.Navigate);

        #endregion

        #region Profile

        var profile = Program.AppHost.Services.GetRequiredService<ProfileManager>();
        window.SubscribeProfileList(profile);
        var instance = Program.AppHost.Services.GetRequiredService<InstanceManager>();
        window.SubscribeState(instance);

        #endregion

        #region Overlay & Notificiation

        var overlay = Program.AppHost.Services.GetRequiredService<OverlayService>();
        overlay.SetHandler(window.PopToast, window.PopModal, window.PopDialog);
        var notification = Program.AppHost.Services.GetRequiredService<NotificationService>();
        notification.SetHandler(window.PopNotification);

        #endregion

        return window;
    }
}