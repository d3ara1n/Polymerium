using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Polymerium.App.Models;
using Velopack;
using VelopackExtension.MirrorChyan.Sources;

namespace Polymerium.App.Services;

public partial class UpdateService(
    ConfigurationService configurationService,
    NotificationService notificationService,
    UpdateManager updateManager,
    IOptions<MirrorChyanSourceOptions> mirrorChyanSourceOptions
)
{
    private Action<AppUpdateModel?>? _handler;

    public bool IsAvailable => updateManager.IsInstalled || Program.IsDebug;

    public AppUpdateState UpdateState { get; private set; } =
        updateManager.IsInstalled || Program.IsDebug
            ? AppUpdateState.Idle
            : AppUpdateState.Unavailable;

    public AppUpdateModel? CurrentUpdate { get; private set; }

    public bool IsUpdateChecked { get; private set; }

    public bool IsChecking { get; private set; }

    public void SetHandler(Action<AppUpdateModel?>? handler) => _handler = handler;

    public async Task CheckUpdateAsync(bool silent = false)
    {
        if (IsChecking)
        {
            return;
        }

        if (!IsAvailable)
        {
            CurrentUpdate = null;
            IsUpdateChecked = false;
            UpdateState = AppUpdateState.Unavailable;
            _handler?.Invoke(null);
            return;
        }

        IsChecking = true;

        try
        {
            ApplySourceConfiguration();

            var result = await updateManager.CheckForUpdatesAsync();
            if (result != null)
            {
                CurrentUpdate = new(result);
                UpdateState = AppUpdateState.Found;
            }
            else
            {
                CurrentUpdate = null;
                UpdateState = AppUpdateState.Latest;
            }

            IsUpdateChecked = true;
            _handler?.Invoke(CurrentUpdate);
        }
        catch (Exception ex)
        {
            if (!IsUpdateChecked)
            {
                UpdateState = AppUpdateState.Idle;
            }

            if (!silent)
            {
                notificationService.PopMessage(ex, "Failed to check updates");
            }
        }
        finally
        {
            IsChecking = false;
        }
    }

    private void ApplySourceConfiguration()
    {
        var cdk = configurationService.Value.UpdateMirrorChyanCdk;
        mirrorChyanSourceOptions.Value.Cdk = !string.IsNullOrEmpty(cdk)
            ? cdk
            : Program.MirrorChyanCdk;
    }
}
