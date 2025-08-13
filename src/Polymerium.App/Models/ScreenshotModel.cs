using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class ScreenshotModel(Uri image, DateTimeOffset time) : ModelBase
    {
        #region Direct

        public DateTimeOffset TimeRaw => time;
        public string Time => TimeRaw.ToString("T");

        public Uri Image => image;

        #endregion
    }
}
