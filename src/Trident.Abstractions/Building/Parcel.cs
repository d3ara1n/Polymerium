namespace Trident.Abstractions.Building;

public record Parcel(string SourcePath, string TargetPath, Uri? Url, string? Sha1, bool IsFragile)
{
    // 根据 Sha1 检查 SourcePath 文件有效性，无效则下载，无法下载就报错。
    // 根据 IsFragile 决定是复制还是创建软连接。
}