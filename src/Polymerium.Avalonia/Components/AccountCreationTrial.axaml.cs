using System;
using Polymerium.Avalonia.Controls;
using TridentCore.Core.Accounts;

namespace Polymerium.Avalonia.Components;

public partial class AccountCreationTrial : AccountCreationStep
{
    public AccountCreationTrial()
    {
        InitializeComponent();
        // 愚人节彩蛋：仅在 4 月 1 日显示 Herobrine 试用账号（index 5 恒定占位，平日隐藏不可选）。
        HerobrineItem.IsVisible = DateTime.Now is { Month: 4, Day: 1 };
    }

    public override object NextStep() =>
        new AccountCreationPreview
        {
            Account = RoleSelectBox.SelectedIndex switch
            {
                0 => TrialAccount.CreateStewie(),
                1 => TrialAccount.CreateBrian(),
                2 => TrialAccount.CreateChris(),
                3 => TrialAccount.CreatePeter(),
                4 => TrialAccount.CreateLois(),
                5 => TrialAccount.CreateHerobrine(),
                _ => throw new ArgumentOutOfRangeException(),
            },
        };
}
