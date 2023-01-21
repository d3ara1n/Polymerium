using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Polymerium.App.Data
{
    public abstract class RefinedModelBase<T>
    {
        [JsonIgnore]
        public abstract Uri Location { get; }
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

        [JsonIgnore]
        public virtual JsonSerializerSettings SerializerSettings { get; } = serializerSettings;
        public abstract T Extract();
        public abstract void Apply(T data);
    }
}
