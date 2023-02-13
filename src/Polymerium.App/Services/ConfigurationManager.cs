using System;
using Polymerium.App.Configurations;
using Polymerium.App.Data;

namespace Polymerium.App.Services;

public sealed class ConfigurationManager : IDisposable
{
    private readonly DataStorage _dataStorage;

    public ConfigurationManager(DataStorage dataStorage)
    {
        _dataStorage = dataStorage;
        Current = dataStorage.Load<ConfigurationModel, Configuration>(() => new Configuration());
    }

    public Configuration Current { get; set; }

    public void Dispose()
    {
        _dataStorage.Save<ConfigurationModel, Configuration>(Current);
    }
}
