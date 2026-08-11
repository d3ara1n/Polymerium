using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

// NOTE: 拍平列表项判别键——派生 record 让 _flat.Keys.OfType<Entry>() 直达所有包键；
//  Header 按组来源去重（Source ⟹ Kind，故 Source 唯一）。
public abstract record PackageListKey
{
    public sealed record Header(string Source) : PackageListKey;

    public sealed record Entry(Profile.Rice.Entry Id) : PackageListKey;
}
