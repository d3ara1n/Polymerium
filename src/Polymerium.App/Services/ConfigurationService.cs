using System;
using System.IO;
using System.Text.Json;
using Polymerium.Trident;

namespace Polymerium.App.Services;

public sealed class ConfigurationService : IDisposable
{
    private readonly Configuration _configuration;
    private readonly string _filePath = PathDef.Default.FileOfPrivateSettings;

    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public ConfigurationService()
    {
        Configuration? read = null;
        if (File.Exists(_filePath))
        {
            try
            {
                read = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(_filePath), _serializerOptions);
            }
            catch
            {
                // ignored
            }
        }

        _configuration = read ?? new Configuration();
    }

    public Configuration Value => _configuration;

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_configuration, _serializerOptions));
        }
        catch
        {
            // ignored
        }
    }
}