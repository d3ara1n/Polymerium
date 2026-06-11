using System;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class MicrosoftUserCodeModel(string deviceCode, string userCode, Uri verificationUri)
    : ModelBase
{
    #region Direct

    public string DeviceCode => deviceCode;
    public string UserCode => userCode;
    public Uri VerificationUri => verificationUri;

    #endregion
}
