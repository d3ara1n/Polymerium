using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels.AddAccountWizard
{
    public class OfflineAccountViewModel : ObservableValidator
    {
        private AccountManager _accountManager;

        private string errorMessage;
        public string ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }

        private string uuid;
        [RegularExpression("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$")]
        public string UUID { get => uuid; set => SetProperty(ref uuid, value, true); }

        private string nickname;
        [RegularExpression("^[a-zA-Z0-9_]{3,16}$")]
        [Required]
        public string Nickname { get => nickname; set => SetProperty(ref nickname, value, true); }

        public string EmptyUUID { get; private set; } = Guid.NewGuid().ToString();

        public OfflineAccountViewModel(AccountManager accountManager)
        {
            _accountManager = accountManager;
            Nickname = string.Empty;
            UUID = string.Empty;
        }

        public void Register()
        {
            var account = new OfflineAccount()
            {
                Id = Guid.NewGuid().ToString(),
                Nickname = Nickname,
                UUID = string.IsNullOrEmpty(UUID) ? EmptyUUID : UUID
            };
            _accountManager.AddAccount(account);
        }
    }
}
