﻿namespace Polymerium.Trident.Models.XboxLiveApi;

public record XboxLiveTokenProperties(
    string RpsTicket,
    string SiteName = "user.auth.xboxlive.com",
    string AuthMethod = "RPS");