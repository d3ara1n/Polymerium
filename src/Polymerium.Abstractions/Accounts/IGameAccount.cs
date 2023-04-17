using System.Threading.Tasks;

namespace Polymerium.Abstractions.Accounts;

public interface IGameAccount
{
    string LoginType { get; }
    string FriendlyType { get; }

    string Id { get; set; }
    string UUID { get; set; }
    string Nickname { get; set; }
    string AccessToken { get; set; }
    Task<bool> ValidateAsync();
    Task<bool> RefreshAsync();
}
