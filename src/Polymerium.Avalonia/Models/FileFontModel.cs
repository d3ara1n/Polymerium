using Avalonia.Media;

namespace Polymerium.Avalonia.Models;

// 外部字体文件。失败（文件缺失/损坏）时 IsAvailable=false、Preview 回退、FamilyName 退化为文件名。
public sealed class FileFontModel(string path, string familyName, FontFamily preview, bool available)
    : FontModelBase(preview, available)
{
    public string FilePath { get; } = path;

    public string FamilyName { get; } = familyName;

    public override string Raw => $"{FilePath}#{FamilyName}";
}
