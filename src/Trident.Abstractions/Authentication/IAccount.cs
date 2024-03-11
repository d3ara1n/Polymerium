namespace Trident.Abstractions
{
    public interface IAccount
    {
        public ValueTask<bool> ValidateAsync();
        public ValueTask<bool> RefreshAsync();
    }
}