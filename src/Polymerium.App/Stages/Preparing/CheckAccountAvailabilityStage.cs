using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Core;
using Polymerium.Core.Accounts;
using Polymerium.Core.StageModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Stages.Preparing
{
    public class CheckAccountAvailabilityStage : StageBase
    {
        public override string StageName => "检查账号可用性";

        private readonly IFileBaseService _fileBase;
        private readonly IGameAccount _account;

        public CheckAccountAvailabilityStage(IFileBaseService fileBase, IGameAccount account)
        {
            _fileBase = fileBase;
            _account = account;
        }

        public override async Task<Option<StageBase>> StartAsync()
        {
            if (!await _account.ValidateAsync())
            {
                if (await _account.RefreshAsync())
                {
                    return Finish();
                }
                else
                {
                    return Error("验证账号可用性失败且无法修复");
                }
            }
            return Finish();
        }
    }
}
