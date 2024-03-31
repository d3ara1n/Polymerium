using Polymerium.App.Extensions;
using Polymerium.App.Models;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Launching;
using Polymerium.Trident.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Trident.Abstractions;

namespace Polymerium.App.Services
{
    public class InstanceService(
        InstanceStatusService instanceStatusService,
        InstanceManager instanceManager,
        ProfileManager profileManager,
        NotificationService notificationService,
        AccountManager accountManager,
        TridentContext trident)
    {
        public bool CanManipulate(string key)
        {
            var status = instanceStatusService.MustHave(key);
            return status is { State.Value: InstanceState.Idle or InstanceState.Stopped };
        }

        public void Deploy(string key, Action? onSuccess = null)
        {
            var profile = profileManager.GetProfile(key);
            if (profile == null)
            {
                throw new KeyNotFoundException($"The key {key} is not found in managed profiles");
            }

            instanceManager.Deploy(key, profile, null, onSuccess, App.Current.Token);
        }

        public void Launch(string key, Action? onSuccess = null)
        {
            var profile = profileManager.GetProfile(key);
            if (profile == null)
            {
                throw new KeyNotFoundException($"The key {key} is not found in managed profiles");
            }

            var builder = LaunchOptions.Builder();
            builder
                .WithWindowSize(new Size((int)profile.GetOverriddenWindowWidth(),
                    (int)profile.GetOverriddenWindowHeight()))
                .WithMaxMemory(profile.GetOverriddenJvmMaxMemory())
                .WithAdditionalArguments(profile.GetOverriddenJvmAdditionalArguments())
                .WithJavaHomeLocator(major =>
                {
                    var home = profile.GetOverriddenJvmHome(major);
                    if (!Directory.Exists(home))
                    {
                        throw new JavaNotFoundException(major);
                    }

                    return home;
                })
                .Managed();
            if (!string.IsNullOrEmpty(profile.AccountId) &&
                accountManager.TryGetByUuid(profile.AccountId, out var account))
            {
                instanceManager.Launch(key, profile, account, builder.Build(), onSuccess, App.Current.Token);
            }
            else
            {
                throw new AccountNotFoundException();
            }
        }

        public void DeployAndLaunchSafelyBecauseThisIsUiPackageAndHasTheAblityToSendTheErrorBackToTheUiLayer(string key)
        {
            try
            {
                Deploy(key, () =>
                {
                    try
                    {
                        Launch(key);
                    }
                    catch (Exception e)
                    {
                        notificationService.PopError($"Launching aborted: {e.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                notificationService.PopError($"Deploying aborted: {e.Message}");
            }
        }

        public void Reset(string key)
        {
            var home = trident.InstanceHomePath(key);
            if (Directory.Exists(home))
            {
                Directory.Delete(home, true);
            }
        }

        public void Delete(string key)
        {
            var home = trident.InstanceHomePath(key);
            if (Directory.Exists(home))
            {
                Directory.Delete(home, true);
            }

            var file = trident.InstanceProfilePath(key);
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            profileManager.Discard(key);
        }
    }
}