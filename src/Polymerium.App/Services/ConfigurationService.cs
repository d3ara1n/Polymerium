using System;
using System.IO;
using System.Text.Json;

namespace Polymerium.App.Services;

public sealed class ConfigurationService : IDisposable
{
    private readonly Configuration _configuration;
    private readonly string _filePath = Path.Combine(Environment.CurrentDirectory, "polymerium.json");

    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public Configuration Value => _configuration;

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

    public void Dispose()
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_configuration, _serializerOptions));
        }
        catch
        {
            // ignored
        }
    }
}