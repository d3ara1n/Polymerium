using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;

namespace Polymerium.App.Dialogs;

public sealed partial class InstanceSelectorDialog : ContentDialog
{
    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CandidatesProperty =
        DependencyProperty.Register(nameof(Candidates), typeof(ObservableCollection<GameInstance>),
            typeof(InstanceSelectorDialog), new PropertyMetadata(new ObservableCollection<GameInstance>()));

    // Using a DependencyProperty as the backing store for SelectedInstance.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedInstanceProperty =
        DependencyProperty.Register(nameof(SelectedInstance), typeof(GameInstance), typeof(InstanceSelectorDialog),
            new PropertyMetadata(null));


    public InstanceSelectorDialog(IEnumerable<GameInstance> candidates)
    {
        InitializeComponent();
        Candidates.Clear();
        foreach (var candidate in candidates) Candidates.Add(candidate);
    }

    public ObservableCollection<GameInstance> Candidates
    {
        get => (ObservableCollection<GameInstance>)GetValue(CandidatesProperty);
        set => SetValue(CandidatesProperty, value);
    }

    public GameInstance SelectedInstance
    {
        get => (GameInstance)GetValue(SelectedInstanceProperty);
        set => SetValue(SelectedInstanceProperty, value);
    }
}