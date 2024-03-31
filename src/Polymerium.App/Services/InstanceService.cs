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
            InstanceStatusModel status = instanceStatusService.MustHave(key);
            return status is { State.Value: InstanceState.Idle or InstanceState.Stopped };
        }

        public void Deploy(string key, Action? onSuccess = null)
        {
            Profile? profile = profileManager.GetProfile(key);
            if (profile == null)
            {
                throw new KeyNotFoundException($"The key {key} is not found in managed profiles");
            }

            instanceManager.Deploy(key, profile, null, onSuccess, App.Current.Token);
        }

        public void Launch(string key, Action? onSuccess = null)
        {
            Profile? profile = profileManager.GetProfile(key);
            if (profile == null)
            {
                throw new KeyNotFoundException($"The key {key} is not found in managed profiles");
            }

            LaunchOptionsBuilder builder = LaunchOptions.Builder();
            builder
                .WithWindowSize(new Size((int)profile.GetOverriddenWindowWidth(),
                    (int)profile.GetOverriddenWindowHeight()))
                .WithMaxMemory(profile.GetOverriddenJvmMaxMemory())
                .WithAdditionalArguments(profile.GetOverriddenJvmAdditionalArguments())
                .WithJavaHomeLocator(major =>
                {
                    string home = profile.GetOverriddenJvmHome(major);
                    if (!Directory.Exists(home))
                    {
                        throw new JavaNotFoundException(major);
                    }

                    return home;
                })
                .Managed();
            if (!string.IsNullOrEmpty(profile.AccountId) &&
                accountManager.TryGetByUuid(profile.AccountId, out IAccount? account))
            {
                instanceManager.Launch(key, profile, account, builder.Build(), onSuccess, App.Current.Token);
            }
            else
            {
                throw new AccountNotFoundException();
            }
        }

        public void LaunchSafelyBecauseThisIsUiPackageAndHasTheAblityToSendTheErrorBackToTheUiLayer(string key)
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
            string home = trident.InstanceHomePath(key);
            if (Directory.Exists(home))
            {
                Directory.Delete(home, true);
            }
        }

        public void Delete(string key)
        {
            string home = trident.InstanceHomePath(key);
            if (Directory.Exists(home))
            {
                Directory.Delete(home, true);
            }

            string file = trident.InstanceProfilePath(key);
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            profileManager.Discard(key);
        }
    }
}