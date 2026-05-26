using System;

namespace Polymerium.App.Utilities;

public static class DateTimeHelper
{
    public static DateTime ToPersistedLocalDateTime(DateTimeOffset value) => value.LocalDateTime;

    public static DateTime? ToPersistedLocalDateTime(DateTimeOffset? value) =>
        value?.LocalDateTime;

    public static DateTimeOffset FromPersistedLocalDateTime(DateTime value)
    {
        var local = value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Local => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local),
        };
        return new(local);
    }

    public static DateTimeOffset? FromPersistedLocalDateTime(DateTime? value) =>
        value is { } local ? FromPersistedLocalDateTime(local) : null;
}
