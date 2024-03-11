using Trident.Abstractions;

namespace Polymerium.Trident
{
    public class MicrosoftAccount : IAccount
    {
        public ValueTask<bool> RefreshAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> ValidateAsync()
        {
            throw new NotImplementedException();
        }

        public static Task<MicrosoftAccount> LoginAsync()
        {
            throw new NotImplementedException();
        }
    }
}