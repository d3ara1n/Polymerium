﻿namespace Polymerium.Trident.Models.ModrinthApi;

public record UserInfo(
    string Username,
    string? Name,
    string? Email,
    object? PayoutData,
    string Id,
    Uri AvatarUrl,
    DateTimeOffset Created,
    string Role,
    uint Badges,
    IReadOnlyList<string>? AuthProviders,
    bool? EmailVerified,
    bool? HasPassword,
    bool? HasTotp,
    string? GithubId);