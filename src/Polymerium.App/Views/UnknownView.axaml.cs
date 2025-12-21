using Avalonia;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;

namespace Polymerium.App.Views;

public partial class UnknownView : Page
{
    public UnknownView()
    {
        InitializeComponent();
    }


    private void ThemeSwitchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            if (Application.Current.ActualThemeVariant == ThemeVariant.Light)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
        }
    }

    private void DropContainer_OnDragOver(object? sender, DropContainer.DragOverEventArgs e)
    {
			if (e.Data.Contains(DataFormat.Text))
        {
            e.IsValid = true;
        }
    }

    private void DropContainer_OnDrop(object? sender, DropContainer.DropEventArgs e)
    {
			if (e.Data.Contains(DataFormat.Text))
        {
				var text = e.Data.TryGetText();
            if (text != null)
            {
                DragText.Text = text;
            }
        }
    }
}
