using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Polymerium.App.Exceptions;
using Polymerium.App.Utilities;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Igniters;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Services
{
    public class InstanceService(
        InstanceManager instanceManager,
        ProfileManager profileManager,
        ConfigurationService configurationService,
        PersistenceService persistenceService,
        MinecraftService minecraftService,
        XboxLiveService xboxLiveService,
        MicrosoftService microsoftService)
    {
        public async Task DeployAndLaunchAsync(string key, LaunchMode mode)
        {
            var selector = persistenceService.GetAccountSelector(key);
            if (selector != null)
            {
                var account = persistenceService.GetAccount(selector.Uuid);
                if (account != null)
                {
                    var cooked = AccountHelper.ToCooked(account);

                    if (cooked is MicrosoftAccount msa)
                    {
                        try
                        {
                            _ = await minecraftService.AcquireAccountProfileByMinecraftTokenAsync(msa.AccessToken);
                        }
                        catch (ApiException ex)
                        {
                            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                var microsoft = await microsoftService.RefreshUserAsync(msa.RefreshToken);
                                var xbox =
                                    await xboxLiveService
                                       .AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(microsoft.AccessToken);
                                var xsts =
                                    await xboxLiveService.AuthorizeForServiceTokenByXboxLiveTokenAsync(xbox.Token);
                                var minecraft =
                                    await minecraftService.AuthenticateByXboxLiveServiceTokenAsync(xsts.Token,
                                        xsts.DisplayClaims.Xui.First().Uhs);

                                msa.AccessToken = minecraft.AccessToken;
                                msa.RefreshToken = microsoft.RefreshToken;
                                persistenceService.UpdateAccount(account.Uuid, AccountHelper.ToRaw(msa));
                            }
                            else
                            {
                                throw new AccountInvalidException(ex.Message, ex);
                            }
                        }
                    }

                    persistenceService.UseAccount(account.Uuid);
                    var profile = profileManager.GetImmutable(key);
                    // Profile 的引用会被捕获，也就是在 Deploy 期间修改 OVERRIDE_JAVA_HOME 也会产生影响
                    // Full Check Mode 只有在检查文件完整性时为 true，不随用户决定
                    var locator = JavaHelper.MakeLocator(profile, configurationService.Value);
                    var deploy =
                        new DeployOptions(profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false),
                                          profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, false),
                                          false);
                    var launch = new LaunchOptions(additionalArguments:
                                                   profile.GetOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS,
                                                                       configurationService.Value
                                                                          .GameJavaAdditionalArguments),
                                                   maxMemory: profile.GetOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                                                       configurationService.Value.GameJavaMaxMemory),
                                                   windowSize: (profile.GetOverride(Profile.OVERRIDE_WINDOW_WIDTH,
                                                                    configurationService.Value
                                                                       .GameWindowInitialWidth),
                                                                profile.GetOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                                                                    configurationService.Value
                                                                       .GameWindowInitialHeight)),
                                                   launchMode: mode,
                                                   account: cooked,
                                                   brand: Program.BRAND);
                    instanceManager.DeployAndLaunch(key, deploy, launch, locator);
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
            bool? fullCheckMode = null)
        {
            var profile = profileManager.GetImmutable(key);
            fastMode ??= profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, false);
            resolveDependency ??= profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, false);
            fullCheckMode ??= false;
            var locator = JavaHelper.MakeLocator(profile, configurationService.Value);
            instanceManager.Deploy(key, new(fastMode, resolveDependency, fullCheckMode), locator);
        }
    }
}
