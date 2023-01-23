using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class GameVersionModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public DateTimeOffset Time { get; set; }
        public DateTimeOffset ReleaseTime { get; set; }
    }
}
