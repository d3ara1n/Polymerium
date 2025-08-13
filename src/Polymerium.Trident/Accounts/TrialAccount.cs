using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts
{
    public class TrialAccount : IAccount
    {
        #region IAccount Members

        public required string Username { get; init; }

        public required string Uuid { get; init; }

        public string AccessToken => "bird_is_the_word";

        public string UserType => "legacy";

        #endregion

        public static TrialAccount CreateStewie() =>
            new() { Username = "Stewie", Uuid = Guid.NewGuid().ToString().Replace("-", string.Empty) };

        public static TrialAccount CreateBrian() =>
            new() { Username = "Brian", Uuid = Guid.NewGuid().ToString().Replace("-", string.Empty) };

        public static TrialAccount CreateChris() =>
            new() { Username = "Chris", Uuid = Guid.NewGuid().ToString().Replace("-", string.Empty) };

        public static TrialAccount CreatePeter() =>
            new() { Username = "Peter", Uuid = Guid.NewGuid().ToString().Replace("-", string.Empty) };

        public static TrialAccount CreateLois() =>
            new() { Username = "Lois", Uuid = Guid.NewGuid().ToString().Replace("-", string.Empty) };
    }
}
