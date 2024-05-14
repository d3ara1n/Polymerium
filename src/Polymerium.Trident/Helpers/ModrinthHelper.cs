using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.Labrinth;
using Polymerium.Trident.Repositories;
using System.Net.Http.Json;
using System.Text.Json;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers
{
    public static class ModrinthHelper
    {
        private const string ENDPOINT = "https://api.modrinth.com/v2";
        private const string PROJECT_URL = "https://modrinth.com/{0}/{1}";

        public static readonly IReadOnlyDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
        {
            { "forge", Loader.COMPONENT_FORGE },
            { "neoforge", Loader.COMPONENT_NEOFORGE },
            { "fabric", Loader.COMPONENT_FABRIC },
            { "quilt", Loader.COMPONENT_QUILT }
        }.AsReadOnly();

        private static readonly JsonSerializerOptions OPTIONS = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public static string GetUrlTypeStringFromKind(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Modpack => "modpack",
                ResourceKind.Mod => "mod",
                ResourceKind.ResourcePack => "resourcepack",
                ResourceKind.ShaderPack => "shader",
                ResourceKind.DataPack => "datapack",
                _ => throw new NotSupportedException()
            };
        }


        public static ResourceKind GetKindFromTypeString(string type)
        {
            return type switch
            {
                "modpack" => ResourceKind.Modpack,
                "mod" => ResourceKind.Mod,
                "resourcepack" => ResourceKind.ResourcePack,
                "shader" => ResourceKind.ShaderPack,
                "datapack" => ResourceKind.DataPack,
                _ => throw new NotSupportedException()
            };
        }

        public static async Task<T> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory, string service,
            CancellationToken token = default)
            where T : struct
        {
            token.ThrowIfCancellationRequested();
            using var client = factory.CreateClient();
            try
            {
                return await client.GetFromJsonAsync<T>(ENDPOINT + service, OPTIONS, token);
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to get {} from Modrinth for {}", service, e.Message);
                throw;
            }
        }

        public static async Task<IEnumerable<T>> GetResourcesAsync<T>(ILogger logger, IHttpClientFactory factory,
            string service,
            CancellationToken token = default)
            where T : struct
        {
            token.ThrowIfCancellationRequested();
            using var client = factory.CreateClient();
            try
            {
                return await client.GetFromJsonAsync<IEnumerable<T>>(ENDPOINT + service, OPTIONS, token) ??
                       Array.Empty<T>();
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to get {} from Modrinth for {}", service, e.Message);
                throw;
            }
        }

        public static async Task<IEnumerable<LabrinthHit>> SearchProjectsAsync(ILogger logger,
            IHttpClientFactory factory, string query, ResourceKind kind, string? gameVersion = null,
            string? modLoaderId = null, uint offset = 0, uint limit = 10, CancellationToken token = default)
        {
            var facets = new List<KeyValuePair<string, string>>();
            if (gameVersion != null)
                facets.Add(new KeyValuePair<string, string>("versions", gameVersion));

            if (kind == ResourceKind.Mod && modLoaderId != null)
                facets.Add(new KeyValuePair<string, string>("categories", modLoaderId switch
                {
                    Loader.COMPONENT_FORGE => "forge",
                    Loader.COMPONENT_NEOFORGE => "neoforge",
                    Loader.COMPONENT_FABRIC => "fabric",
                    Loader.COMPONENT_QUILT => "quilt",
                    _ => ""
                }));

            facets.Add(new KeyValuePair<string, string>("project_type", GetUrlTypeStringFromKind(kind)));
            var service =
                $"/search?query={Uri.EscapeDataString(query)}&offset={offset}&limit={limit}&facets=[{string.Join(',', facets.Select(x => $"[\"{x.Key}:{x.Value}\"]"))}]";
            return (await GetResourceAsync<HitResponseWrapper>(logger, factory, service, token)).Hits;
        }

        public static async Task<LabrinthProject> GetProjectAsync(ILogger logger, IHttpClientFactory factory,
            string projectId,
            CancellationToken token = default)
        {
            var service = $"/project/{projectId}";
            return await GetResourceAsync<LabrinthProject>(logger, factory, service, token);
        }

        public static async Task<LabrinthVersion> GetVersionAsync(ILogger logger, IHttpClientFactory factory,
            string projectId,
            string versionId, CancellationToken token = default)
        {
            var service = $"/version/{versionId}";
            return await GetResourceAsync<LabrinthVersion>(logger, factory, service, token);
        }

        public static async Task<IEnumerable<LabrinthVersion>> GetVersionsAsync(ILogger logger,
            IHttpClientFactory factory,
            string projectId, CancellationToken token = default)
        {
            var service = $"/project/{projectId}/version";
            return await GetResourcesAsync<LabrinthVersion>(logger, factory, service, token);
        }

        public static async Task<IEnumerable<LabrinthTeamMember>> GetTeamMembersAsync(ILogger logger,
            IHttpClientFactory factory, string teamId,
            CancellationToken token = default)
        {
            var service = $"/team/{teamId}/members";
            return await GetResourcesAsync<LabrinthTeamMember>(logger, factory, service, token);
        }

        public static async Task<Project> GetIntoProjectAsync(ILogger logger, IHttpClientFactory factory,
            string projectId,
            CancellationToken token = default)
        {
            var project = await GetProjectAsync(logger, factory, projectId, token);
            var versions = await GetVersionsAsync(logger, factory, projectId, token);
            var members = await GetTeamMembersAsync(logger, factory, project.Team, token);
            return new Project(project.Id ?? project.Slug,
                project.Title,
                RepositoryLabels.MODRINTH,
                project.IconUrl,
                string.Join(", ", members.Select(x => x.User.Username)),
                project.Description,
                new Uri(PROJECT_URL.Replace("{0}", project.ProjectType).Replace("{1}", project.Slug)),
                GetKindFromTypeString(project.ProjectType),
                project.Published,
                project.Updated ?? project.Published,
                project.Downloads,
                project.Body,
                project.Gallery.Select(x => new Project.Screenshot(x.Title, x.Url)),
                versions.Select(x =>
                {
                    var first = x.Files.First();
                    return new Project.Version(x.Id, x.VersionNumber, x.Changelog, x.ExtractReleaseType(),
                        x.DatePublished, first.Filename, first.Hashes.Sha1, first.Url, x.ExtractRequirement(),
                        x.ExtractDependencies());
                }));
        }

        public static async Task<Package> GetIntoPackageAsync(ILogger logger, IHttpClientFactory factory,
            string projectId, string? versionId, string? gameVersion, string? modLoader,
            CancellationToken token = default)
        {
            var project = await GetProjectAsync(logger, factory, projectId, token);
            LabrinthVersion? version = null;
            if (versionId != null)
            {
                version = await GetVersionAsync(logger, factory, projectId, versionId, token);
            }
            else
            {
                var versions = (await GetVersionsAsync(logger, factory, projectId, token)).ToArray();
                var filtered = versions.Where(x =>
                {
                    var valid = true;
                    var game = gameVersion == null || x.GameVersions.Contains(gameVersion);
                    if (modLoader != null && MODLOADER_MAPPINGS.Values.Contains(modLoader))
                    {
                        var loaderName = MODLOADER_MAPPINGS.First(y => y.Value == modLoader).Key;
                        var loader = x.Loaders.Contains(loaderName);
                        return valid && game && loader;
                    }

                    return valid && game;
                }).ToArray();
                if (filtered.Any())
                {
                    version = filtered.MaxBy(x => x.DatePublished);
                }
            }

            if (version.HasValue)
            {
                var members = await GetTeamMembersAsync(logger, factory, project.Team, token);
                var first = version.Value.Files.First();
                var package = new Package(project.Id ?? project.Slug,
                    project.Title,
                    version.Value.Id,
                    version.Value.VersionNumber,
                    RepositoryLabels.MODRINTH,
                    project.IconUrl,
                    string.Join(", ", members.Select(x => x.User.Username)),
                    project.Description,
                    new Uri(PROJECT_URL.Replace("{0}", project.ProjectType).Replace("{1}", project.Slug)),
                    GetKindFromTypeString(project.ProjectType),
                    version.Value.ExtractReleaseType(),
                    version.Value.DatePublished,
                    first.Filename,
                    first.Url,
                    first.Hashes.Sha1,
                    version.Value.ExtractRequirement(),
                    version.Value.ExtractDependencies()
                );
                return package;
            }

            throw new ResourceNotFoundException(
                $"({RepositoryLabels.MODRINTH},{projectId},{versionId ?? "any"},Filter({gameVersion},{modLoader}))");
        }

        public struct HitResponseWrapper
        {
            public IEnumerable<LabrinthHit> Hits { get; init; }
            public int Offset { get; init; }
            public int Limit { get; init; }
            public int TotalHits { get; init; }
        }
    }
}