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
    // 对于 github:user/package 这种没有标记 version 也就是特定 commit/release 的，会视为有效，本着构建完尽可能不修改原则
    public record ViabilityData(string Version, string Loader, IReadOnlyList<string> Packages);


    // IsNative 决定是否解压到 Natives 目录，IsPresent 决定是否添加到 ClassPath，两者互不干扰
    public record Library(Library.Identity Id, Uri Url, string? Sha1, bool IsNative = false, bool IsPresent = true)
    {
        public record Identity(string Namespace, string Name, string Version, string? Platform, string Extension);
    }

    // 解析之后的包，会被软链接到目标目录。
    // 同样软链接的还有 Persistence 清单
    // Import 导入位于 Persistence 固化之前，意味着可以把 Import 导入的文件也持久化
    public record Parcel(string SourcePath, string TargetPath, Uri Download, string Sha1);

    public record AssetData(string Id, Uri Url, string Sha1);
}