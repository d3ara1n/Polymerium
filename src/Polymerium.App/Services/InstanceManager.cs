using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Polymerium.Abstractions;
using Polymerium.App.Data;
using Polymerium.App.Messages;
using Polymerium.Core;

namespace Polymerium.App.Services;

public enum InstanceManagerError
{
    DuplicateId,
    FileSystemOperationFailed
}

public sealed class InstanceManager : IDisposable
{
    private readonly DataStorage _dataStorage;
    private readonly IFileBaseService _fileBase;
    private readonly MemoryStorage _memoryStorage;

    public InstanceManager(
        DataStorage dataStorage,
        MemoryStorage memoryStorage,
        IFileBaseService fileBase
    )
    {
        _dataStorage = dataStorage;
        _memoryStorage = memoryStorage;
        _fileBase = fileBase;
        var instances = dataStorage.LoadList<InstanceModel, GameInstance>(
            () => Enumerable.Empty<GameInstance>()
        );
        foreach (var instance in instances)
            _memoryStorage.Instances.Add(instance);
    }

    public void Dispose()
    {
        _dataStorage.SaveList<InstanceModel, GameInstance>(_memoryStorage.Instances);
    }

    public IEnumerable<GameInstance> GetView()
    {
        return _memoryStorage.Instances;
    }

    public InstanceManagerError? AddInstance(GameInstance instance)
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        if (string.IsNullOrWhiteSpace(instance.Name))
            instance.Name = "_";
        if (_memoryStorage.Instances.Any(x => x.Id == instance.Id))
            return InstanceManagerError.DuplicateId;
        instance.FolderName = string.Join(
            "",
            instance.FolderName.Select(x => invalidFileNameChars.Contains(x) ? '_' : x)
        );
        while (_memoryStorage.Instances.Any(x => x.FolderName == instance.FolderName))
            instance.FolderName += '_';
        _memoryStorage.Instances.Add(instance);
        WeakReferenceMessenger.Default.Send(new InstanceAddedMessage(instance));
        return null;
    }

    public InstanceManagerError? RenameInstanceSafe(GameInstance instance, string name)
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        if (string.IsNullOrWhiteSpace(name))
            name = "_";
        var folderName = string.Join(
            "",
            name.Select(x => invalidFileNameChars.Contains(x) ? '_' : x)
        );
        while (_memoryStorage.Instances.Any(x => x.Id != instance.Id && x.FolderName == folderName))
            folderName += '_';
        var instanceDir = _fileBase.Locate(new Uri($"poly-file://{instance.Id}"));
        if (Directory.Exists(instanceDir))
        {
            var newDir =
                Path.Combine(Path.GetDirectoryName(instanceDir.EndsWith('\\') ? instanceDir[..^1] : instanceDir)!,
                    folderName);
            try
            {
                Directory.Move(instanceDir, newDir);
            }
            catch
            {
                return InstanceManagerError.FileSystemOperationFailed;
            }
        }

        instance.Name = name;
        instance.FolderName = folderName;
        return null;
    }

    public Option<GameInstance> FindById(string id)
    {
        if (TryFindById(id, out var instance))
            return Option<GameInstance>.Some(instance!);
        return Option<GameInstance>.None();
    }

    public bool TryFindById(string id, out GameInstance? instance)
    {
        instance = _memoryStorage.Instances.FirstOrDefault(x => x.Id == id);
        return instance != null;
    }

    public void RemoveInstance(GameInstance instance)
    {
        if (TryFindById(instance.Id, out var found))
        {
            _memoryStorage.Instances.Remove(found!);
            WeakReferenceMessenger.Default.Send(new InstanceRemovedMessage(found!));
        }
    }
}