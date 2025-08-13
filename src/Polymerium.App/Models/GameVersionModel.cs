using System;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class GameVersionModel(string name, string type, DateTimeOffset releaseTime) : ModelBase
    {
        public string Name => name;
        public string TypeRaw => type;
        public DateTimeOffset ReleaseTimeRaw => releaseTime;

        public string ReleaseTime { get; } = releaseTime.Humanize();
    }
}
