namespace Polymerium.Trident.Services.Accounts;

public record AccountVault(string? Default, IList<AccountEntry> Entries)
{
}