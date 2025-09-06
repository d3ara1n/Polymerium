using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public partial class UnknownViewModel(
    ViewBag bag,
    NotificationService notificationService,
    OverlayService overlayService) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";


    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(7), token);

        if (Application.Current is { PlatformSettings: not null })
        {
            var accent1 = Application.Current.PlatformSettings.GetColorValues().AccentColor1;
            var accent2 = Application.Current.PlatformSettings.GetColorValues().AccentColor2;
            var accent3 = Application.Current.PlatformSettings.GetColorValues().AccentColor3;
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void ShowNotification() => notificationService.PopMessage("Hello", "Hi there!");

    [RelayCommand]
    private void ShowToast()
    {
        PopToast();
        return;

        void PopToast()
        {
            var pop = new Button { Content = "POP" };
            pop.Click += (_, __) => PopToast();
            overlayService.PopToast(new()
            {
                Header = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}",
                Content = new StackPanel
                {
                    Spacing = 8d,
                    Children =
                    {
                        new TextBlock
                        {
                            Text =
                                "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM"
                        },
                        new TextBox(),
                        pop
                    }
                }
            });
        }
    }

    [RelayCommand]
    private void ShowDrawer()
    {
        PopDrawer();
        return;

        void PopDrawer()
        {
            var drawer = new Drawer();
            var pop = new Button() { Content = "POP" };
            var dismiss = new Button() { Content = "DISMISS" };
            pop.Click += (_, __) => PopDrawer();
            dismiss.Click += (_, __) => drawer.Dismiss();
            drawer.Content = new StackPanel
            {
                Spacing = 8d,
                Children = { new TextBox() { Text = $"DRAWER {Random.Shared.Next(1000, 9999)}" }, pop, dismiss }
            };
            overlayService.PopDrawer(drawer);
        }
    }

    [RelayCommand]
    private void ShowModal()
    {
        PopModal();
        return;

        void PopModal()
        {
            var modal = new Modal();
            var pop = new Button() { Content = "POP" };
            var dismiss = new Button() { Content = "DISMISS" };
            pop.Click += (_, __) => PopModal();
            dismiss.Click += (_, __) => modal.Dismiss();
            modal.Content = new StackPanel
            {
                Spacing = 8d,
                Children = { new TextBox() { Text = $"MODAL {Random.Shared.Next(1000, 9999)}" }, pop, dismiss }
            };
            overlayService.PopModal(modal);
        }
    }

    [RelayCommand]
    private void ShowDialog()
    {
        PopDialog();
        return;

        void PopDialog()
        {
            var pop = new Button { Content = "POP" };
            pop.Click += (_, __) => PopDialog();
            overlayService.PopDialog(new()
            {
                Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
                Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                Content = new StackPanel
                {
                    Spacing = 8d, Children = { new TextBox(), pop }
                }
            });
        }
    }

    [RelayCommand]
    private void Debug()
    {
        overlayService.PopDialog(new ModpackExporterDialog());
    }

    #endregion
}
