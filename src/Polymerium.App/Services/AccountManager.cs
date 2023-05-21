using System;
using System.Collections.Generic;
using System.Linq;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Data;

namespace Polymerium.App.Services;

public sealed class AccountManager : IDisposable
{
    private readonly DataStorage _dataStorage;
    private readonly MemoryStorage _memoryStorage;

    public AccountManager(MemoryStorage memoryStorage, DataStorage dataStorage)
    {
        _memoryStorage = memoryStorage;
        _dataStorage = dataStorage;
        var accounts = dataStorage.LoadList<AccountModel, IGameAccount>(
            () => Enumerable.Empty<IGameAccount>()
        );
        foreach (var account in accounts)
            _memoryStorage.Accounts.Add(account);
    }

    public void Dispose()
    {
        _dataStorage.SaveList<AccountModel, IGameAccount>(_memoryStorage.Accounts);
    }

    public IEnumerable<IGameAccount> GetView()
    {
        return _memoryStorage.Accounts;
    }

    public bool TryFindById(string id, out IGameAccount? account)
    {
        var found = _memoryStorage.Accounts.FirstOrDefault(x => x.Id == id);
        if (found != null)
        {
            account = found;
            return true;
        }

        account = null;
        return false;
    }

    public void AddAccount(IGameAccount account)
    {
        _memoryStorage.Accounts.Add(account);
    }

    public void RemoveAccount(IGameAccount account)
    {
        _memoryStorage.Accounts.Remove(account);
    }
}