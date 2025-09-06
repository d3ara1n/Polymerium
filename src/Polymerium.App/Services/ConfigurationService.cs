using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trident.Abstractions;

namespace Polymerium.App.Services;

public sealed class ConfigurationService : IDisposable
{
    private readonly string _filePath = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "settings.json");

    private readonly JsonSerializerOptions _serializerOptions =
        new(JsonSerializerDefaults.General) { WriteIndented = true };

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;

        Configuration? read = null;
        if (File.Exists(_filePath))
        {
            try
            {
                read = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(_filePath), _serializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read configuration from disk, using default configuration");
            }
        }

        Value = read ?? new Configuration();
    }

    public Configuration Value { get; }

    #region Injected

    private readonly ILogger _logger;

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(Value, _serializerOptions));
        }
        catch
        {
            // ignored
        }
    }

    #endregion
}
