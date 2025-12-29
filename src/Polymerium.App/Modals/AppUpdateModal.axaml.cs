using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Velopack;

namespace Polymerium.App.Modals;

public partial class AppUpdateModal : Modal
{
    public static readonly DirectProperty<AppUpdateModal, AppUpdateModel?> ModelProperty =
        AvaloniaProperty.RegisterDirect<AppUpdateModal, AppUpdateModel?>(nameof(Model),
                                                                         o => o.Model,
                                                                         (o, v) => o.Model = v);

    public static readonly DirectProperty<AppUpdateModal, bool> IsDownloadingProperty =
        AvaloniaProperty.RegisterDirect<AppUpdateModal, bool>(nameof(IsDownloading),
                                                              o => o.IsDownloading,
                                                              (o, v) => o.IsDownloading = v);

    public static readonly DirectProperty<AppUpdateModal, int> DownloadProgressProperty =
        AvaloniaProperty.RegisterDirect<AppUpdateModal, int>(nameof(DownloadProgress),
                                                             o => o.DownloadProgress,
                                                             (o, v) => o.DownloadProgress = v);

    public AppUpdateModal() => InitializeComponent();

    #region Properties

    public AppUpdateModel? Model
    {
        get;
        set => SetAndRaise(ModelProperty, ref field, value);
    }

    public bool IsDownloading
    {
        get;
        set => SetAndRaise(IsDownloadingProperty, ref field, value);
    }

    public int DownloadProgress
    {
        get;
        set => SetAndRaise(DownloadProgressProperty, ref field, value);
    }

    #endregion

    #region Services

    public required UpdateManager UpdateManager { get; init; }
    public required NotificationService NotificationService { get; init; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ConfirmUpdateAsync()
    {
        if (Model == null)
        {
            return;
        }

        IsDownloading = true;
        DownloadProgress = 0;

        try
        {
            void Report(int value) => Dispatcher.UIThread.Post(() => DownloadProgress = value);

            await UpdateManager.DownloadUpdatesAsync(Model.Update, Report);

            // 下载完成后直接重启应用更新
            Program.Terminate(() => UpdateManager.ApplyUpdatesAndRestart(Model.Update));
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            NotificationService.PopMessage(ex, "Failed to download update");
        }
    }

    #endregion
}
