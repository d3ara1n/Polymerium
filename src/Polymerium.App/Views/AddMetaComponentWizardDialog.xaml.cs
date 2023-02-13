using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;
using Polymerium.Core.Components;

namespace Polymerium.App.Views;

public sealed partial class AddMetaComponentWizardDialog : CustomDialog
{
    // Using a DependencyProperty as the backing store for IsOperable.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOperableProperty = DependencyProperty.Register(
        nameof(IsOperable),
        typeof(bool),
        typeof(AddMetaComponentWizardDialog),
        new PropertyMetadata(true)
    );

    public AddMetaComponentWizardDialog()
    {
        ViewModel = App.Current.Provider.GetRequiredService<AddMetaComponentWizardViewModel>();
        InitializeComponent();
        ViewModel.DismissHandler = Dismiss;
    }

    public bool IsOperable
    {
        get => (bool)GetValue(IsOperableProperty);
        set => SetValue(IsOperableProperty, value);
    }

    public AddMetaComponentWizardViewModel ViewModel { get; }

    private void MetaSelection_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Any())
        {
            var identity = ((ComponentMeta)e.AddedItems.First()).Identity;
            IsOperable = false;
            VisualStateManager.GoToState(Root, "Loading", true);
            Task.Run(async () => await ViewModel.LoadVersionsAsync(identity, Callback));
        }
    }

    private void Callback(IEnumerable<string> versions)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Versions = versions;
            IsOperable = true;
            VisualStateManager.GoToState(Root, "Default", true);
        });
    }
}
