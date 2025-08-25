using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class InstanceReferenceModel(
        string purl,
        string label,
        string projectName,
        string versionId,
        string versionName,
        Uri? thumbnail,
        Uri sourceUrl) : ModelBase
    {
        #region Direct

        public string Purl => purl;
        public string Label => label;
        public string ProjectName => projectName;
        public string VersionId => versionId;
        public string VersionName => versionName;
        public Uri? Thumbnail => thumbnail;
        public Uri SourceUrl => sourceUrl;

        #endregion
    }
}
