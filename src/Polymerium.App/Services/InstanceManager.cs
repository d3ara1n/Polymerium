using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;
using Polymerium.Abstractions;
using Polymerium.App.Data;
using Polymerium.App.Messages;

namespace Polymerium.App.Services
{
    public enum InstanceManagerError
    {
        DuplicateId
    }
    public sealed class InstanceManager : IDisposable
    {
        private readonly DataStorage _dataStorage;
        private readonly MemoryStorage _memoryStorage;
        public InstanceManager(DataStorage dataStorage, MemoryStorage memoryStorage)
        {
            _dataStorage = dataStorage;
            _memoryStorage = memoryStorage;
            var instances = dataStorage.LoadList<InstanceModel, GameInstance>(() => new List<GameInstance>());
            foreach (var instance in instances)
            {
                _memoryStorage.Instances.Add(instance);
            }
        }

        public void Dispose()
        {
            _dataStorage.SaveList<InstanceModel, GameInstance>(_memoryStorage.Instances);
        }

        public IEnumerable<GameInstance> GetView()
        {
            return _memoryStorage.Instances;
        }

        public Result<InstanceManagerError> AddInstance(GameInstance instance)
        {
            if (_memoryStorage.Instances.Any(x => x.Id == instance.Id))
            {
                return Result<InstanceManagerError>.Err(InstanceManagerError.DuplicateId);
            }
            else
            {
                while (_memoryStorage.Instances.Any(x => x.FolderName == instance.FolderName)) instance.FolderName += '_';
                return Result<InstanceManagerError>.Ok();
            }
        }

        public Option<GameInstance> FindById(string id)
        {
            if (TryFindById(id, out var instance))
            {
                return Option<GameInstance>.Some(instance);
            }
            else
            {
                return Option<GameInstance>.None();
            }
        }

        public bool TryFindById(string id, out GameInstance instance)
        {
            instance = _memoryStorage.Instances.FirstOrDefault(x => x.Id == id);
            return instance != null;
        }
    }
}
