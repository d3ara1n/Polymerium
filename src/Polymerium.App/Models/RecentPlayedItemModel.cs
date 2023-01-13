using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class RecentPlayedItemModel
    {
        public string InstanceId { get; set; }
        public string ThumbnailFile { get; set; }
        public string Name { get; set; }
        public DateTimeOffset LastPlayedAt { get; set; }
    }
}
