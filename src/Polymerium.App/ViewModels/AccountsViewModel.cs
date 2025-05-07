﻿using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using Trident.Abstractions.Accounts;

namespace Polymerium.App.ViewModels;

public partial class AccountsViewModel(
    OverlayService overlayService,
    NotificationService notificationService,
    PersistenceService persistenceService,
    ConfigurationService configurationService) : ViewModelBase
{
    #region Direct

    public ObservableCollection<AccountModel> Accounts { get; } = [];

    #endregion

    private bool Finish(IAccount account)
    {
        var found = persistenceService.GetAccount(account.Uuid);
        if (found != null)
        {
            notificationService.PopMessage("Account with the same uuid already exists",
                                           "Account creation failed",
                                           NotificationLevel.Danger);
            return false;
        }

        var raw = AccountHelper.ToRaw(account, DateTimeOffset.Now, null, false);
        persistenceService.AppendAccount(raw);
        Accounts.Add(new AccountModel(account.GetType(), account.Uuid, account.Username, DateTimeOffset.Now, null));
        return true;
    }

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        var defaultAccount = persistenceService.GetDefaultAccount();
        foreach (var account in persistenceService.GetAccounts())
        {
            var cooked = AccountHelper.ToCooked(account);
            var model = new AccountModel(cooked.GetType(),
                                         cooked.Uuid,
                                         cooked.Username,
                                         account.EnrolledAt,
                                         account.LastUsedAt) { IsDefault = account.Uuid == defaultAccount?.Uuid };
            Accounts.Add(model);
        }


        return base.OnInitializedAsync(token);
    }

    #region Commands

    [RelayCommand]
    private void CreateAccount()
    {
        overlayService.PopModal(new AccountCreationModal
        {
            FinishCallback = Finish,
            IsOfflineAvailable = configurationService.Value.ApplicationSuperPowerActivated
                              || persistenceService.HasMicrosoftAccount()
        });
    }

    [RelayCommand]
    private void ViewAccount(AccountModel? model)
    {
        if (model != null)
            overlayService.PopModal(new AccountEntryModal { DataContext = model });
    }

    [RelayCommand]
    private void MarkAsDefaultAccount(AccountModel? model)
    {
        if (model != null)
        {
            persistenceService.MarkDefaultAccount(model.Uuid);
            foreach (var account in Accounts)
                account.IsDefault = account.Uuid == model.Uuid;
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