using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.DownloadSources.Models
{
    public class FetchedResult<T>
        where T: struct, IComparable
    {
        private static FetchedResult<T> failure = new FetchedResult<T>() { Success = false };
        public static FetchedResult<T> Failure => failure;

        public bool Success { get; set; }

        public T Data { get; set; }
    }
}
