using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public partial class UnknownViewModel(
    ViewBag bag,
    NotificationService notificationService,
    OverlayService overlayService) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";

    protected override Task OnInitializedAsync(CancellationToken token) => Task.Delay(TimeSpan.FromSeconds(7), token);

    #region Commands

    [RelayCommand]
    private void Hello() => notificationService.PopMessage("Hello", "Hi there!");

    [RelayCommand]
    private void World()
    {
        void PopToast()
        {
            Button pop = new() { Content = "POP" };
            pop.Click += (_, __) => PopToast();
            overlayService.PopToast(new Toast
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

        PopToast();
    }

    [RelayCommand]
    private void Butcher()
    {
        void PopDialog()
        {
            Button pop = new() { Content = "POP" };
            pop.Click += (_, __) => PopDialog();
            overlayService.PopDialog(new Dialog
            {
                Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
                Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
                Content = new StackPanel
                {
                    Spacing = 8d, Children = { new TextBox(), pop }
                }
            });
        }

        PopDialog();
    }

    [RelayCommand]
    private void Debug()
    {
        overlayService.PopModal(new AccountEntryModal());
    }

    #endregion
}