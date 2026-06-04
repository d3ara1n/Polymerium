using System;
using System.Threading.Tasks;
using Polymerium.App.Exceptions;
using Polymerium.App.Utilities;
using TridentCore.Abstractions.Accounts;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.App.Services;

public class InstanceService
{
    private readonly InstanceManager _instanceManager;
    private readonly ProfileManager _profileManager;
    private readonly ConfigurationService _configurationService;
    private readonly PersistenceService _persistenceService;

    public InstanceService(
        InstanceManager instanceManager,
        ProfileManager profileManager,
        ConfigurationService configurationService,
        PersistenceService persistenceService
    )
    {
        _instanceManager = instanceManager;
        _profileManager = profileManager;
        _configurationService = configurationService;
        _persistenceService = persistenceService;
        instanceManager.AccountUpdated += OnAccountUpdated;
    }

    private void OnAccountUpdated(object? sender, IAccount account)
    {
        _persistenceService.UpdateAccount(account.Uuid, AccountHelper.ToRaw(account));
    }

    public void DeployAndLaunch(string key, LaunchMode mode)
    {
        var selector = _persistenceService.GetAccountSelector(key);
        if (selector != null)
        {
            var account = _persistenceService.GetAccount(selector.Uuid);
            if (account != null)
            {
                var cooked = AccountHelper.ToCooked(account);
                _persistenceService.UseAccount(account.Uuid);
                var profile = _profileManager.GetImmutable(key);
                var locator = CreateJavaLocator(profile, _configurationService.Value);
                var deploy = new DeployOptions(
                    profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false),
                    profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, false),
                    false
                );
                var launch = new LaunchOptions(
                    additionalArguments: profile.GetOverride(
                        Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                        _configurationService.Value.GameJavaAdditionalArguments
                    ),
                    maxMemory: profile.GetOverride(
                        Profile.OVERRIDE_JAVA_MAX_MEMORY,
                        _configurationService.Value.GameJavaMaxMemory
                    ),
                    windowSize: (
                        profile.GetOverride(
                            Profile.OVERRIDE_WINDOW_WIDTH,
                            _configurationService.Value.GameWindowInitialWidth
                        ),
                        profile.GetOverride(
                            Profile.OVERRIDE_WINDOW_HEIGHT,
                            _configurationService.Value.GameWindowInitialHeight
                        )
                    ),
                    quickConnectAddress: profile.GetOverride<string>(
                        Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER
                    ),
                    commandWrapperTemplate: profile.GetOverride(
                        Profile.OVERRIDE_BEHAVIOR_COMMAND_WRAPPER,
                        _configurationService.Value.GameCommandWrapper
                    ),
                    launchMode: mode,
                    account: cooked,
                    brand: Program.Brand
                );
                _instanceManager.DeployAndLaunch(key, deploy, launch, locator);
            }
        }
        else
        {
            throw new AccountNotFoundException("Account is not provided or removed after set");
        }
    }

    public void Deploy(
        string key,
        bool? fastMode = null,
        bool? resolveDependency = null,
        bool? fullCheckMode = null
    )
    {
        var profile = _profileManager.GetImmutable(key);
        fastMode ??= profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false);
        resolveDependency ??= profile.GetOverride(
            Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY,
            false
        );
        fullCheckMode ??= false;
        var locator = CreateJavaLocator(profile, _configurationService.Value);
        _instanceManager.Deploy(key, new(fastMode, resolveDependency, fullCheckMode), locator);
    }

    private static JavaHomeLocatorDelegate CreateJavaLocator(
        Profile profile,
        Configuration configuration
    ) =>
        JavaHelper.MakeLocator(major =>
            profile.GetOverride(
                Profile.OVERRIDE_JAVA_HOME,
                major switch
                {
                    8 => configuration.RuntimeJavaHome8,
                    11 => configuration.RuntimeJavaHome11,
                    16 or 17 => configuration.RuntimeJavaHome17,
                    21 => configuration.RuntimeJavaHome21,
                    24 or 25 => configuration.RuntimeJavaHome25,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(major),
                        major,
                        $"Unsupported java version: {major}"
                    ),
                }
            )
        );
}
