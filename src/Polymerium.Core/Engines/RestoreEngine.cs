using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using DotNext;
using FluentPipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Core.Components;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Engines.Restoring.Stages;
using Polymerium.Core.Managers;
using Polymerium.Core.StageModels;
using Wupoo;

namespace Polymerium.Core.Engines;

public class RestoreEngine
{
    private readonly AssetManager _assetManager;
    private readonly DownloadEngine _downloader;
    private readonly IFileBaseService _fileBase;
    private readonly ILogger _logger;
    private readonly AssetManager _manager;
    private readonly ResolveEngine _resolver;
    private readonly IServiceScope _scope;
    private readonly SHA1 _sha1 = SHA1.Create();
    private readonly WapooOptions _wapooOptions;

    public RestoreEngine(
        ILogger<RestoreEngine> logger,
        AssetManager manager,
        ResolveEngine resolver,
        DownloadEngine downloader,
        IFileBaseService fileBase,
        IServiceProvider provider,
        AssetManager assetManager
    )
    {
        _logger = logger;
        _manager = manager;
        _resolver = resolver;
        _downloader = downloader;
        _fileBase = fileBase;
        _assetManager = assetManager;
        _scope = provider.CreateScope();
        var settings = new JsonSerializerSettings();
        _wapooOptions = new WapooOptions { JsonSerializerOptions = settings };
    }

    [Obsolete]
    public StageBase ProduceStage(GameInstance instance, IEnumerable<ComponentMeta> metas)
    {
        return new CheckAvailabilityStage(
            instance,
            _sha1,
            metas,
            _downloader,
            _resolver,
            _fileBase,
            _scope.ServiceProvider,
            _assetManager
        );
    }

    public Pipeline<RestoreError, GameInstance> ProducePipeline()
    {
        return PipelineBuilder<RestoreError, GameInstance>
            .Create(BuildPolylockData)
            .Produces(x => (RestoreContext)x)
            .Then<RestoreContext>(CompleteAssets)
            .Produces(x => (RestoreContext)x)
            .Requires<DownloadEngine, RestoreContext>(x => (RestoreContext)x)
            .Then<RestoreContext>(CompleteLibraries)
            .Produces(x => (RestoreContext)x)
            .Requires<DownloadEngine, RestoreContext>(x => (RestoreContext)x)
            .Then<RestoreContext>(CompleteAttachments)
            .Produces(x => (RestoreContext)x)
            .Requires<DownloadEngine, RestoreContext>(x => (RestoreContext)x)
            .Setup().Build();
    }

    private Result<object?, RestoreError> BuildPolylockData(GameInstance instance)
    {
        throw new NotImplementedException();
    }

    private Result<object?, RestoreError> CompleteAssets(GameInstance instance, RestoreContext polylock)
    {
        throw new NotImplementedException();
    }

    private Result<object?, RestoreError> CompleteLibraries(GameInstance instance, RestoreContext polylock)
    {
        throw new NotImplementedException();
    }

    private Result<object?, RestoreError> CompleteAttachments(GameInstance instance, RestoreContext polylock)
    {
        throw new NotImplementedException();
    }
}