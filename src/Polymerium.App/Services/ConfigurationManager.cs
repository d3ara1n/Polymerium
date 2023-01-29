using Polymerium.App.Configurations;
using Polymerium.App.Data;
using System;

namespace Polymerium.App.Services
{
    public sealed class ConfigurationManager : IDisposable
    {
        private readonly DataStorage _dataStorage;
        public Configuration Current { get; set; }

        public ConfigurationManager(DataStorage dataStorage)
        {
            _dataStorage = dataStorage;
            Current = dataStorage.Load<ConfigurationModel, Configuration>(() => new Configuration());
        }

        public void Dispose()
        {
            _dataStorage.Save<ConfigurationModel, Configuration>(Current);
        }
    }
}