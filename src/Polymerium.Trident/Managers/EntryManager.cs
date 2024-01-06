using Microsoft.Extensions.Logging;
using Polymerium.Trident.Data;
using System.Text.Json;
using Trident.Abstractions;

namespace Polymerium.Trident.Managers;

public class EntryManager
{
    private readonly ILogger _logger;
    private readonly PolymeriumContext _context;
    private readonly JsonSerializerOptions _options;

    public EntryManager(ILogger<EntryManager> logger, PolymeriumContext context, JsonSerializerOptions options)
    {
        _logger = logger;
        _context = context;
        _options = options;
    }

    public Handle<Profile>? GetProfile(string key)
        => Handle<Profile>.Create(Path.Combine(_context.InstanceDir, $"{key}.json"), _options);

    public IEnumerable<Entry> Scan()
    {
        var entries = new List<Entry>();
        foreach (var file in Directory.GetFiles(_context.InstanceDir, "*.json"))
        {
            try
            {
                var content = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<Profile>(content);
                if (profile != null)
                {
                    entries.Add(new Entry(Path.GetFileNameWithoutExtension(file), profile));
                    _logger.LogInformation("Appended profile {0}", profile.Name);
                }
                else
                {
                    _logger.LogWarning("JsonSerializer.Deserialze got null returned while processing {0}, but, why?", Path.GetFileName(file));
                }
            }
            catch (JsonException e)
            {
                _logger.LogWarning("Broken profile format detected in {0} for {1}", Path.GetFileName(file), e.Message);
            }
            catch
            {
                _logger.LogWarning("Bad file operation in {0}", Path.GetFileName(file));
            }
        }
        return entries;
    }
}
