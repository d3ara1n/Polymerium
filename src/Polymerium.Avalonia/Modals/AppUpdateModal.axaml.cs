using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Velopack;

namespace Polymerium.Avalonia.Modals;

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModelProperty || change.Property == IsDownloadingProperty)
        {
            ConfirmUpdateCommand.NotifyCanExecuteChanged();
        }
    }

    #region Commands

    private bool CanConfirmUpdate() =>
        !IsDownloading && UpdateManager is not null && !string.IsNullOrWhiteSpace(Model?.Update.TargetFullRelease.FileName);

    [RelayCommand(CanExecute = nameof(CanConfirmUpdate))]
    private async Task ConfirmUpdateAsync()
    {
        if (!CanConfirmUpdate() || Model is not { } model || UpdateManager is not { } updateManager)
        {
            return;
        }

        IsDownloading = true;
        DownloadProgress = 0;

        try
        {
            void Report(int value) => Dispatcher.UIThread.Post(() => DownloadProgress = value);

            await updateManager.DownloadUpdatesAsync(model.Update, Report);

            await Program.TerminateAsync(() => updateManager.ApplyUpdatesAndRestart(model.Update));
        }
        catch (Exception ex)
        {
            NotificationService.PopMessage(ex, LanguageManager.Instance.AppUpdateModal_DownloadUpdateDangerNotificationTitle.Current());
        }
        finally
        {
            IsDownloading = false;
        }
    }

    #endregion

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

    public UpdateManager? UpdateManager { get; init; }
    public required NotificationService NotificationService { get; init; }

    #endregion
}
