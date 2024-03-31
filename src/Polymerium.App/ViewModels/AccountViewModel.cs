using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Polymerium.App.Extensions;
using Polymerium.App.Messages;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;

namespace Polymerium.App.ViewModels
{
    public class AccountViewModel : RecipientViewModelBase, IRecipient<AccountAddedMessage>,
        IRecipient<AccountRemovedMessage>
    {
        private readonly AccountManager _accountManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly DispatcherQueue _dispatcher;
        private readonly ModalService _modalService;

        public AccountViewModel(ModalService modalService, IHttpClientFactory clientFactory,
            AccountManager accountManger)
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            _modalService = modalService;
            _clientFactory = clientFactory;
            _accountManager = accountManger;
            OpenMicrosoftWizardCommand = new RelayCommand(OpenMicrosoftWizard, CanOpenMicrosoftWizard);
            OpenAuthlibWizardCommand = new RelayCommand(OpenAuthlibWizard, CanOpenAuthlibWizard);
            AddFamilyGuyAccountCommand = new RelayCommand<string>(AddFamilyGuyAccount, CanAddFamilyGuyAccount);
            SetAsDefaultEntryCommand = new RelayCommand<AccountModel>(SetAsDefaultEntry, CanSetAsDefaultEntry);
            RemoveEntryCommand = new RelayCommand<AccountModel>(RemoveEntry, CanRemoveEntry);

            DefaultUuid = _accountManager.ToBindable(x => x.DefaultUuid, (x, v) => x.DefaultUuid = v);

            Entries = new ObservableCollection<AccountModel>(_accountManager.Managed.Select(x =>
                new AccountModel(x, DefaultUuid, SetAsDefaultEntryCommand, RemoveEntryCommand)));
        }

        public ObservableCollection<AccountModel> Entries { get; }

        public ICommand OpenMicrosoftWizardCommand { get; }
        public ICommand OpenAuthlibWizardCommand { get; }
        public ICommand AddFamilyGuyAccountCommand { get; }
        public ICommand SetAsDefaultEntryCommand { get; }
        public ICommand RemoveEntryCommand { get; }

        public Bindable<AccountManager, string?> DefaultUuid { get; }

        public void Receive(AccountAddedMessage message)
        {
            if (IsActive)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Entries.Add(new AccountModel(message.Account, DefaultUuid, SetAsDefaultEntryCommand,
                        RemoveEntryCommand));
                });
            }
        }

        public void Receive(AccountRemovedMessage message)
        {
            if (IsActive)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    AccountModel? found = Entries.FirstOrDefault(x => x.Inner.Uuid == message.Account.Uuid);
                    if (found != null)
                    {
                        Entries.Remove(found);
                    }
                });
            }
        }

        public override bool OnAttached(object? parameter)
        {
            IsActive = true;
            return base.OnAttached(parameter);
        }

        public override void OnDetached()
        {
            IsActive = false;
            base.OnDetached();
        }

        public bool CanOpenMicrosoftWizard()
        {
            return true;
        }

        public void OpenMicrosoftWizard()
        {
            MicrosoftAccountWizardModal modal = new(_clientFactory, _accountManager);
            _modalService.Pop(modal);
        }

        public bool CanOpenAuthlibWizard()
        {
            return false;
        }

        public void OpenAuthlibWizard()
        {
        }

        public bool CanSetAsDefaultEntry(AccountModel? model)
        {
            return model != null && !model.IsDefault.Value;
        }

        public void SetAsDefaultEntry(AccountModel? model)
        {
            if (model != null)
            {
                DefaultUuid.Value = model.Inner.Uuid;
            }
        }

        public bool CanRemoveEntry(AccountModel? model)
        {
            return model != null;
        }

        public void RemoveEntry(AccountModel? model)
        {
            if (model != null)
            {
                _accountManager.Remove(model.Inner.Uuid);
            }
        }

        public bool CanAddFamilyGuyAccount(string? who)
        {
            return !string.IsNullOrEmpty(who) && new[] { "Stewie", "Brian", "Peter", "Lois" }.Contains(who);
        }

        public void AddFamilyGuyAccount(string? who)
        {
            FamilyAccount account = who switch
            {
                "Stewie" => FamilyAccount.CreateStewie(),
                "Brian" => FamilyAccount.CreateBrian(),
                "Peter" => FamilyAccount.CreatePeter(),
                "Lois" => FamilyAccount.CreateLois(),
                _ => throw new NotImplementedException()
            };
            _accountManager.Append(account);
        }
    }
}