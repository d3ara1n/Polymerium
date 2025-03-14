namespace Polymerium.Trident.Engines;

public class DeployOptions(string? javaHomeOverride = null)
{
    // 目前方案无法解决下载来的 Java.zip 目录通过 Explosive 解压之后重命名为指定文件名的问题，无法实现自动部署运行时
    public string? JavaHomeOverride => javaHomeOverride;

    // TODO: Persistent & Snapshots
}