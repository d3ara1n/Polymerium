using Trident.Abstractions;

namespace Polymerium.Trident.Accounts
{
    public class FamilyAccount(string username, string uuid) : IAccount
    {
        public string Username { get; } = username;

        public string Uuid { get; } = uuid;

        public string AccessToken => "invalid";

        public string UserType => "legacy";

        public static FamilyAccount CreateStewie()
        {
            return new("Stewie", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreateBrian()
        {
            return new("Brian", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreatePeter()
        {
            return new("Peter", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreateLois()
        {
            return new("Lois", Guid.NewGuid().ToString());
        }

        public ValueTask<bool> RefreshAsync()
        {
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> ValidateAsync()
        {
            return ValueTask.FromResult(true);
        }
    }
}
