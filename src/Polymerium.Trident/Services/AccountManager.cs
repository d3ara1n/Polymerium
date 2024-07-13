using Microsoft.Extensions.Logging;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Services.Accounts;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.Trident.Services;

public sealed class AccountManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _options;
    private readonly TridentContext _trident;

    private bool disposedValue;

    public AccountManager(TridentContext trident, ILogger<AccountManager> logger, JsonSerializerOptions options)
    {
        _trident = trident;
        _logger = logger;
        _options = options;

        Scan();
    }

    public string? DefaultUuid { get; set; }

    public IList<IAccount> Managed { get; } = new List<IAccount>();

    public void Dispose()
    {
        if (!disposedValue)
        {
            Flush();
            disposedValue = true;
        }
    }

    public event AccountCollectionChangedHandler? AccountCollectionChanged;

    private void Scan()
    {
        Managed.Clear();
        var path = _trident.AccountVaultFile;
        if (File.Exists(path))
        {
            try
            {
                var content = File.ReadAllText(path);
                var vault = JsonSerializer.Deserialize<AccountVault>(content, _options);
                DefaultUuid = vault?.Default;
                foreach (var entry in vault?.Entries ?? Enumerable.Empty<AccountEntry>())
                {
                    var unmasked = Encoding.UTF8.GetString(entry.Opaque);
                    var account = JsonSerializer.Deserialize(unmasked, entry.Type switch
                    {
                        nameof(MicrosoftAccount) => typeof(MicrosoftAccount),
                        nameof(AuthlibAccount) => typeof(AuthlibAccount),
                        nameof(FamilyAccount) => typeof(FamilyAccount),
                        _ => throw new NotSupportedException($"The type of account is not supported: {entry.Type}")
                    }, _options) as IAccount;
                    ArgumentNullException.ThrowIfNull(account);
                    Managed.Add(account);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred while loading account vault");
            }
        }
    }

    private void Flush()
    {
        var path = _trident.AccountVaultFile;
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        List<AccountEntry> list = new();
        try
        {
            foreach (var account in Managed)
            {
                var unmasked = JsonSerializer.Serialize(account, account.GetType(), _options);
                var masked = Encoding.UTF8.GetBytes(unmasked);
                list.Add(new AccountEntry(account.GetType().Name, masked));
            }

            var content = JsonSerializer.Serialize(new AccountVault(DefaultUuid, list), _options);
            File.WriteAllText(path, content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while saving account vault");
        }
    }

    public void Append(IAccount account)
    {
        Managed.Add(account);
        if (string.IsNullOrEmpty(DefaultUuid))
        {
            DefaultUuid = account.Uuid;
        }

        AccountCollectionChanged?.Invoke(this,
            new AccountCollectionChangedEventArgs(AccountCollectionChangedAction.Add, account));
    }

    public void Remove(string uuid)
    {
        var found = Managed.FirstOrDefault(x => x.Uuid == uuid);
        if (found != null)
        {
            Managed.Remove(found);
            AccountCollectionChanged?.Invoke(this,
                new AccountCollectionChangedEventArgs(AccountCollectionChangedAction.Remove, found));
        }
    }

    public bool TryGetByUuid(string uuid, [MaybeNullWhen(false)] out IAccount result)
    {
        var found = Managed.FirstOrDefault(x => x.Uuid == uuid);
        if (found != null)
        {
            result = found;
            return true;
        }

        result = null;
        return false;
    }
}