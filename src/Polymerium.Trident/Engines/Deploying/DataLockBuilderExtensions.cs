using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Engines.Deploying;

public static class DataLockBuilderExtensions
{
    public static DataLockBuilder AddParcel(
        this DataLockBuilder self,
        string label,
        string? @namespace,
        string pid,
        string vid,
        string source,
        string target,
        Uri url,
        string sha1) =>
        self.AddParcel(new DataLock.Parcel(label, @namespace, pid, vid, source, target, url, sha1));

    // PATCH: 为了适配奇葩 PrismLauncher Meta 的多态数据
    public static DataLockBuilder AddLibraryPrismFlavor(this DataLockBuilder self, string fullname, Uri url)
    {
        var exactUrl = url.AbsoluteUri.EndsWith('/') ? url : new Uri(url.AbsoluteUri + '/');
        // 当迁移到 TridentCore/launcher-meta 的之后移除该函数
        DataLock.Library.Identity id;
        var extension = "jar";
        var index = fullname.IndexOf('@');
        if (index > 0)
        {
            extension = fullname[(index + 1)..];
            fullname = fullname[..index];
        }

        var split = fullname.Split(':');
        if (split.Length == 4)
            id = new DataLock.Library.Identity(split[0], split[1], split[2], split[3], extension);
        else if (split.Length == 3)
            id = new DataLock.Library.Identity(split[0], split[1], split[2], null, extension);
        else
            throw new NotSupportedException($"Not recognized package name format: {fullname}");

        var fullUrl = new Uri(exactUrl,
                              $"{id.Namespace.Replace('.', '/')}/{id.Name}/{id.Version}/{id.Name}-{id.Version}.{extension}");
        return self.AddLibrary(new DataLock.Library(id, fullUrl, null));
    }


    public static DataLockBuilder AddLibrary(
        this DataLockBuilder self,
        string fullname,
        Uri url,
        string sha1,
        bool native = false,
        bool present = true)
    {
        DataLock.Library.Identity id;
        var extension = "jar";
        var index = fullname.IndexOf('@');
        if (index > 0)
        {
            extension = fullname[(index + 1)..];
            fullname = fullname[..index];
        }

        var split = fullname.Split(':');
        if (split.Length == 4)
            id = new DataLock.Library.Identity(split[0], split[1], split[2], split[3], extension);
        else if (split.Length == 3)
            id = new DataLock.Library.Identity(split[0], split[1], split[2], null, extension);
        else
            throw new NotSupportedException($"Not recognized package name format: {fullname}");

        return self.AddLibrary(new DataLock.Library(id, url, sha1, native, present));
    }

    public static DataLockBuilder SetAssetIndex(this DataLockBuilder self, string id, Uri url, string sha1) =>
        self.SetAssetIndex(new DataLock.AssetData(id, url, sha1));
}