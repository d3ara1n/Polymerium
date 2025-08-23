using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class MicrosoftUserCodeModel(string deviceCode, string userCode, Uri verificationUri) : ModelBase
    {
        #region Direct

        public string DeviceCode => deviceCode;
        public string UserCode => userCode;
        public Uri VerificationUri => verificationUri;

        #endregion
    }
}
