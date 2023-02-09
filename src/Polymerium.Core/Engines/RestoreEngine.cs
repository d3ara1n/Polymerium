using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Core.Components;
using Polymerium.Core.Engines.Restoring.Stages;
using Polymerium.Core.StageModels;
using Wupoo;

namespace Polymerium.Core.Engines;

public class RestoreEngine
{
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly ILogger _logger;
    private readonly GameManager _manager;
    private readonly ResolveEngine _resolver;
    private readonly IServiceScope _scope;
    private readonly SHA1 _sha1 = SHA1.Create();
    private readonly WapooOptions _wapooOptions;

    public RestoreEngine(ILogger<RestoreEngine> logger, GameManager manager, ResolveEngine resolver,
        DownloadEngine downloader, IFileBaseService fileBase, IServiceProvider provider)
    {
        _logger = logger;
        _manager = manager;
        _resolver = resolver;
        _downloader = downloader;
        _fileBase = fileBase;
        _scope = provider.CreateScope();
        var settings = new JsonSerializerSettings();
        _wapooOptions = new WapooOptions
        {
            JsonSerializerOptions = settings
        };
    }

    public StageBase ProduceStage(GameInstance instance, IEnumerable<ComponentMeta> metas)
    {
        return new CheckAvailabilityStage(instance, _sha1, metas, _downloader, _resolver, _fileBase,
            _scope.ServiceProvider);
    }
}