using Polymerium.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Managers.GameModels
{
    public class RunTracker
    {
        public RunTracker(GameInstance instance)
        {
            Instance = instance;
        }

        public GameInstance Instance { get; set; }
    }
}
