using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts
{
    public class OfflineAccount : IAccount
    {
        #region IAccount Members

        public required string Username { get; init; }
        public required string Uuid { get; init; }
        public string AccessToken => "rand(32)";
        public string UserType => "legacy";

        #endregion
    }
}
