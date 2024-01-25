using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Data;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions;
using static Trident.Abstractions.Profile.RecordData;

namespace Polymerium.Trident.Services;

public sealed class ProfileManager : IDisposable
{
    public const string DUMMY_KEY = "_DUMMY";

    public static readonly Profile DUMMY_PROFILE = new(string.Empty, null, null,
        new Profile.RecordData(Enumerable.Empty<TimelinePoint>().ToList(), Enumerable.Empty<Todo>().ToList(),
            string.Empty), new Metadata(string.Empty, Enumerable.Empty<Metadata.Layer>().ToList()),
        new Dictionary<string, object>(), null);

    private readonly PolymeriumContext _context;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _options;

    private bool disposedValue;

    public event ProfileCollectionChangedDelegate? ProfileChanged;

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
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public string RequestKey(string key)
    {
        var output = FileNameHelper.Sanitize(key);
        if (string.IsNullOrEmpty(output)) output += "_";
        while (Managed.ContainsKey(output)) output += '_';
        return output;
    }

    public Profile Create(string key, string name, Uri? thumbnail, string? reference, Metadata metadata)
    {
        var now = DateTimeOffset.Now;
        var profile = new Profile(name, thumbnail, reference, new Profile.RecordData(
            new List<TimelinePoint>
            {
                new(true, null, TimelinePoint.TimelimeAction.Create,
                    now, now)
            }, new List<Todo>(), string.Empty), metadata, new Dictionary<string, object>(), null);
        Managed.Add(key, new Handle<Profile>(profile, Path.Combine(_context.InstanceDir, $"{key}.json"), _options));
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

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                FlushAll();
                Managed.Clear();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }
}