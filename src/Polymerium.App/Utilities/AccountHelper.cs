using System;
using System.Text.Json;
using Polymerium.App.Services;
using Polymerium.Trident.Accounts;
using Trident.Abstractions.Accounts;

namespace Polymerium.App.Utilities
{
    public static class AccountHelper
    {
        public static IAccount ToCooked(PersistenceService.Account raw) =>
            (IAccount?)(raw.Kind switch
            {
                nameof(MicrosoftAccount) => JsonSerializer.Deserialize<MicrosoftAccount>(raw.Data),
                nameof(TrialAccount) => JsonSerializer.Deserialize<TrialAccount>(raw.Data),
                nameof(OfflineAccount) => JsonSerializer.Deserialize<OfflineAccount>(raw.Data),
                _ => JsonSerializer.Deserialize<OfflineAccount>(raw.Data)
            })
         ?? throw new FormatException("Failed to deserialize account from the raw data");

        public static PersistenceService.Account ToRaw(
            IAccount account,
            DateTimeOffset enrolledAt,
            DateTimeOffset? lastUsedAt,
            bool isDefault) =>
            new(account.Uuid,
                account.GetType().Name,
                JsonSerializer.Serialize(account, account.GetType()),
                enrolledAt.DateTime,
                lastUsedAt?.DateTime,
                isDefault);

        public static string ToRaw(IAccount account) => JsonSerializer.Serialize(account, account.GetType());
    }
}
