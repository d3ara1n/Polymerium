using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Toasts;

public partial class ExhibitModpackToast : Toast
{
    public static readonly StyledProperty<IRelayCommand<ExhibitVersionModel>?> InstallCommandProperty =
        AvaloniaProperty.Register<ExhibitModpackToast, IRelayCommand<ExhibitVersionModel>?>(nameof(InstallCommand));

    public ExhibitModpackToast() => InitializeComponent();

    public IRelayCommand<ExhibitVersionModel>? InstallCommand
    {
        get => GetValue(InstallCommandProperty);
        set => SetValue(InstallCommandProperty, value);
    }

    private void SourceLinkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExhibitModpackModel { ReferenceUrl: not null } model)
        {
            var toplevel = TopLevel.GetTopLevel(this);
            toplevel?.Launcher.LaunchUriAsync(model.ReferenceUrl);
        }
    }
}