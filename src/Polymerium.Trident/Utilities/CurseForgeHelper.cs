using Polymerium.Trident.Models.CurseForgeApi;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using FileInfo = Polymerium.Trident.Models.CurseForgeApi.FileInfo;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Utilities
{
    public static class CurseForgeHelper
    {
        public const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
        public const string ENDPOINT = "https://api.curseforge.com";
        public const string PROJECT_URL = "https://www.curseforge.com/minecraft/{0}/{1}";

        public const uint GAME_ID = 432;
        public const uint CLASSID_MODPACK = 4471;
        public const uint CLASSID_MOD = 6;
        public const uint CLASSID_WORLD = 17;
        public const uint CLASSID_DATAPACK = 4546;
        public const uint CLASSID_SHADERPACK = 6552;
        public const uint CLASSID_RESOURCEPACK = 12;


        public static readonly IReadOnlyDictionary<string, string> LOADER_MAPPINGS = new Dictionary<string, string>
        {
            ["Forge"] = LoaderHelper.LOADERID_FORGE,
            ["NeoForge"] = LoaderHelper.LOADERID_NEOFORGE,
            ["Fabric"] = LoaderHelper.LOADERID_FABRIC,
            ["Quilt"] = LoaderHelper.LOADERID_QUILT,
            ["LiteLoader"] = "LiteLoader",
            ["Cauldron"] = "Cauldron",
            ["Any"] = "Any"
        };

        public static ModLoaderTypeModel? LoaderIdToType(string? loader) =>
            loader switch
            {
                LoaderHelper.LOADERID_NEOFORGE => ModLoaderTypeModel.NeoForge,
                LoaderHelper.LOADERID_FORGE => ModLoaderTypeModel.Forge,
                LoaderHelper.LOADERID_FABRIC => ModLoaderTypeModel.Fabric,
                LoaderHelper.LOADERID_QUILT => ModLoaderTypeModel.Quilt,
                _ => null
            };

        public static string? LoaderIdToName(string? loader) =>
            loader switch
            {
                LoaderHelper.LOADERID_NEOFORGE => "NeoForge",
                LoaderHelper.LOADERID_FORGE => "Forge",
                LoaderHelper.LOADERID_FABRIC => "Fabric",
                LoaderHelper.LOADERID_QUILT => "Quilt",
                _ => null
            };

        public static uint? ResourceKindToClassId(ResourceKind? kind) =>
            kind switch
            {
                ResourceKind.Modpack => CLASSID_MODPACK,
                ResourceKind.Mod => CLASSID_MOD,
                ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
                ResourceKind.ShaderPack => CLASSID_SHADERPACK,
                ResourceKind.World => CLASSID_WORLD,
                ResourceKind.DataPack => CLASSID_DATAPACK,
                _ => null
            };

        public static ResourceKind? ClassIdToResourceKind(uint? classId) =>
            classId switch
            {
                CLASSID_MODPACK => ResourceKind.Modpack,
                CLASSID_MOD => ResourceKind.Mod,
                CLASSID_RESOURCEPACK => ResourceKind.ResourcePack,
                CLASSID_SHADERPACK => ResourceKind.ShaderPack,
                CLASSID_WORLD => ResourceKind.World,
                CLASSID_DATAPACK => ResourceKind.DataPack,
                _ => null
            };

        public static string ResourceKindToUrlKind(ResourceKind? kind) =>
            kind switch
            {
                ResourceKind.Modpack => "modpacks",
                ResourceKind.Mod => "mc-mods",
                ResourceKind.World => "worlds",
                ResourceKind.ResourcePack => "texture-packs",
                ResourceKind.ShaderPack => "shaders",
                ResourceKind.DataPack => "data-packs",
                _ => "unknown"
            };

        public static ReleaseType ToReleaseType(FileInfo.FileReleaseType type) =>
            type switch
            {
                FileInfo.FileReleaseType.Alpha => ReleaseType.Alpha,
                FileInfo.FileReleaseType.Beta => ReleaseType.Beta,
                FileInfo.FileReleaseType.Release => ReleaseType.Release,
                _ => ReleaseType.Release
            };

        public static Uri ToDownloadUrl(FileInfo file) =>
            file.DownloadUrl
         ?? new
                Uri($"https://edge.forgecdn.net/files/{file.Id / 1000}/{file.Id % 1000}/{Uri.EscapeDataString(file.FileName)}");

        public static string? ToSha1(FileInfo file) =>
            file.Hashes.Any(x => x.Algo == FileInfo.FileHash.HashAlgo.Sha1)
                ? file.Hashes.First(x => x.Algo == FileInfo.FileHash.HashAlgo.Sha1).Value
                : null;

        public static Requirement ToRequirement(FileInfo file)
        {
            List<string> gameReq = [];
            List<string> loaderReq = [];
            foreach (var version in file.GameVersions.Where(x => x != "Client" && x != "Server"))
            {
                if (LOADER_MAPPINGS.TryGetValue(version, out var loader))
                {
                    loaderReq.Add(loader);
                }
                else
                {
                    gameReq.Add(version);
                }
            }

            return new(gameReq, loaderReq);
        }

        public static IReadOnlyList<Dependency> ToDependencies(string label, FileInfo file) =>
        [
            .. file
              .Dependencies
              .Where(x => x.RelationType is FileInfo.FileDependency.FileRelationType.RequiredDependency
                                         or FileInfo.FileDependency.FileRelationType.OptionalDependency)
              .Select(x => new Dependency(label,
                                          null,
                                          x.ModId.ToString(),
                                          null,
                                          x.RelationType
                                       == FileInfo.FileDependency.FileRelationType.RequiredDependency))
        ];


        public static Exhibit ToExhibit(string label, ModInfo mod) =>
            new(label,
                null,
                mod.Id.ToString(),
                mod.Name,
                mod.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? mod.Logo?.Url : mod.Logo?.ThumbnailUrl,
                mod.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
                mod.Summary,
                ClassIdToResourceKind(mod.ClassId) ?? ResourceKind.Unknown,
                mod.DownloadCount,
                [.. mod.Categories.Select(x => x.Name)],
                mod.Links.WebsiteUrl
             ?? new Uri(PROJECT_URL.Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(mod.ClassId)))),
                mod.DateCreated,
                mod.DateModified);


        public static Package ToPackage(string label, ModInfo mod, FileInfo file) =>
            new(label,
                null,
                mod.Id.ToString(),
                file.Id.ToString(),
                mod.Name,
                file.DisplayName,
                mod.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? mod.Logo?.Url : mod.Logo?.ThumbnailUrl,
                mod.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
                mod.Summary,
                mod.Links.WebsiteUrl
             ?? new Uri(PROJECT_URL.Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(mod.ClassId)))),
                ClassIdToResourceKind(mod.ClassId) ?? ResourceKind.Unknown,
                ToReleaseType(file.ReleaseType),
                file.FileDate,
                ToDownloadUrl(file),
                file.FileLength,
                file.FileName,
                ToSha1(file),
                ToRequirement(file),
                ToDependencies(label, file));

        public static Version ToVersion(string label, FileInfo file) =>
            new(label,
                null,
                file.ModId.ToString(),
                file.Id.ToString(),
                file.DisplayName.Trim(),
                ToReleaseType(file.ReleaseType),
                file.FileDate,
                file.DownloadCount,
                ToRequirement(file),
                ToDependencies(label, file));


        public static Project ToProject(string label, ModInfo info) =>
            new(label,
                null,
                info.Id.ToString(),
                info.Name,
                info.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? info.Logo?.Url : info.Logo?.ThumbnailUrl,
                info.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
                info.Summary,
                info.Links.WebsiteUrl
             ?? new Uri(PROJECT_URL
                       .Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(info.ClassId)))
                       .Replace("{1}", info.Slug)),
                ClassIdToResourceKind(info.ClassId) ?? ResourceKind.Unknown,
                [.. info.Categories.Select(x => x.Name)],
                info.DateCreated,
                info.DateModified,
                info.DownloadCount,
                [.. info.Screenshots.Select(x => new Project.Screenshot(x.Title, x.Url))]);
    }
}
