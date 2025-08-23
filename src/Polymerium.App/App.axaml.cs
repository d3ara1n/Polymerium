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
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App
{
    public class App : Application
    {
        private static int activatorErrorCount;

        public HuskuiTheme? Theme { get; private set; }


        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowOrDump(e.ExceptionObject, e.IsTerminating);
            TaskScheduler.UnobservedTaskException += (_, e) => ShowOrDump(e.Exception, !e.Observed);
            Dispatcher.UIThread.UnhandledException += (_, e) => ShowOrDump(e.Exception, !e.Handled);

            foreach (var styles in Styles)
            {
                if (styles is HuskuiTheme husk)
                {
                    Theme = husk;
                    break;
                }
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = ConstructWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }


        private static void ShowOrDump(object core, bool critical = false)
        {
            if (core is Exception ex
             && !critical
             && Program.AppHost?.Services.GetService<NavigationService>() is { } navigation)
            {
                Dispatcher.UIThread.Post(() => navigation.Navigate<ExceptionView>(ex));
            }
            else
            {
                Dump(core);
            }
        }

        private static void Dump(object core)
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                                    "dumps",
                                    $"Exception-{DateTimeOffset.Now.ToFileTime()}.log");
            var sb = new StringBuilder($"""
                                        // {DateTimeOffset.Now.ToString()}
                                        // {Assembly.GetEntryAssembly()?.GetName().Version}

                                        """);
            sb.AppendLine();
            DumpInternal(sb, core, 0);
            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

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
                    {
                        DumpInternal(builder, inner, level + 1);
                    }

                    if (ae.InnerException is not null)
                    {
                        DumpInternal(builder, ae.InnerException, level + 1);
                    }

                    break;
                case Exception e:
                    builder.AppendLine($"""
                                        --- LEVEL: {level} ---
                                        Exception: {e.GetType().Name}
                                        Message: {e.Message}
                                        StackTrace: {e.StackTrace}

                                        """);
                    if (e.InnerException is not null)
                    {
                        DumpInternal(builder, e.InnerException, level + 1);
                    }

                    break;
                default:
                    builder.AppendLine($"""
                                        --- LEVEL: {level} ---
                                        Content: {core.ToString()}

                                        """);
                    break;
            }
        }

        private static object? ActivatePage(Type view, object? parameter)
        {
            try
            {
                if (!view.IsAssignableTo(typeof(Page)))
                {
                    throw new ArgumentOutOfRangeException(nameof(view),
                                                          view,
                                                          "Parameter view must be derived from Page");
                }

                var name = view.FullName!.Replace("View", "ViewModel", StringComparison.Ordinal);
                var type = Type.GetType(name);

                var page = Activator.CreateInstance(view) as Page;

                if (page is not null && type is not null)
                {
                    if (!type.IsAssignableTo(typeof(ObservableObject)))
                    {
                        throw new ArgumentOutOfRangeException(nameof(view),
                                                              type,
                                                              $"{view.Name} was bound to a view model which is not derived from ObservableObject");
                    }

                    using var scope = Program.AppHost!.Services.CreateScope();

                    var factory = scope.ServiceProvider.GetRequiredService<ViewBagFactory>();
                    factory.Bag = parameter;

                    var viewModel = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type);

                    page.DataContext = viewModel;

                    if (viewModel is IPageModel pageModel)
                    {
                        page.Model = pageModel;
                    }
                }

                activatorErrorCount = 0;
                return page;
            }
            catch (NavigationFailedException ex)
            {
                // 避免又产生异常而导致无限循环
                if (activatorErrorCount++ < 3)
                {
                    return ActivatePage(typeof(PageNotReachedView), ex.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                // 避免又产生异常而导致无限循环
                if (activatorErrorCount++ < 3)
                {
                    return ActivatePage(typeof(ExceptionView), ex);
                }

                throw;
            }
        }

        private static CultureInfo GetSafeCultureInfo(string cultureName)
        {
            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                // 回退到英语（美国）
                return CultureInfo.GetCultureInfo("en-US");
            }
            catch (ArgumentException)
            {
                // 处理无效的文化名称参数
                return CultureInfo.GetCultureInfo("en-US");
            }
        }

        private static Window ConstructWindow()
        {
            if (Program.AppHost is null)
            {
                return new();
            }

            var configuration = Program.AppHost.Services.GetRequiredService<ConfigurationService>();
            CultureInfo.CurrentUICulture = GetSafeCultureInfo(configuration.Value.ApplicationLanguage);
            Properties.Resources.Culture = CultureInfo.CurrentUICulture;

            var window = new MainWindow();

            window.SetColorVariant(configuration.Value.ApplicationStyleAccent);
            window.SetThemeVariantByIndex(configuration.Value.ApplicationStyleThemeVariant);
            window.SetTransparencyLevelHintByIndex(configuration.Value.ApplicationStyleBackground);
            window.IsTitleBarVisible = configuration.Value.ApplicationTitleBarVisibility;
            window.IsLeftPanelMode = configuration.Value.ApplicationLeftPanelMode;
            // 并不还原窗体大小，没必要

            #region Navigation

            // Link navigation service
            var navigation = Program.AppHost.Services.GetRequiredService<NavigationService>();
            navigation.SetHandler(window.Navigate, window.GoBack, window.CanGoBack, window.ClearHistory);
            // Closure captures Program.AppHost.Services
            window.PageActivator = ActivatePage;

            #endregion

            #region Overlay & Notificiation

            var overlay = Program.AppHost.Services.GetRequiredService<OverlayService>();
            overlay.SetHandler(window.PopToast, window.PopModal, window.PopDialog);
            var notification = Program.AppHost.Services.GetRequiredService<NotificationService>();
            notification.SetHandler(window.PopNotification);

            #endregion


            // 需要放在整个 window 初始化之后，因为 MainWindowContext 的构造函数要求 window 已与服务绑定
            window.DataContext = ActivatorUtilities.CreateInstance<MainWindowContext>(Program.AppHost.Services);

            return window;
        }
    }
}
