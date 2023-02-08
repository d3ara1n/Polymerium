using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Models;

public class AccountItemModel
{
    public AccountItemModel(IGameAccount inner, string avatarFaceSource, string avatarBustSource)
    {
        Inner = inner;
        AvatarFaceSource = avatarFaceSource;
        AvatarBustSource = avatarBustSource;
    }

    public IGameAccount Inner { get; set; }

    // 指向皮肤文件，缓存到本地之后分解成正面和脸部
    public string AvatarFaceSource { get; set; }

    public string AvatarBustSource { get; set; }
}