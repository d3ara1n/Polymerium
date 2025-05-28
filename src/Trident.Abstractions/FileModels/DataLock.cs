namespace Trident.Abstractions.FileModels;

// 相对于 TridentV1 的 Artifact，DataLock 可以迁移
public record DataLock(
    DataLock.ViabilityData Viability,
    string MainClass,
    uint JavaMajorVersion,
    IReadOnlyList<string> GameArguments,
    IReadOnlyList<string> JavaArguments,
    IReadOnlyList<DataLock.Library> Libraries,
    IReadOnlyList<DataLock.Parcel> Parcels,
    DataLock.AssetData AssetIndex)
{
    public const int FORMAT = 1;

    #region Nested type: AssetData

    public record AssetData(string Id, Uri Url, string Sha1);

    #endregion

    #region Nested type: Library

    // IsNative 决定是否解压到 Natives 目录，IsPresent 决定是否添加到 ClassPath，两者互不干扰
    public record Library(Library.Identity Id, Uri Url, string? Sha1, bool IsNative = false, bool IsPresent = true)
    {
        #region Nested type: Identity

        public record Identity(string Namespace, string Name, string Version, string? Platform, string Extension);

        #endregion
    }

    #endregion

    #region Nested type: Parcel

    // 解析之后的包，会被软链接到目标目录。
    // 同样软链接的还有 Persistence 清单
    // Import 导入位于 Persistence 固化之前，意味着可以把 Import 导入的文件也持久化
    public record Parcel(
        string Label,
        string? Namespace,
        string Pid,
        string Vid,
        string Path,
        Uri Download,
        string? Sha1);

    #endregion

    #region Nested type: ViabilityData

    // 对于 github:user/package 这种没有标记 version 也就是特定 commit/release 的，会视为有效，本着构建完尽可能不修改原则
    // Home 是 Trident Home，Key 是 Profile Key，这两者结合来保证必须是同一个目录的实例
    public record ViabilityData(
        int Format,
        string Watermark,
        string Home,
        string Key,
        string Version,
        string? Loader,
        IReadOnlyList<string> Packages);

    #endregion
}