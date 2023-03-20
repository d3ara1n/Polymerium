using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceMetadataConfigurationView : Page
{
    // Using a DependencyProperty as the backing store for IsModChecked.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsModCheckedProperty =
        DependencyProperty.Register(nameof(IsModChecked), typeof(bool), typeof(InstanceMetadataConfigurationView),
            new PropertyMetadata(true, FilterCheckBoxChanged));

    // Using a DependencyProperty as the backing store for IsModChecked.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsResourcepackCheckedProperty =
        DependencyProperty.Register(nameof(IsResourcepackChecked), typeof(bool),
            typeof(InstanceMetadataConfigurationView), new PropertyMetadata(true, FilterCheckBoxChanged));

    // Using a DependencyProperty as the backing store for IsModChecked.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsShaderCheckedProperty =
        DependencyProperty.Register(nameof(IsShaderChecked), typeof(bool), typeof(InstanceMetadataConfigurationView),
            new PropertyMetadata(true, FilterCheckBoxChanged));

    // Using a DependencyProperty as the backing store for IsModChecked.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsFileCheckedProperty =
        DependencyProperty.Register(nameof(IsFileChecked), typeof(bool), typeof(InstanceMetadataConfigurationView),
            new PropertyMetadata(true, FilterCheckBoxChanged));

    // Using a DependencyProperty as the backing store for SelectedItemCount.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedItemCountProperty =
        DependencyProperty.Register(nameof(SelectedItemCount), typeof(int), typeof(InstanceMetadataConfigurationView),
            new PropertyMetadata(0));

    // Using a DependencyProperty as the backing store for CanDelete.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanDeleteProperty =
        DependencyProperty.Register(nameof(CanDelete), typeof(bool), typeof(InstanceMetadataConfigurationView),
            new PropertyMetadata(false));

    private static Func<InstanceAttachmentItemModel, bool>? filter;

    public InstanceMetadataConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceMetadataConfigurationViewModel>();
        ViewModel.SetCallback(AddAttachmentHandler);
        InitializeComponent();
    }

    public bool IsModChecked
    {
        get => (bool)GetValue(IsModCheckedProperty);
        set => SetValue(IsModCheckedProperty, value);
    }

    public bool IsResourcepackChecked
    {
        get => (bool)GetValue(IsResourcepackCheckedProperty);
        set => SetValue(IsResourcepackCheckedProperty, value);
    }

    public bool IsShaderChecked
    {
        get => (bool)GetValue(IsShaderCheckedProperty);
        set => SetValue(IsShaderCheckedProperty, value);
    }

    public bool IsFileChecked
    {
        get => (bool)GetValue(IsFileCheckedProperty);
        set => SetValue(IsFileCheckedProperty, value);
    }


    public int SelectedItemCount
    {
        get => (int)GetValue(SelectedItemCountProperty);
        set => SetValue(SelectedItemCountProperty, value);
    }

    public bool CanDelete
    {
        get => (bool)GetValue(CanDeleteProperty);
        set => SetValue(CanDeleteProperty, value);
    }


    public InstanceMetadataConfigurationViewModel ViewModel { get; }

    private void AddAttachmentHandler(InstanceAttachmentItemModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
            {
                ViewModel.IsAttachmentBeingParsed = true;
                ViewModel.Attachments.Add(model);
            }
            else
            {
                ViewModel.IsAttachmentBeingParsed = false;
            }
        });
    }

    private void AttachmentBox_Loaded(object sender, RoutedEventArgs e)
    {
        Task.Run(() => ViewModel.LoadParseAttachmentsAsync(ViewModel.Context.AssociatedInstance!.Attachments));
    }

    private void AttachmentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItemCount = AttachmentList.SelectedItems.Count;
        CanDelete = AttachmentList.SelectedItems.Count > 0;
    }

    private static void FilterCheckBoxChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var self = (InstanceMetadataConfigurationView)sender;
        filter = x => x.Type switch
        {
            ResourceType.Mod => self.IsModChecked,
            ResourceType.ResourcePack => self.IsResourcepackChecked,
            ResourceType.ShaderPack => self.IsShaderChecked,
            ResourceType.File => self.IsFileChecked,
            _ => true
        };
        self.UpdateFilter();
    }

    private void AttachmentSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) UpdateFilter();
    }

    private void UpdateFilter()
    {
        // 变量会被捕获
        AttachmentSource.Filter = x =>
            (filter?.Invoke((InstanceAttachmentItemModel)x) ?? true) &&
            (string.IsNullOrEmpty(AttachmentSearchBox.Text) ||
             ((InstanceAttachmentItemModel)x).Caption.StartsWith(AttachmentSearchBox.Text,
                 StringComparison.OrdinalIgnoreCase));
        AttachmentSource.RefreshFilter();
    }

    private void OpenReferenceUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var item = (InstanceAttachmentItemModel)button.DataContext;
        var url = item.Reference;
        if (url != null)
            Process.Start(new ProcessStartInfo(url.AbsoluteUri)
            {
                UseShellExecute = true
            });
    }
}