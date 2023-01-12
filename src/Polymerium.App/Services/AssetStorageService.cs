using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.App.Messages;

namespace Polymerium.App.Services;

public class AssetStorageService: IRecipient<ApplicationAliveChangedMessage>
{
    private readonly ILogger _logger;
    private string storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".polymerium");
    private readonly string instanceManifestPath;
    private readonly List<GameInstance> instances;
    public AssetStorageService(ILogger<AssetStorageService> logger)
    {
        _logger = logger;
        logger.LogInformation("Storage in: {}", storageDirectory);
        StrongReferenceMessenger.Default.RegisterAll(this);
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        instanceManifestPath = Path.Combine(storageDirectory, "instances.json");
        // read from instances.json
        if (File.Exists(instanceManifestPath))
        {
            instances = JsonConvert.DeserializeObject<IEnumerable<GameInstance>>(File.ReadAllText(instanceManifestPath)).ToList();
        }
        else
        {
            instances = new();
        }
    }

    public IEnumerable<GameInstance> GetViewOfInstances()
    {
        return instances;
    }

    public Option<GameInstance> FindById(string id)
    {
        var found = instances.FirstOrDefault(it => it.Id == id);
        return Option<GameInstance>.Wrap(found);
    }

    public void AddInstance(GameInstance instance)
    {
        if (instances.Any(x => x.Id == instance.Id))
        {
            // 重复了
        }
        else
        {
            instances.Add(instance);
            StrongReferenceMessenger.Default.Send(new GameInstanceAddedMessage(instance));
        }
    }

    public void Receive(ApplicationAliveChangedMessage message)
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        File.WriteAllText(instanceManifestPath, JsonConvert.SerializeObject(instances, Formatting.Indented));
    }
}
