using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;
using Polymerium.Core.Resources;

namespace Polymerium.App.Views;

public sealed partial class SearchDetailDialog : CustomDialog
{
    public SearchDetailDialog(RepositoryAssetMeta resource)
    {
        ViewModel = App.Current.Provider.GetRequiredService<SearchDetailViewModel>();
        // Context 中没有绑定 Instance 则弹出 InstanceSelectorDialog 进行选择。
        // Modpack 安装则不需要选择实例，但要有安装进度
        ViewModel.GotResource(resource);
        InitializeComponent();
    }

    public SearchDetailViewModel ViewModel { get; }
}