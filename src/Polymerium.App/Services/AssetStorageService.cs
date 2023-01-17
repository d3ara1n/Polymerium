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

// TODO: 未来要被 DataCenter 和各种 asset manager 替代。
// GameInstance 本身就是用于磁盘存储的形式。Polymerium 维护的是 DataCenter 库中虚拟资产，
// 对游戏本体的操作要通过启动游戏前的 restore(将游戏本体配置到 GameMetadata 中描述的状态) 进行。
// GameInstance 是 GameMetadata 与实际文件的连结，表示游戏本体文件的一般状态（所在位置）和一些附加信息（实例名，上次游玩时间）。
// 通过 GameInstance，Polymerium.Core 的 restore 设施可以在指定位置创造一个游戏版本并配置到目标状态，至于维护游戏文件，那是玩家的事。
// InstanceView 的修改页面可以改变 GameInstance 状态，本体页面则能对游戏文件做出修改
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
