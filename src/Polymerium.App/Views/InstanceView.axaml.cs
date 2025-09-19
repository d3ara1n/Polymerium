using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public partial class InstanceView : ScopedPage
{
    public static readonly DirectProperty<InstanceView, InstanceViewModelBase.InstanceContextParameter?>
        ContextProperty =
            AvaloniaProperty
               .RegisterDirect<InstanceView, InstanceViewModelBase.InstanceContextParameter?>(nameof(Context),
                                                                        o => o.Context,
                                                                        (o, v) => o.Context = v);


    public static readonly DirectProperty<InstanceView, ICommand?> NavigateCommandProperty =
        AvaloniaProperty.RegisterDirect<InstanceView, ICommand?>(nameof(NavigateCommand),
                                                                 o => o.NavigateCommand,
                                                                 (o, v) => o.NavigateCommand = v);

    public InstanceView()
    {
        InitializeComponent();

        if (!Design.IsDesignMode)
        {
            Frame.PageActivator = MainWindow.Instance.PageActivator;
        }

        NavigateCommand = new RelayCommand<Type>(Navigate);
    }

    public InstanceViewModelBase.InstanceContextParameter? Context
    {
        get;
        set => SetAndRaise(ContextProperty, ref field, value);
    }

    public ICommand? NavigateCommand
    {
        get;
        set => SetAndRaise(NavigateCommandProperty, ref field, value);
    }


    private void EntryBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var oldIndex = -1;
        if (e.RemovedItems.Count > 0)
        {
            oldIndex = EntryBox.Items.IndexOf(e.RemovedItems[0]);
        }

        if (e.AddedItems.Count > 0)
        {
            if (e.AddedItems[0] is InstanceSubpageEntryModel selected)
            {
                var newIndex = EntryBox.Items.IndexOf(selected);
                IPageTransition transition = oldIndex != -1
                                                 ? new PageSlideTransition
                                                 {
                                                     Direction = newIndex - oldIndex > 0
                                                                     ? DirectionFrom.Bottom
                                                                     : DirectionFrom.Top
                                                 }
                                                 : new PopUpTransition();

                Frame.Navigate(selected.Page, Context, transition);
            }
        }
    }

    private void Navigate(Type? page)
    {
        if (page != null)
        {
            var found = -1;
            for (var i = 0; i < EntryBox.Items.Count; i++)
            {
                if (EntryBox.Items[i] is InstanceSubpageEntryModel entry && entry.Page == page)
                {
                    found = i;
                    break;
                }
            }

            if (found != -1)
            {
                EntryBox.SelectedIndex = found;
            }
            // do nothing
        }
    }

    private void DropContainer_OnDragOver(object? sender, DropContainer.DragOverEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var path = e.Data.GetFiles()?.FirstOrDefault()?.TryGetLocalPath();
            if (path != null && File.Exists(path))
            {
                e.IsValid = true;
            }
        }
    }

    private async void DropContainer_OnDrop(object? sender, DropContainer.DropEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var path = e.Data.GetFiles()?.FirstOrDefault()?.TryGetLocalPath();
            if (path != null && File.Exists(path) && DataContext is InstanceViewModel viewModel)
            {
                await viewModel.ImportFromFileAsync(path);
            }
        }
    }
}
