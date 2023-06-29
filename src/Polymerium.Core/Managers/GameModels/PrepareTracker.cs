using Polymerium.Abstractions;
using Polymerium.Core.Engines.Restoring;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Core.Managers.GameModels
{
    public class PrepareTracker
    {
        public PrepareTracker(GameInstance instance)
        {
            Instance = instance;
            TokenSource = new CancellationTokenSource();
        }

        public GameInstance Instance { get; set; }
        public CancellationTokenSource TokenSource { get; }

        // precentage null for indeterminate, success null for processing
        public Action<int?>? UpdateCallback { get; set; }

        public Action<bool, PrepareError?, Exception?, RestoreError?>? FinishCallback { get; set; }

        internal Task? Task { get; set; }
    }
}
