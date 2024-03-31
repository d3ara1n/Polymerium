using Trident.Abstractions.Building;

namespace Trident.Abstractions.Extensions
{
    public static class ArtifactBuilderExtensions
    {

        public static ArtifactBuilder SetViability(this ArtifactBuilder self, string key, string watermark, string home)
        {
            return self.SetViability(new Artifact.ViabilityData(key, watermark, home));
        }

        public static ArtifactBuilder AddParcel(this ArtifactBuilder self, string source, string target, Uri url,
            string? sha1)
        {
            return self.AddParcel(new Artifact.Parcel(source, target, url, sha1));
        }

        // PATCH: 为了适配奇葩 PrismLauncher Meta 的多态数据
        public static ArtifactBuilder AddLibraryPrismFlavor(this ArtifactBuilder self, string fullname, Uri url)
        {
            var exactUrl = url.AbsoluteUri.EndsWith('/') ? url : new Uri(url.AbsoluteUri + '/');
            // 当迁移到 TridentCore/launcher-meta 的之后移除该函数
            Artifact.Library.Identity id;
            var extension = "jar";
            var index = fullname.IndexOf('@');
            if (index > 0)
            {
                extension = fullname[(index + 1)..];
                fullname = fullname[..index];
            }

            var split = fullname.Split(':');
            if (split.Length == 4)
            {
                id = new Artifact.Library.Identity(split[0], split[1], split[2], split[3], extension);
            }
            else if (split.Length == 3)
            {
                id = new Artifact.Library.Identity(split[0], split[1], split[2], null, extension);
            }
            else
            {
                throw new NotSupportedException($"Not recognized package name format: {fullname}");
            }

            var fullUrl = new Uri(exactUrl, $"{id.Namespace.Replace('.', '/')}/{id.Name}/{id.Version}/{id.Name}-{id.Version}.{extension}");
            return self.AddLibrary(new Artifact.Library(id, fullUrl, null, false, true));
        }


        public static ArtifactBuilder AddLibrary(this ArtifactBuilder self, string fullname, Uri url, string sha1,
            bool native = false, bool present = true)
        {
            Artifact.Library.Identity id;
            var extension = "jar";
            var index = fullname.IndexOf('@');
            if (index > 0)
            {
                extension = fullname[(index + 1)..];
                fullname = fullname[..index];
            }

            var split = fullname.Split(':');
            if (split.Length == 4)
            {
                id = new Artifact.Library.Identity(split[0], split[1], split[2], split[3], extension);
            }
            else if (split.Length == 3)
            {
                id = new Artifact.Library.Identity(split[0], split[1], split[2], null, extension);
            }
            else
            {
                throw new NotSupportedException($"Not recognized package name format: {fullname}");
            }

            return self.AddLibrary(new Artifact.Library(id, url, sha1, native, present));
        }

        public static ArtifactBuilder AddProcessor(this ArtifactBuilder self, string id, string data, string? condition)
        {
            return self.AddProcessor(new Artifact.Processor(condition, id, data));
        }

        public static ArtifactBuilder SetAssetIndex(this ArtifactBuilder self, string id, Uri url, string sha1)
        {
            return self.SetAssetIndex(new Artifact.AssetData(id, url, sha1));
        }
    }
}