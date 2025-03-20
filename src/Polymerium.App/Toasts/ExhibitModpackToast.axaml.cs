using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Toasts;

public partial class ExhibitModpackToast : Toast
{
    public ExhibitModpackToast() => InitializeComponent();

    public required IRelayCommand<ExhibitVersionModel> InstallCommand { get; init; }

    private void SourceLinkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExhibitModpackModel { ReferenceUrl: not null } model)
        {
            var toplevel = TopLevel.GetTopLevel(this);
            toplevel?.Launcher.LaunchUriAsync(model.ReferenceUrl);
        }
    }
}