using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions;
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
    public bool IsOperating
    {
        get => (bool)GetValue(IsOperatingProperty);
        set => SetValue(IsOperatingProperty, value);
    }

    // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsOperatingProperty =
        DependencyProperty.Register(nameof(IsOperating), typeof(bool), typeof(SearchDetailDialog),
            new PropertyMetadata(false));

    public SearchDetailDialog(RepositoryAssetMeta resource, GameInstance? scope)
    {
        ViewModel = App.Current.Provider.GetRequiredService<SearchDetailViewModel>();
        // Context 中没有绑定 Instance 则弹出 InstanceSelectorDialog 进行选择。
        // Modpack 安装则不需要选择实例，但要有安装进度
        ViewModel.GotResources(resource, scope);
        InitializeComponent();
    }

    public SearchDetailViewModel ViewModel { get; }

    private readonly CancellationTokenSource source = new();

    private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
    {
        VersionSource.Filter = VersionSourceFilter;
        IsOperating = true;
        Task.Run(() => ViewModel.LoadVersionsAsync(LoadVersionHandler));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void LoadVersionHandler(SearchCenterResultItemVersionModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
                ViewModel.Versions.Add(model);
            else
                IsOperating = false;
        });
    }

    private bool VersionSourceFilter(object obj)
    {
        var file = (SearchCenterResultItemVersionModel)obj;
        if (ViewModel.Scope != null)
        {
            var coreVersion = ViewModel.Scope.GetCoreVersion();
            var isModLoaderSupported = !file.File.SupportedModLoaders.Any() ||
                                       file.File.SupportedModLoaders.Any(x =>
                                           ViewModel.Scope.Metadata.Components.Any(y => x == y.Identity));
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
        if (ViewModel.Resource!.Value.Type == ResourceType.Modpack)
        {
            IsOperating = true;
            await Task.Run(
                () => ViewModel.InstallModpackAsync(ViewModel.SelectedVersion!, ReportProgress, source.Token));
        }
        else
        {
            if (ViewModel.Context.AssociatedInstance != null)
            {
                ViewModel.InstallAsset(ViewModel.Context.AssociatedInstance.Inner, ViewModel.SelectedVersion!);
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
                    ViewModel.InstallAsset(instance, ViewModel.SelectedVersion!);
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