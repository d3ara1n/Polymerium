using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services
{
    public class ProfileManager : IDisposable
    {
        #region Injected

        private readonly ILogger<ProfileManager> _logger;

        #endregion

        private readonly List<ProfileHandle> _profiles = [];
        private readonly JsonSerializerOptions _serializerOptions;
        internal readonly IList<ReservedKey> ReservedKeys = new List<ReservedKey>();


        public ProfileManager(ILogger<ProfileManager> logger)
        {
            _logger = logger;
            _serializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
            _serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            _serializerOptions.Converters.Add(new SystemObjectNewtonsoftCompatibleConverter());

            var dir = new DirectoryInfo(PathDef.Default.InstanceDirectory);
            if (!dir.Exists)
            {
                return;
            }

            foreach (var ins in dir.GetDirectories())
            {
                var path = PathDef.Default.FileOfProfile(ins.Name);
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var bomb = PathDef.Default.FileOfBomb(ins.Name);
                    if (File.Exists(bomb))
                    {
                        ins.Delete(true);
                        continue;
                    }

                    var handle = ProfileHandle.Create(ins.Name, path, _serializerOptions);
                    _profiles.Add(handle);
                    logger.LogInformation("{} scanned", handle.Key);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to add profile {}", ins.Name);
                }
            }
        }

        public IEnumerable<(string, Profile)> Profiles => _profiles.Select(x => (x.Key, x.Value));

        public bool TryGetMutable(string key, [MaybeNullWhen(false)] out ProfileGuard profile)
        {
            var handle = _profiles.FirstOrDefault(x => x.Key == key);
            if (handle is not null)
            {
                profile = new(this, handle);
                return true;
            }

            profile = null;
            return false;
        }

        public bool TryGetImmutable(string key, [MaybeNullWhen(false)] out Profile profile)
        {
            var handle = _profiles.FirstOrDefault(x => x.Key == key);
            if (handle is not null)
            {
                profile = handle.Value;
                return true;
            }

            profile = null;
            return false;
        }

        public Profile GetImmutable(string key) =>
            TryGetImmutable(key, out var profile)
                ? profile
                : throw new KeyNotFoundException($"{key} is not a key to the managed profile");

        public ProfileGuard GetMutable(string key) =>
            TryGetMutable(key, out var profile)
                ? profile
                : throw new KeyNotFoundException($"{key} is not a key to the managed profile");

        public ReservedKey RequestKey(string key)
        {
            var sanitized = !string.IsNullOrEmpty(key)
                                ? string.Join(string.Empty,
                                              key
                                                 .Trim()
                                                 .ToLower()
                                                 .Where(x => !Path.GetInvalidFileNameChars().Contains(x))
                                                 .Select(x => x is ' ' or '-' ? '_' : x))
                                : "_";
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            while (_profiles.Any(x => x.Key == sanitized) || ReservedKeys.Any(x => x.Key == sanitized))
            {
                sanitized += '_';
            }

            var rv = new ReservedKey(sanitized, this);
            ReservedKeys.Add(rv);
            return rv;
        }

        public void Add(ReservedKey key, Profile profile)
        {
            var handle = new ProfileHandle(key.Key,
                                           profile,
                                           PathDef.Default.FileOfProfile(key.Key),
                                           _serializerOptions);
            handle.SaveAsync().Wait();
            _profiles.Add(handle);
            key.Dispose();
            handle.SaveAsync().Wait();

            _logger.LogInformation("{} added", handle.Key);
            OnProfileAdded(key.Key, profile);
        }

        public void Remove(string key)
        {
            var handle = _profiles.FirstOrDefault(x => x.Key == key);
            if (handle is null)
            {
                throw new InvalidOperationException($"{key} is not in profiles");
            }

            _profiles.Remove(handle);

            _logger.LogInformation("{} removed", key);
            OnProfileRemoved(key, handle.Value);
        }

        public void Update(
            string key,
            string? source,
            string name,
            string version,
            string? loader,
            IReadOnlyList<string> packages,
            IDictionary<string, object> overrides)
        {
            var handle = _profiles.FirstOrDefault(x => x.Key == key);
            if (handle is null)
            {
                throw new InvalidOperationException($"{key} is not in profiles");
            }

            var changeSet = packages.ToDictionary(PackageHelper.ExtractProjectIdentityIfValid);
            var removeSet = new List<Profile.Rice.Entry>();
            foreach (var entry in handle.Value.Setup.Packages.Where(x => x.Source == handle.Value.Setup.Source))
            {
                var extracted = PackageHelper.ExtractProjectIdentityIfValid(entry.Purl);
                if (changeSet.TryGetValue(extracted, out var change))
                {
                    entry.Purl = change;
                    entry.Source = source;
                    changeSet.Remove(extracted);
                }
                else
                {
                    removeSet.Add(entry);
                }
            }

            foreach (var remove in removeSet)
            {
                handle.Value.Setup.Packages.Remove(remove);
            }

            foreach (var add in changeSet.Values)
            {
                handle.Value.Setup.Packages.Add(new(add, true, source, null));
            }

            foreach (var (k, v) in overrides)
            {
                handle.Value.Overrides[k] = v;
            }

            handle.Value.Name = name;
            handle.Value.Setup.Source = source;
            handle.Value.Setup.Version = version;
            handle.Value.Setup.Loader = loader;

            handle.SaveAsync().Wait();
            _logger.LogInformation("{} updated", key);
            OnProfileUpdated(key, handle.Value);
        }

        #region Nested type: SystemObjectNewtonsoftCompatibleConverter

        private class SystemObjectNewtonsoftCompatibleConverter : JsonConverter<object>
        {
            public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.True:
                        return true;
                    case JsonTokenType.False:
                        return false;
                    case JsonTokenType.Number when reader.TryGetInt64(out var l):
                        return l;
                    case JsonTokenType.Number:
                        return reader.GetDouble();
                    case JsonTokenType.String when reader.TryGetDateTime(out var datetime):
                        return datetime;
                    case JsonTokenType.String:
                        return reader.GetString();
                    default:
                    {
                        // Use JsonElement as fallback.
                        // Newtonsoft uses JArray or JObject.
                        using var document = JsonDocument.ParseValue(ref reader);
                        return document.RootElement.Clone();
                    }
                }
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) =>
                writer.WriteRawValue(JsonSerializer.Serialize(value));
        }

        #endregion


        #region Profile Changed Event

        public class ProfileChangedEventArgs(string key, Profile profile) : EventArgs
        {
            public string Key => key;
            public Profile Value => profile;
        }

        public event EventHandler<ProfileChangedEventArgs>? ProfileUpdated;

        public event EventHandler<ProfileChangedEventArgs>? ProfileRemoved;

        public event EventHandler<ProfileChangedEventArgs>? ProfileAdded;

        internal void OnProfileUpdated(string key, Profile profile) => ProfileUpdated?.Invoke(this, new(key, profile));

        internal void OnProfileRemoved(string key, Profile profile) => ProfileRemoved?.Invoke(this, new(key, profile));

        internal void OnProfileAdded(string key, Profile profile) => ProfileAdded?.Invoke(this, new(key, profile));

        #endregion

        #region Dispose

        private bool _isDisposing;

        public void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }

            _isDisposing = true;

            var tasks = _profiles.Select(x => x.DisposeAsync().AsTask()).ToArray();
            Task.WaitAll(tasks);

            _profiles.Clear();

            _isDisposing = false;
        }

        #endregion
    }
}
