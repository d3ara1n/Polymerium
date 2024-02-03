using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Data;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Profiles;

namespace Polymerium.Trident.Services;

public sealed class ProfileManager : IDisposable
{
    public const string DUMMY_KEY = "_DUMMY";

    public static readonly Profile DUMMY_PROFILE = new(string.Empty, null, null,
        new RecordData(Enumerable.Empty<TimelinePoint>().ToList(), Enumerable.Empty<Todo>().ToList(),
            string.Empty), new Metadata(string.Empty, Enumerable.Empty<Metadata.Layer>().ToList()),
        new Dictionary<string, object>(), null);

    private readonly PolymeriumContext _context;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _options;

    private readonly IList<ReservedKey> reservedKeys = new List<ReservedKey>();

    private bool disposedValue;

    public ProfileManager(ILogger<ProfileManager> logger, PolymeriumContext context, JsonSerializerOptions options)
    {
        _logger = logger;
        _context = context;
        _options = options;

        Scan();
    }

    public IDictionary<string, Handle<Profile>> Managed { get; } = new Dictionary<string, Handle<Profile>>();
    public IEnumerable<Profile> Profiles => Managed.Values.Select(x => x.Value);

    public void Dispose()
    {
        if (!disposedValue)
        {
            FlushAll();
            Managed.Clear();
            disposedValue = true;
        }
    }

    public event ProfileCollectionChangedHandler? ProfileCollectionChanged;

    public ReservedKey RequestKey(string key)
    {
        var output = FileNameHelper.Sanitize(key).ToLower();
        if (string.IsNullOrEmpty(output)) output += "_";
        while (reservedKeys.Any(x => x.Key == output) || Managed.ContainsKey(output)) output += '_';
        var reserved = new ReservedKey(this, output);
        reservedKeys.Add(reserved);
        return reserved;
    }

    internal void ReleaseKey(ReservedKey key)
    {
        if (reservedKeys.Contains(key)) reservedKeys.Remove(key);
    }

    public Profile Append(ReservedKey key, string name, Uri? thumbnail, string? reference, Metadata metadata)
    {
        if (key.Disposed) throw new ArgumentException($"Disposed key: {key.Key}");
        var now = DateTimeOffset.Now;
        var profile = new Profile(name, thumbnail, reference, new RecordData(
            new List<TimelinePoint>
            {
                new(true, null, TimelimeAction.Create,
                    now, now)
            }, new List<Todo>(), string.Empty), metadata, new Dictionary<string, object>(), null);
        Managed.Add(key.Key,
            new Handle<Profile>(profile, Path.Combine(_context.InstanceDir, $"{key.Key}.json"), _options));
        if (!key.Disposed) key.Dispose();
        ProfileCollectionChanged?.Invoke(this,
            new ProfileCollectionChangedEventArgs(ProfileCollectionChangedAction.Add, key.Key, profile));
        _logger.LogInformation("Profile appended {name}({key})", name, key.Key);
        return profile;
    }

    public Profile? GetProfile(string key)
    {
        return Managed.TryGetValue(key, out var value) ? value.Value : null;
    }

    private void FlushAll()
    {
        var count = 0;
        foreach (var handle in Managed.Values)
        {
            handle.Flush();
            count++;
        }

        if (Managed.Count != 0)
            _logger.LogInformation("FlushAll triggered, {0} flushed", count);
    }

    public bool Flush(string key)
    {
        if (!Managed.TryGetValue(key, out var handle)) return false;
        handle.Flush();
        _logger.LogInformation("Flush triggered for {0}", key);
        return true;
    }

    private void Scan()
    {
        FlushAll();
        Managed.Clear();
        if (!Directory.Exists(_context.InstanceDir)) Directory.CreateDirectory(_context.InstanceDir);
        foreach (var file in Directory.GetFiles(_context.InstanceDir, "*.json"))
            try
            {
                var handle = Handle<Profile>.Create(file, _options);
                if (handle != null)
                {
                    Managed.Add(Path.GetFileNameWithoutExtension(file), handle);
                    _logger.LogInformation("Appended profile {0}", handle.Value.Name);
                }
                else
                {
                    _logger.LogWarning("JsonSerializer.Deserialize got null returned while processing {0}, but, why?",
                        Path.GetFileName(file));
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
}