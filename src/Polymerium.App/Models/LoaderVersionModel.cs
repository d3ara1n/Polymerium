using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public record LoaderVersionModel(string Identity, string Version, DateTimeOffset ReleasedAt,bool Highlighted = false)
    {
    }
}
