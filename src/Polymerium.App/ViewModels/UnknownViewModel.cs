using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
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
    private void Hello() => notificationService.PopMessage("Hello", "Hi there!");

    [RelayCommand]
    private void World()
    {
        PopToast();
        return;

        void PopToast()
        {
            Button pop = new() { Content = "POP" };
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
    private void Butcher()
    {
        PopDialog();
        return;

        void PopDialog()
        {
            Button pop = new() { Content = "POP" };
            pop.Click += (_, __) => PopDialog();
            overlayService.PopDialog(new()
            {
                Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
                Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                Content = new StackPanel
                {
                    Spacing = 8d,
                    Children = { new TextBox(), pop }
                }
            });
        }
    }

    [RelayCommand]
    private void Debug() { }

    #endregion
}
