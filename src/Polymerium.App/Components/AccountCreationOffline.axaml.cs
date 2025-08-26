using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Polymerium.App.Controls;
using Trident.Core.Accounts;

namespace Polymerium.App.Components
{
    public partial class AccountCreationOffline : AccountCreationStep
    {
        public static readonly DirectProperty<AccountCreationOffline, string> UserNameProperty =
            AvaloniaProperty.RegisterDirect<AccountCreationOffline, string>(nameof(UserName),
                                                                            o => o.UserName,
                                                                            (o, v) => o.UserName = v);

        public static readonly DirectProperty<AccountCreationOffline, string> UuidProperty =
            AvaloniaProperty.RegisterDirect<AccountCreationOffline, string>(nameof(Uuid),
                                                                            o => o.Uuid,
                                                                            (o, v) => o.Uuid = v);

        public static readonly DirectProperty<AccountCreationOffline, string> UuidOverwriteProperty =
            AvaloniaProperty.RegisterDirect<AccountCreationOffline, string>(nameof(UuidOverwrite),
                                                                            o => o.UuidOverwrite,
                                                                            (o, v) => o.UuidOverwrite = v);

        public static readonly DirectProperty<AccountCreationOffline, bool> IsWarnedProperty =
            AvaloniaProperty.RegisterDirect<AccountCreationOffline, bool>(nameof(IsWarned),
                                                                          o => o.IsWarned,
                                                                          (o, v) => o.IsWarned = v);


        public AccountCreationOffline() => InitializeComponent();

        public string UserName
        {
            get;
            set => SetAndRaise(UserNameProperty, ref field, value);
        } = string.Empty;

        public string Uuid
        {
            get;
            set => SetAndRaise(UuidProperty, ref field, value);
        } = string.Empty;

        public string UuidOverwrite
        {
            get;
            set => SetAndRaise(UuidOverwriteProperty, ref field, value);
        } = string.Empty;

        public bool IsWarned
        {
            get;
            set => SetAndRaise(IsWarnedProperty, ref field, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == UserNameProperty)
            {
                var name = change.GetNewValue<string>();
                Uuid = GenerateUuid(name).ToString().Replace("-", string.Empty);
                IsWarned = !name.All(x => x is '_' || char.IsAsciiLetterOrDigit(x));
                IsNextAvailable = !string.IsNullOrEmpty(name);
            }
        }

        private Guid GenerateUuid(string playerName)
        {
            var raw = $"OfflinePlayer:{playerName}";
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
            return new(hash);
        }

        public override object NextStep()
        {
            var account = new OfflineAccount
            {
                Username = UserName,
                Uuid = !string.IsNullOrEmpty(UuidOverwrite) ? UuidOverwrite : Uuid
            };

            return new AccountCreationPreview { Account = account };
        }
    }
}
