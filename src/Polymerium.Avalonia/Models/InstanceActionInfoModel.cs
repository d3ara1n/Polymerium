using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

// NOTE: 一次包变更解析成功后的展示数据——Old/New 版本名各自可空（Update 一侧解析失败则留空），
//  项目级数据取能解析到的那一侧。
public class InstanceActionInfoModel(
    string projectName,
    string? oldVersionName,
    string? newVersionName,
    Bitmap thumbnail) : ModelBase
{
    public string ProjectName => projectName;
    public string? OldVersionName => oldVersionName;
    public string? NewVersionName => newVersionName;
    public Bitmap Thumbnail => thumbnail;
}
