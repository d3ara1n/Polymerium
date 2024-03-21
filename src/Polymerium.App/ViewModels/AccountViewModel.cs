using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Messages;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;

namespace Polymerium.App.ViewModels
{
    public class AccountViewModel : RecipientViewModelBase
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

            Entries = new ObservableCollection<AccountModel>(_accountManager.Managed.Select(x =>
                new AccountModel(x.GetType().Name, x.Username,
                    $"https://starlightskins.lunareclipse.studio/skin-render/default/{x.Username}/face")));
        }

        public ObservableCollection<AccountModel> Entries { get; }

        public ICommand OpenMicrosoftWizardCommand { get; }
        public ICommand OpenAuthlibWizardCommand { get; }

        public void Receive(AccountAddedMessage message)
        {
            if (IsActive)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Entries.Add(new AccountModel(message.Account.GetType().Name, message.Account.Username,
                        $"https://starlightskins.lunareclipse.studio/skin-render/default/{message.Account.Username}/face"));
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
    }
}