using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.Trident.Engines.Launching;

namespace Polymerium.App.Models
{
    public partial class ScrapModel(
        string message,
        ScrapLevel level,
        DateTimeOffset time,
        string thread,
        string sender) : ModelBase
    {
        #region Reactive

        [ObservableProperty]
        public partial string Message { get; set; } = message;

        #endregion

        #region Direct

        public ScrapLevel Level => level;
        public DateTimeOffset Time => time;
        public string Thread => thread;
        public string Sender => sender;

        #endregion
    }
}
