using System;
using System.Collections.ObjectModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class ScreenshotGroupModel(DateTimeOffset date) : ModelBase
    {
        #region Reactive

        public ObservableCollection<ScreenshotModel> Screenshots { get; } = [];

        #endregion

        #region Direct

        public DateTimeOffset DateRaw => date;
        public string Date => DateRaw.ToString("D");

        #endregion
    }
}
