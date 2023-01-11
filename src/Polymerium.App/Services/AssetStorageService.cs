using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Polymerium.App.Services;

public class AssetStorageService
{
    private string storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private readonly ILogger _logger;

    public AssetStorageService(ILogger<AssetStorageService> logger)
    {
        _logger = logger;
        logger.LogInformation("{}", storageDirectory);
    }
}
