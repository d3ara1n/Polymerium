using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Accounts;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.PageModels;

public partial class AccountsPageModel(
    OverlayService overlayService,
    NotificationService notificationService,
    PersistenceService persistenceService,
    ConfigurationService configurationService,
    MicrosoftService microsoftService,
    XboxLiveService xboxLiveService,
    MinecraftService minecraftService,
    YggdrasilService yggdrasilService) : ViewModelBase
{
    #region Direct

    public ObservableCollection<AccountModel> Accounts { get; } = [];

    #endregion

    #region Other

    private bool Finish(IAccount account)
    {
        var found = persistenceService.GetAccount(account.Uuid);
        if (found != null)
        {
            notificationService.PopMessage(Resources.AccountsPage_AccountAddingDangerNotificationMessage,
                                           Resources.AccountsPage_AccountAddingDangerNotificationTitle,
                                           GrowlLevel.Danger,
                                           thumbnail: AccountHelper.GetFaceUrl(AccountHelper.BuildSkinSource(account)));
            return false;
        }

        var isDefault = persistenceService.GetDefaultAccount() == null;
        var enrolledAt = DateTimeOffset.Now;
        var raw = AccountHelper.ToRaw(account, enrolledAt, null, isDefault);
        persistenceService.AppendAccount(raw);
        if (isDefault)
        {
            persistenceService.MarkDefaultAccount(account.Uuid);
        }

        var model = AccountHelper.CreateModelFromAccount(account, enrolledAt);
        model.IsDefault = isDefault;
        Accounts.Add(model);
        return true;
    }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        var defaultAccount = persistenceService.GetDefaultAccount();
        foreach (var account in persistenceService.GetAccounts())
        {
            var cooked = AccountHelper.ToCooked(account);
            var model = AccountHelper.CreateModelFromAccount(cooked,
                                                             DateTimeHelper
                                                                .FromPersistedLocalDateTime(account.EnrolledAt),
                                                             DateTimeHelper
                                                                .FromPersistedLocalDateTime(account.LastUsedAt));
            model.IsDefault = account.Uuid == defaultAccount?.Uuid;
            Accounts.Add(model);
        }

        return base.OnInitializeAsync(token);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void CreateAccount() =>
        overlayService.PopModal(new AccountCreationModal
        {
            FinishCallback = Finish,
            IsOfflineAvailable =
                configurationService.Value.ApplicationSuperPowerActivated
             || persistenceService.HasMicrosoftAccount(),
            MicrosoftService = microsoftService,
            XboxLiveService = xboxLiveService,
            MinecraftService = minecraftService,
            NotificationService = notificationService,
            YggdrasilService = yggdrasilService
        });

    [RelayCommand]
    private void ViewAccount(AccountModel? model)
    {
        if (model != null)
        {
            overlayService.PopModal(new AccountEntryModal { DataContext = model });
        }
    }

    [RelayCommand]
    private void MarkAsDefaultAccount(AccountModel? model)
    {
        if (model != null)
        {
            persistenceService.MarkDefaultAccount(model.Uuid);
            foreach (var account in Accounts)
            {
                account.IsDefault = account.Uuid == model.Uuid;
            }
        }
    }

    [RelayCommand]
    private void RemoveAccount(AccountModel? model)
    {
        if (model != null)
        {
            persistenceService.RemoveAccount(model.Uuid);
            Accounts.Remove(model);
        }
    }

    #endregion
}
