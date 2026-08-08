using System.Globalization;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LanguageModel(CultureInfo info) : ModelBase
{
    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj) => obj is LanguageModel other && other.Id == Id;

    #region Direct

    public string Id => info.Name;
    // NOTE: 语言名用 NativeName（自身语言、文化无关），不随 UI 语言变——语言选择器里每项展示它自己的
    //  母语名是惯例，也避免 DisplayName 那种随 CurrentUICulture 变却不通知导致的刷新问题。
    public string Display => info.NativeName;

    #endregion
}
