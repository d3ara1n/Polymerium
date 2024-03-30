using Microsoft.Extensions.Logging;
using Polymerium.Trident.Data;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Profiles;
using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services
{
    public sealed class ProfileManager : IDisposable
    {
        public const string DUMMY_KEY = "_DUMMY";

        public static readonly Profile DUMMY_PROFILE = new(string.Empty, null,
            new Profile.RecordData(Enumerable.Empty<Profile.RecordData.TimelinePoint>().ToList(),
                Enumerable.Empty<Profile.RecordData.Todo>().ToList(),
                string.Empty), new Metadata(string.Empty, Enumerable.Empty<Metadata.Layer>().ToList()),
            new Dictionary<string, object>(), null);

        private readonly TridentContext _context;
        private readonly AccountManager _accountManager;
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _options;

        private readonly IList<ReservedKey> reservedKeys = new List<ReservedKey>();

        private bool disposedValue;

        public ProfileManager(ILogger<ProfileManager> logger, TridentContext context, JsonSerializerOptions options, AccountManager accountManager)
        {
            _logger = logger;
            _context = context;
            _options = options;
            _accountManager = accountManager;

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
            string output = FileNameHelper.Sanitize(key).ToLower();
            if (string.IsNullOrEmpty(output))
            {
                output += "_";
            }

            while (reservedKeys.Any(x => x.Key == output) || Managed.ContainsKey(output))
            {
                output += '_';
            }

            ReservedKey reserved = new(this, output);
            reservedKeys.Add(reserved);
            return reserved;
        }

        internal void ReleaseKey(ReservedKey key)
        {
            if (reservedKeys.Contains(key))
            {
                reservedKeys.Remove(key);
            }
        }

        public Profile Append(ReservedKey key, string name, Attachment? reference, Metadata metadata)
        {
            if (key.Disposed)
            {
                throw new ArgumentException($"Disposed key: {key.Key}");
            }

            DateTimeOffset now = DateTimeOffset.Now;
            Profile profile = new(name, reference, new Profile.RecordData(
                    new List<Profile.RecordData.TimelinePoint>
                    {
                        new(true, reference, Profile.RecordData.TimelinePoint.TimelimeAction.Create,
                            now, now)
                    }, new List<Profile.RecordData.Todo>(), string.Empty), metadata, new Dictionary<string, object>(),
                _accountManager.DefaultUuid);
            Managed.Add(key.Key,
                new Handle<Profile>(profile, Path.Combine(_context.InstanceDir, $"{key.Key}.json"), _options));
            if (!key.Disposed)
            {
                key.Dispose();
            }

            ProfileCollectionChanged?.Invoke(this,
                new ProfileCollectionChangedEventArgs(ProfileCollectionChangedAction.Add, key.Key, profile));
            _logger.LogInformation("Profile appended {name}({key})", name, key.Key);
            return profile;
        }

        public Profile? GetProfile(string key)
        {
            return Managed.TryGetValue(key, out Handle<Profile>? value) ? value.Value : null;
        }

        private void FlushAll()
        {
            int count = 0;
            foreach (Handle<Profile> handle in Managed.Values)
            {
                handle.Flush();
                count++;
            }

            if (Managed.Count != 0)
            {
                _logger.LogInformation("FlushAll triggered, {0} flushed", count);
            }
        }

        public bool Flush(string key)
        {
            if (!Managed.TryGetValue(key, out Handle<Profile>? handle))
            {
                return false;
            }

            handle.Flush();
            _logger.LogInformation("Flush triggered for {0}", key);
            return true;
        }

        private void Scan()
        {
            FlushAll();
            Managed.Clear();
            if (!Directory.Exists(_context.InstanceDir))
            {
                Directory.CreateDirectory(_context.InstanceDir);
            }

            foreach (string file in Directory.GetFiles(_context.InstanceDir, "*.json"))
            {
                try
                {
                    Handle<Profile>? handle = Handle<Profile>.Create(file, _options);
                    if (handle != null)
                    {
                        Managed.Add(Path.GetFileNameWithoutExtension(file), handle);
                        _logger.LogInformation("Appended profile {0}", handle.Value.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "JsonSerializer.Deserialize got null returned while processing {0}, but, why?",
                            Path.GetFileName(file));
                    }
                }
                catch (JsonException e)
                {
                    _logger.LogWarning("Broken profile format detected in {0} for {1}", Path.GetFileName(file),
                        e.Message);
                }
                catch
                {
                    _logger.LogWarning("Bad file operation in {0}", Path.GetFileName(file));
                }
            }
        }

        public void Discard(string key)
        {
            if (Managed.TryGetValue(key, out Handle<Profile>? profile))
            {
                profile.Activated = false;
                Managed.Remove(key);
            }
        }
    }
}