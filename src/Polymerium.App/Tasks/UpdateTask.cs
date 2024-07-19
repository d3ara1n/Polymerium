using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Abstractions;

namespace Polymerium.App.Tasks;
public class UpdateTask : TaskBase
{
    public UpdateTask(string key, Metadata metadata) : base(key, $"Updating {key}...", "Preparing")
    {
    }
}
