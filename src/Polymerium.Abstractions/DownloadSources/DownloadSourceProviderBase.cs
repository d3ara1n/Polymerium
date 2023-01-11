using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.DownloadSources.Models;

namespace Polymerium.Abstractions.DownloadSources
{
    public abstract class DownloadSourceProviderBase
    {
        public abstract string Identity { get; }

        public virtual Task<Option<IEnumerable<GameVersion>>> GetGameVersionsAsync() => Task.FromResult<Option<IEnumerable<GameVersion>>>(Option<IEnumerable<GameVersion>>.None());
    }
}
