namespace Trident.Abstractions.Accounts;

public interface IAccount
{
    string Username { get; }
    string Uuid { get; }
    string AccessToken { get; }
    string UserType { get; }
}