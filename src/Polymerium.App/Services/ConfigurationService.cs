﻿using System;
using System.IO;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.App.Services;

public sealed class ConfigurationService : IDisposable
{
    private readonly string _filePath = PathDef.Default.FileOfPrivateSettings(Program.Brand);

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

        Value = read ?? new Configuration();
    }

    public Configuration Value { get; }

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(Value, _serializerOptions));
        }
        catch
        {
            // ignored
        }
    }
}