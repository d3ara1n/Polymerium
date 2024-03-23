﻿using Microsoft.Extensions.Logging;
using Polymerium.Trident.Accounts;
using Polymerium.Trident.Services.Accounts;
using System.Text;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.Trident.Services
{
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
            string path = _trident.AccountVaultFile;
            if (File.Exists(path))
            {
                try
                {
                    string content = File.ReadAllText(path);
                    List<AccountEntry>? list = JsonSerializer.Deserialize<List<AccountEntry>>(content, _options);
                    foreach (AccountEntry entry in list ?? Enumerable.Empty<AccountEntry>())
                    {
                        string unmasked = Encoding.UTF8.GetString(entry.Opaque);
                        IAccount? account = JsonSerializer.Deserialize(unmasked, entry.Type switch
                        {
                            nameof(MicrosoftAccount) => typeof(MicrosoftAccount),
                            nameof(AuthlibAccount) => typeof(AuthlibAccount),
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
            string path = _trident.AccountVaultFile;
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            List<AccountEntry> list = new();
            try
            {
                foreach (IAccount account in Managed)
                {
                    string unmasked = JsonSerializer.Serialize(account, _options);
                    byte[] masked = Encoding.UTF8.GetBytes(unmasked);
                    list.Add(new AccountEntry(account.GetType().Name, masked));
                }

                string content = JsonSerializer.Serialize(list, _options);
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
            AccountCollectionChanged?.Invoke(this,
                new AccountCollectionChangedEventArgs(AccountCollectionChangedAction.Add, account));
        }

        public void Remove(string uuid)
        {
            IAccount? found = Managed.FirstOrDefault(x => x.Uuid == uuid);
            if (found != null)
            {
                Managed.Remove(found);
                AccountCollectionChanged?.Invoke(this,
                    new AccountCollectionChangedEventArgs(AccountCollectionChangedAction.Remove, found));
            }
        }
    }
}