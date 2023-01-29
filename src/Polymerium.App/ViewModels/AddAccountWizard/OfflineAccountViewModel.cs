using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Polymerium.App.ViewModels.AddAccountWizard
{
    public class OfflineAccountViewModel : ObservableValidator
    {
        private readonly AccountManager _accountManager;
        private readonly MD5 md5 = MD5.Create();
        private string errorMessage;
        public string ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }

        private string uuid;

        [RegularExpression("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$")]
        public string UUID { get => uuid; set => SetProperty(ref uuid, value, true); }

        private string nickname;

        [RegularExpression("^[a-zA-Z0-9_]{3,16}$")]
        [Required]
        public string Nickname
        {
            get => nickname; set
            {
                SetProperty(ref nickname, value, true);
                EmptyUUID = NameUUIDFromNickname(value == null ? string.Empty : value);
            }
        }

        public string EmptyUUID { get; private set; } = Guid.NewGuid().ToString();

        public OfflineAccountViewModel(AccountManager accountManager)
        {
            _accountManager = accountManager;
            Nickname = "Steve";
            UUID = string.Empty;
        }

        private string NameUUIDFromNickname(string nickname)
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"OfflinePlayer::{nickname}"));
            hash[6] &= 0x0f;
            hash[6] |= 0x30;
            hash[8] &= 0x3f;
            hash[8] |= 0x80;
            string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            return hex.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
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