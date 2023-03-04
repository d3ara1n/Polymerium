using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Core;
using Polymerium.Core.StageModels;

namespace Polymerium.App.Stages.Preparing;

public class CheckAccountAvailabilityStage : StageBase
{
    private readonly IGameAccount _account;

    private readonly IFileBaseService _fileBase;

    public CheckAccountAvailabilityStage(IFileBaseService fileBase, IGameAccount account)
    {
        _fileBase = fileBase;
        _account = account;
    }

    public override string StageName => "检查账号可用性";

    public override async Task<Option<StageBase>> StartAsync()
    {
        if (!await _account.ValidateAsync())
        {
            if (await _account.RefreshAsync())
                return Finish();
            return Error("验证账号可用性失败且无法修复");
        }

        return Finish();
    }
}