using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class CreateInstanceWizardDialog : CustomDialog
{
    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VersionsProperty =
        DependencyProperty.Register(nameof(Versions), typeof(IEnumerable<GameVersionModel>),
            typeof(CreateInstanceWizardDialog), new PropertyMetadata(null));

    public static readonly DependencyProperty IsOperableProperty =
        DependencyProperty.Register(nameof(IsOperable), typeof(bool), typeof(CreateInstanceWizardDialog),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(CreateInstanceWizardDialog),
            new PropertyMetadata(string.Empty));

    private ContentControl? _root;

    public CreateInstanceWizardDialog()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<CreateInstanceWizardViewModel>();
    }

    public IEnumerable<GameVersionModel> Versions
    {
        get => (IEnumerable<GameVersionModel>)GetValue(VersionsProperty);
        set => SetValue(VersionsProperty, value);
    }

    public bool IsOperable
    {
        get => (bool)GetValue(IsOperableProperty);
        set => SetValue(IsOperableProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public CreateInstanceWizardViewModel ViewModel { get; }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = FindName("Root") as ContentControl;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void CreateInstanceWizardDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Loading", false);
        IsOperable = false;
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_FillDataCompletedAsync(IEnumerable<GameVersionModel> data)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            Versions = data;
            ViewModel.SelectedVersion = Versions.FirstOrDefault();
            ViewModel.InstanceName = ViewModel.SelectedVersion?.Id ?? string.Empty;
            VisualStateManager.GoToState(_root, "Default", false);
        });
        return Task.CompletedTask;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var errors = ViewModel.GetErrors();
        var validationResults = errors as ValidationResult[] ?? errors.ToArray();
        if (validationResults.Any())
        {
            ErrorMessage = string.Join('\n', validationResults.Select(x => x.ErrorMessage));
        }
        else
        {
            ErrorMessage = string.Empty;
            VisualStateManager.GoToState(_root, "Working", false);
            IsOperable = false;
            Task.Run(() => ViewModel.Commit(ViewModel_CommitCompletedAsync), CancellationToken.None);
        }
    }

    private Task ViewModel_CommitCompletedAsync()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            VisualStateManager.GoToState(_root, "Default", false);
            Dismiss();
        });
        return Task.CompletedTask;
    }

    private void VersionBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var input = sender.Text;
            if (string.IsNullOrEmpty(input))
                sender.ItemsSource = Versions;
            else
                sender.ItemsSource = Versions.Where(x => x.Id.StartsWith(input));
        }
    }

    private void VersionBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var item = args.SelectedItem as GameVersionModel;
        ViewModel.SelectedVersion = item;
        if (string.IsNullOrEmpty(ViewModel.InstanceName) ||
            ViewModel.InstanceName == item?.Id)
            ViewModel.InstanceName = item?.Id ?? string.Empty;
    }
}