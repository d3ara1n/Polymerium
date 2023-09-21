using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.ExtraData
{
    public record TodoItem
    {
        public TodoItem(string content, DateTimeOffset createdAt, DateTimeOffset? completedAt = null)
        {
            Content = content;
            CreatedAt = createdAt;
            CompletedAt = completedAt;
        }

        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
