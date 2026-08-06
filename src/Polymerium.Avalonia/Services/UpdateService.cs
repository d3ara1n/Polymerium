using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Polymerium.Avalonia.Models;
using TridentCore.Abstractions.Lifetimes;
using Velopack;
using VelopackExtension.MirrorChyan.Sources;

namespace Polymerium.Avalonia.Services;

public class UpdateService(
    ConfigurationService configurationService,
    UpdateManager updateManager,
    IOptions<MirrorChyanSourceOptions> mirrorChyanSourceOptions) : ILifetimeService
{
    public bool IsAvailable => updateManager.IsInstalled || Program.IsDebug;

    public bool CanCheckUpdate => IsAvailable && !IsChecking;

    public AppUpdateState UpdateState { get; private set; } = updateManager.IsInstalled || Program.IsDebug
                                                                  ? AppUpdateState.Idle
                                                                  : AppUpdateState.Unavailable;

    public AppUpdateModel? CurrentUpdate { get; private set; }

    public bool IsUpdateChecked { get; private set; }

    public bool IsChecking { get; private set; }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (!configurationService.Value.UpdateAutoCheck)
        {
            return;
        }

        try
        {
            await CheckUpdateAsync();
            if (UpdateState == AppUpdateState.Found && CurrentUpdate is { } update)
            {
                UpdateFound?.Invoke(update);
            }
        }
        catch (Exception)
        {
            // slient
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <summary>
    ///     自动检查（启动时）发现新版本时触发；手动检查走各自的调用方反馈，不经过此事件。
    /// </summary>
    public event Action<AppUpdateModel>? UpdateFound;

    public async Task CheckUpdateAsync()
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
        }
        catch (Exception)
        {
            if (!IsUpdateChecked)
            {
                UpdateState = AppUpdateState.Idle;
            }

            throw;
        }
        finally
        {
            IsChecking = false;
        }
    }

    private void ApplySourceConfiguration()
    {
        var cdk = configurationService.Value.UpdateMirrorChyanCdk;
        mirrorChyanSourceOptions.Value.Cdk = !string.IsNullOrEmpty(cdk) ? cdk : Program.MirrorChyanCdk;
    }
}
