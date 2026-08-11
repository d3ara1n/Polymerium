using Avalonia.Media;

namespace Polymerium.Avalonia.Models;

// NOTE: 系统已安装字体——family 不存在时 IsAvailable=false、Preview 回退。
public sealed class SystemFontModel(string familyName, FontFamily preview, bool available)
    : FontModelBase(preview, available)
{
    public string FamilyName { get; } = familyName;

    public override string Raw => FamilyName;
}
