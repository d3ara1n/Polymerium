using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using Polymerium.Core.Extensions;
using Polymerium.Core.Resources;

namespace Polymerium.App.Views;

public sealed partial class SearchDetailDialog : CustomDialog
{
    // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOperatingProperty =
        DependencyProperty.Register(nameof(IsOperating), typeof(bool), typeof(SearchDetailDialog),
            new PropertyMetadata(false));

    private readonly CancellationTokenSource source = new();

    public SearchDetailDialog(RepositoryAssetMeta resource, GameInstanceModel? scope)
    {
        ViewModel = App.Current.Provider.GetRequiredService<SearchDetailViewModel>();
        ViewModel.GotResources(resource, scope);
        InitializeComponent();
    }

    public bool IsOperating
    {
        get => (bool)GetValue(IsOperatingProperty);
        set => SetValue(IsOperatingProperty, value);
    }

    public SearchDetailViewModel ViewModel { get; }

    public ObservableCollection<SearchCenterResultItemVersionModel> Versions { get; } = new();

    public ObservableCollection<SearchCenterResultItemScreenshotModel> Screenshots { get; } = new();

    private async void CustomDialog_Loaded(object sender, RoutedEventArgs e)
    {
        VersionSource.Filter = VersionSourceFilter;
        VersionSource.SortDescriptions.Add(new SortDescription("ReleaseDateTime", SortDirection.Descending));
        IsOperating = true;
        await DescriptionPresenter.EnsureCoreWebView2Async();
        await Task.Run(() => ViewModel.LoadInfoAsync(LaodInfoHandler, LoadVersionHandler));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void LaodInfoHandler(string description, IEnumerable<SearchCenterResultItemScreenshotModel> screenshots)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            DescriptionPresenter.NavigateToString(description);
            foreach (var screenshot in screenshots) Screenshots.Add(screenshot);
        });
    }

    private void LoadVersionHandler(SearchCenterResultItemVersionModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
            {
                Versions.Add(model);
                VersionList.SelectedItem ??= model;
            }
            else
                IsOperating = false;
        });
    }

    private bool VersionSourceFilter(object obj)
    {
        var file = (SearchCenterResultItemVersionModel)obj;
        if (ViewModel.Scope != null)
        {
            var coreVersion = ViewModel.Scope.Inner.GetCoreVersion();
            var isModLoaderSupported = !file.File.SupportedModLoaders.Any() ||
                                       file.File.SupportedModLoaders.Any(x =>
                                           ViewModel.Scope.Components.Any(y =>
                                               x == ViewModel.GetModloaderFriendlyName(y.Identity)));
            var isVersionSupported = !file.File.SupportedCoreVersions.Any() || coreVersion == null ||
                                     file.File.SupportedCoreVersions.Contains(coreVersion);
            return isModLoaderSupported && isVersionSupported;
        }

        return true;
    }

    protected override void OnDismiss()
    {
        source.Cancel();
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var model = (SearchCenterResultItemVersionModel)VersionList.SelectedValue;
        if (ViewModel.Resource!.Value.Type == ResourceType.Modpack)
        {
            IsOperating = true;
            await Task.Run(
                () => ViewModel.InstallModpackAsync(model, ReportProgress, source.Token));
        }
        else
        {
            if (ViewModel.Scope != null)
            {
                ViewModel.InstallAsset(ViewModel.Scope, model);
                Dismiss();
            }
            else
            {
                var selector = new InstanceSelectorDialog(ViewModel.GetGameInstances())
                {
                    XamlRoot = XamlRoot
                };
                var result = await selector.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var instance = selector.SelectedInstance;
                    ViewModel.InstallAsset(instance, model);
                    Dismiss();
                }
            }
        }
    }

    private void ReportProgress(double? progressPercent, bool finished = false)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (finished)
            {
                IsOperating = false;
                Dismiss();
            }
            else
            {
                if (progressPercent.HasValue)
                {
                    Progress.IsIndeterminate = false;
                    Progress.Value = progressPercent.Value * 100;
                }
                else
                {
                    Progress.IsIndeterminate = true;
                }
            }
        });
    }
}