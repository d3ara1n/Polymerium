using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
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
        private readonly DataStorageService _storageService;

        private readonly List<GameInstance> _instances;
        public InstanceManager(DataStorageService storageService)
        {
            _storageService = storageService;
            var instances = storageService.LoadList<InstanceModel, GameInstance>(() => new List<GameInstance>());
            _instances = instances.ToList();
        }

        public void Dispose()
        {
            _storageService.SaveList<InstanceModel, GameInstance>(_instances);
        }

        public IEnumerable<GameInstance> GetView()
        {
            return _instances;
        }

        public Result<InstanceManagerError> AddInstance(GameInstance instance)
        {
            if (_instances.Any(x => x.Id == instance.Id))
            {
                return Result<InstanceManagerError>.Err(InstanceManagerError.DuplicateId);
            }
            else
            {
                _instances.Add(instance);
                StrongReferenceMessenger.Default.Send(new GameInstanceAddedMessage(instance));
                return Result<InstanceManagerError>.Ok();
            }
        }
    }
}
