using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Views;

public partial class InstanceView : ScopedPage
{
    public InstanceView()
    {
        InitializeComponent();

        NavigateCommand = new RelayCommand<Type>(Navigate);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var activator = this.FindAncestorOfType<MainWindow>()?.PageActivator;
        if (activator != null)
            Frame.PageActivator = activator;
    }

    public static readonly DirectProperty<InstanceView, string> KeyProperty =
        AvaloniaProperty.RegisterDirect<InstanceView, string>(nameof(Key), o => o.Key, (o, v) => o.Key = v);

    private string _key = string.Empty;

    public string Key
    {
        get => _key;
        set => SetAndRaise(KeyProperty, ref _key, value);
    }

    public static readonly DirectProperty<InstanceView, ICommand?> NavigateCommandProperty =
        AvaloniaProperty.RegisterDirect<InstanceView, ICommand?>(nameof(NavigateCommand),
                                                                 o => o.NavigateCommand,
                                                                 (o, v) => o.NavigateCommand = v);

    private ICommand? _navigateCommand;

    public ICommand? NavigateCommand
    {
        get => _navigateCommand;
        set => SetAndRaise(NavigateCommandProperty, ref _navigateCommand, value);
    }


    private void EntryBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var oldIndex = -1;
        if (e.RemovedItems.Count > 0)
            oldIndex = EntryBox.Items.IndexOf(e.RemovedItems[0]);

        if (e.AddedItems.Count > 0)
            if (e.AddedItems[0] is InstanceSubpageEntryModel selected)
            {
                var newIndex = EntryBox.Items.IndexOf(selected);
                IPageTransition transition = oldIndex != -1
                                                 ? new PageSlideTransition(newIndex - oldIndex > 0
                                                                               ? DirectionFrom.Bottom
                                                                               : DirectionFrom.Top)
                                                 : new HookUpTransition();

                Frame.Navigate(selected.Page, Key, transition);
            }
    }

    private void Navigate(Type? page)
    {
        if (page != null)
        {
            var found = -1;
            for (var i = 0; i < EntryBox.Items.Count; i++)
                if (EntryBox.Items[i] is InstanceSubpageEntryModel entry && entry.Page == page)
                {
                    found = i;
                    break;
                }

            if (found != -1)
                EntryBox.SelectedIndex = found;
            else
                throw new NotImplementedException();
        }
    }
}