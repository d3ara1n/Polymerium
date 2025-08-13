using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts
{
    public class MicrosoftAccount : IAccount
    {
        public required string RefreshToken { get; set; }

        #region IAccount Members

        public required string Username { get; init; }

        public required string Uuid { get; init; }
        public required string AccessToken { get; set; }

        public string UserType => "msa";

        #endregion
    }
}
