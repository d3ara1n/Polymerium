using Polymerium.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Action<bool>? FinishCallback { get; set; }

        internal Task? Task { get; set; }
    }
}
