using Trident.Abstractions;

namespace Polymerium.Trident.Accounts
{
    public class FamilyAccount(string username, string uuid) : IAccount
    {
        public string Username { get; } = username;

        public string Uuid { get; } = uuid;

        public string AccessToken => "invalid";

        public string UserType => "legacy";

        public ValueTask<bool> RefreshAsync()
        {
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> ValidateAsync()
        {
            return ValueTask.FromResult(true);
        }

        public static FamilyAccount CreateStewie()
        {
            return new FamilyAccount("Stewie", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreateBrian()
        {
            return new FamilyAccount("Brian", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreatePeter()
        {
            return new FamilyAccount("Peter", Guid.NewGuid().ToString());
        }

        public static FamilyAccount CreateLois()
        {
            return new FamilyAccount("Lois", Guid.NewGuid().ToString());
        }
    }
}