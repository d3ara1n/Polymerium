using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FreeSql.DataAnnotations;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Core.Accounts;

namespace Polymerium.Avalonia.Services;

public class PersistenceService(IFreeSql freeSql)
{
    #region ActionKind enum

    public enum ActionKind
    {
        Install,
        Update,
        Unlock,
        Reset,
        Rename,
        EditPackage,
        EditLoader,
    }

    #endregion

    #region Nested type: Account

    public class Account
    {
        [Column(IsPrimary = true)]
        public required string Uuid { get; set; }

        public required string Kind { get; set; }

        [Column(DbType = "BLOB")]
        public required string Data { get; set; }

        public required DateTime EnrolledAt { get; set; }
        public required DateTime? LastUsedAt { get; set; }
        public required bool IsDefault { get; set; }
    }

    #endregion

    #region Nested type: AccountSelector

    public class AccountSelector
    {
        [Column(IsPrimary = true)]
        public required string Key { get; set; }

        public required string Uuid { get; set; }
    }

    #endregion

    #region Nested type: Action

    public class Action
    {
        public DateTime At { get; set; } = DateTime.Now;
        public required string Key { get; set; }

        public required ActionKind Kind { get; set; }
        public string? Old { get; set; }
        public string? New { get; set; }
    }

    #endregion

    #region Nested type: Activity

    public class Activity
    {
        public required string Key { get; set; }

        public required DateTime Begin { get; set; }
        public required DateTime End { get; set; }
        public required string AccountId { get; set; }
        public required bool DieInPeace { get; set; }
    }

    #endregion

    #region Nested type: WidgetLocalSection

    public class WidgetLocalSection
    {
        [Column(IsPrimary = true)]
        public required string Key { get; set; }

        [Column(IsPrimary = true)]
        public required string WidgetId { get; set; }

        [Column(IsPrimary = true)]
        public required string Indicator { get; set; }

        [Column(DbType = "BLOB")]
        public required string Data { get; set; }
    }

    #endregion

    #region Nested type: ViewState
    public class ViewState
    {
        [Column(IsPrimary = true)]
        public required string Key { get; set; }

        [Column(DbType = "BLOB")]
        public required string Data { get; set; }
    }
    #endregion

    #region Nested type: FavoriteProject

    public class FavoriteProject
    {
        [Column(IsPrimary = true)]
        public required string Label { get; set; }

        [Column(IsPrimary = true)]
        public required string Namespace { get; set; }

        [Column(IsPrimary = true)]
        public required string ProjectId { get; set; }

        public required string ProjectName { get; set; }
        public required string Author { get; set; }
        public required string Summary { get; set; }
        public required string Reference { get; set; }
        public string? Thumbnail { get; set; }
        public required ResourceKind Kind { get; set; }
        public required ulong DownloadCount { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        [Column(DbType = "BLOB")]
        public required string Tags { get; set; }
    }

    #endregion

    #region Nested type: PinnedInstance

    public class PinnedInstance
    {
        [Column(IsPrimary = true)]
        public required string Key { get; set; }
    }

    #endregion

    #region Nested type: InstanceTag

    public class InstanceTag
    {
        [Column(IsPrimary = true)]
        public required string Key { get; set; }

        [Column(IsPrimary = true)]
        public required string Tag { get; set; }
    }

    #endregion


    #region Actions

    public void AppendAction(Action action) => freeSql.Insert(action).ExecuteAffrows();

    public IReadOnlyList<Action> GetActions(string key, int pageIndex, int pageSize, out int totalCount)
    {
        var query = freeSql
            .Select<Action>()
            .Where(x => x.Key == key && x.Kind == ActionKind.EditPackage);

        totalCount = (int)query.Count();

        return query.OrderByDescending(x => x.At)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public IReadOnlyList<Action> GetActions(string key) =>
        freeSql
            .Select<Action>()
            .Where(x => x.Key == key && x.Kind == ActionKind.EditPackage)
            .OrderByDescending(x => x.At)
            .ToList();

    public int ClearActions(string key) =>
        freeSql.Delete<Action>().Where(x => x.Key == key).ExecuteAffrows();

    public int ClearAllActions() => freeSql.Delete<Action>().Where("1=1").ExecuteAffrows();

    #endregion

    #region Activity

    public void AppendActivity(Activity activity) => freeSql.Insert(activity).ExecuteAffrows();

    public Activity? GetLastActivity(string key) =>
        freeSql.Select<Activity>().Where(x => x.Key == key).OrderByDescending(x => x.End).First();

    public Activity? GetLastActivity() =>
        freeSql.Select<Activity>().OrderByDescending(x => x.End).First();

    public TimeSpan GetTotalPlayTime(string key)
    {
        var totalSeconds = freeSql
            .Select<Activity>()
            .Where(x => x.Key == key)
            .Sum(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds((double)totalSeconds);
    }

    public TimeSpan GetTotalPlayTime()
    {
        var totalSeconds = freeSql.Select<Activity>().Sum(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds((double)totalSeconds);
    }

    public int GetActiveDays(string key) =>
        (int)freeSql.Select<Activity>().Where(x => x.Key == key).GroupBy(x => x.End.Date).Count();

    public int GetActiveDays() => (int)freeSql.Select<Activity>().GroupBy(x => x.End.Date).Count();

    public int GetSessionCount(string key) =>
        (int)freeSql.Select<Activity>().Where(x => x.Key == key).Count();

    public int GetSessionCount() => (int)freeSql.Select<Activity>().Count();

    public int GetCrashCount(string key) =>
        (int)freeSql.Select<Activity>().Where(x => x.Key == key && !x.DieInPeace).Count();

    public int GetCrashCount() => (int)freeSql.Select<Activity>().Where(x => !x.DieInPeace).Count();

    public int GetTotalPlayTimeRank(string key)
    {
        var allActivities = freeSql.Select<Activity>().ToList();

        var playTimes = allActivities
            .GroupBy(x => x.Key)
            .Select(g => new { g.Key, TotalHours = g.Sum(x => (x.End - x.Begin).TotalHours) })
            .OrderByDescending(x => x.TotalHours)
            .ToList();

        var rank = playTimes.FindIndex(x => x.Key == key);
        return rank == -1 ? playTimes.Count + 1 : rank + 1;
    }

    public TimeSpan GetDayPlayTime(string key, DateTimeOffset date)
    {
        var totalSeconds = freeSql
            .Select<Activity>()
            .Where(x => x.Key == key && x.End.Date == DateTimeHelper.ToPersistedLocalDateTime(date).Date)
            .Sum(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds((double)totalSeconds);
    }

    public double GetPercentageInTotalPlayTime(string key)
    {
        var keyTotalSeconds = freeSql
            .Select<Activity>()
            .Where(x => x.Key == key)
            .Sum(x => (x.End - x.Begin).TotalSeconds);

        var allTotalSeconds = freeSql.Select<Activity>().Sum(x => (x.End - x.Begin).TotalSeconds);

        if (allTotalSeconds == 0)
        {
            return 0d;
        }

        return (double)(keyTotalSeconds / allTotalSeconds);
    }

    public Activity? GetFirstActivity(string key) =>
        freeSql.Select<Activity>().Where(x => x.Key == key).OrderBy(x => x.Begin).First();

    public TimeSpan GetLongestSession(string key)
    {
        var activities = freeSql.Select<Activity>().Where(x => x.Key == key).ToList();
        if (activities.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var longest = activities.Max(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds(longest);
    }

    public TimeSpan GetWeekPlayTime(string key, int weeksAgo)
    {
        var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - weeksAgo * 7);
        var weekEnd = weekStart.AddDays(7);
        var totalSeconds = freeSql
            .Select<Activity>()
            .Where(x => x.Key == key && x.End >= weekStart && x.End < weekEnd)
            .Sum(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds((double)totalSeconds);
    }

    public int ClearActivities(string key) =>
        freeSql.Delete<Activity>().Where(x => x.Key == key).ExecuteAffrows();

    public int ClearAllActivities() => freeSql.Delete<Activity>().Where("1=1").ExecuteAffrows();

    #endregion

    #region Accounts

    public void AppendAccount(Account account) => freeSql.Insert(account).ExecuteAffrows();

    public IReadOnlyList<Account> GetAccounts() => freeSql.Select<Account>().ToList();

    public Account? GetDefaultAccount() =>
        freeSql.Select<Account>().Where(x => x.IsDefault).First();

    public Account? GetAccount(string uuid) =>
        freeSql.Select<Account>().Where(x => x.Uuid == uuid).First();

    public bool HasMicrosoftAccount() =>
        freeSql.Select<Account>().Where(x => x.Kind == nameof(MicrosoftAccount)).Any();

    public void MarkDefaultAccount(string uuid) =>
        freeSql.Transaction(() =>
        {
            freeSql.Update<Account>().Set(x => x.IsDefault, false).ExecuteAffrows();
            freeSql
                .Update<Account>()
                .Where(x => x.Uuid == uuid)
                .Set(x => x.IsDefault, true)
                .ExecuteAffrows();
        });

    public void RemoveAccount(string uuid) =>
        freeSql.Delete<Account>().Where(x => x.Uuid == uuid).ExecuteAffrows();

    public void UseAccount(string uuid) =>
        freeSql
            .Update<Account>()
            .Where(x => x.Uuid == uuid)
            .Set(x => x.LastUsedAt, DateTime.Now)
            .ExecuteAffrows();

    public void UpdateAccount(string uuid, string data) =>
        freeSql
            .Update<Account>()
            .Where(x => x.Uuid == uuid)
            .Set(x => x.Data, data)
            .ExecuteAffrows();

    #endregion

    #region AccountSelectors

    public AccountSelector? GetAccountSelector(string key) =>
        freeSql.Select<AccountSelector>().Where(x => x.Key == key).First();

    public void SetAccountSelector(string key, string uuid) =>
        freeSql.Transaction(() =>
        {
            var found = freeSql.Select<AccountSelector>().Where(x => x.Key == key).First();
            if (found != null)
            {
                if (found.Uuid != uuid)
                {
                    freeSql
                        .Update<AccountSelector>()
                        .Where(x => x.Key == key)
                        .Set(x => x.Uuid, uuid)
                        .ExecuteAffrows();
                }
            }
            else
            {
                freeSql.Insert(new AccountSelector { Key = key, Uuid = uuid }).ExecuteAffrows();
            }
        });

    #endregion

    #region PinnedInstance

    public bool IsPinnedInstance(string key) => freeSql.Select<PinnedInstance>().Where(x => x.Key == key).Any();

    public IEnumerable<string> GetPinnedInstanceKeys() => freeSql.Select<PinnedInstance>().ToList(x => x.Key);

    public void SetPinnedInstance(string key, bool pinned)
    {
        if (pinned)
        {
            freeSql.InsertOrUpdate<PinnedInstance>().SetSource(new() { Key = key }).ExecuteAffrows();
        }
        else
        {
            freeSql.Delete<PinnedInstance>().Where(x => x.Key == key).ExecuteAffrows();
        }
    }

    #endregion

    #region Widgets

    public T? GetWidgetLocalData<T>(string key, string widgetId, string indicator)
    {
        var data = freeSql
            .Select<WidgetLocalSection>()
            .Where(x => x.Key == key && x.WidgetId == widgetId && x.Indicator == indicator)
            .First();
        return data == null ? default : JsonSerializer.Deserialize<T>(data.Data);
    }

    public void SetWidgetLocalData<T>(string key, string widgetId, string indicator, T data)
    {
        var serializedData = JsonSerializer.Serialize(data);
        var widgetSection = new WidgetLocalSection
        {
            Key = key,
            WidgetId = widgetId,
            Indicator = indicator,
            Data = serializedData,
        };

        freeSql.InsertOrUpdate<WidgetLocalSection>().SetSource(widgetSection).ExecuteAffrows();
    }

    public string[] GetInstanceTags(string key) =>
        freeSql.Select<InstanceTag>().Where(x => x.Key == key).ToList(x => x.Tag).ToArray();

    public void SetInstanceTags(string key, string[] tags)
    {
        freeSql.Delete<InstanceTag>().Where(x => x.Key == key).ExecuteAffrows();
        foreach (var tag in tags)
        {
            freeSql.Insert(new InstanceTag { Key = key, Tag = tag }).ExecuteAffrows();
        }
    }

    public void RemoveInstanceTags(string key) =>
        freeSql.Delete<InstanceTag>().Where(x => x.Key == key).ExecuteAffrows();

    #endregion

    #region ViewStates

    public object? GetViewState(string key, Type type)
    {
        var data = freeSql.Select<ViewState>().Where(x => x.Key == key).First();
        return data == null ? default : JsonSerializer.Deserialize(data.Data, type);
    }

    public void SetViewState(string key, object data)
    {
        var serializedData = JsonSerializer.Serialize(data);
        var state = new ViewState() { Key = key, Data = serializedData };
        freeSql.InsertOrUpdate<ViewState>().SetSource(state).ExecuteAffrows();
    }

    #endregion

    #region Favorites

    public bool IsFavoriteProject(string label, string? ns, string projectId) =>
        freeSql
            .Select<FavoriteProject>()
            .Where(x => x.Label == label && x.Namespace == (ns ?? string.Empty) && x.ProjectId == projectId)
            .Any();

    public void AddFavoriteProject(Project project) =>
        AddFavoriteProject(
            project.Label,
            project.Namespace,
            project.ProjectId,
            project.ProjectName,
            project.Author,
            project.Summary,
            project.Reference,
            project.Thumbnail,
            project.Kind,
            project.DownloadCount,
            project.Tags,
            project.CreatedAt,
            project.UpdatedAt
        );

    public void AddFavoriteProject(
        string label,
        string? ns,
        string projectId,
        string projectName,
        string author,
        string summary,
        Uri reference,
        Uri? thumbnail,
        ResourceKind kind,
        ulong downloadCount,
        IReadOnlyList<string> tags,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt
    )
    {
        var existing = freeSql
            .Select<FavoriteProject>()
            .Where(x => x.Label == label && x.Namespace == (ns ?? string.Empty) && x.ProjectId == projectId)
            .First();
        var favorite = new FavoriteProject
        {
            Label = label,
            Namespace = ns ?? string.Empty,
            ProjectId = projectId,
            ProjectName = projectName,
            Author = author,
            Summary = summary,
            Reference = reference.AbsoluteUri,
            Thumbnail = thumbnail?.AbsoluteUri,
            Kind = kind,
            DownloadCount = downloadCount,
            CreatedAt = DateTimeHelper.ToPersistedLocalDateTime(createdAt),
            UpdatedAt = DateTimeHelper.ToPersistedLocalDateTime(updatedAt),
            AddedAt = existing?.AddedAt ?? DateTime.Now,
            Tags = JsonSerializer.Serialize(tags),
        };

        freeSql.InsertOrUpdate<FavoriteProject>().SetSource(favorite).ExecuteAffrows();
    }

    public int RemoveFavoriteProject(string label, string? ns, string projectId) =>
        freeSql
            .Delete<FavoriteProject>()
            .Where(x => x.Label == label && x.Namespace == (ns ?? string.Empty) && x.ProjectId == projectId)
            .ExecuteAffrows();

    public IReadOnlyList<FavoriteProject> SearchFavoriteProjects(
        string query,
        Filter filter,
        uint pageIndex,
        uint pageSize,
        out int totalCount
    )
    {
        var normalizedQuery = query.Trim();
        var filtered = freeSql
            .Select<FavoriteProject>()
            .ToList()
            .Where(x => filter.Kind is null || x.Kind == filter.Kind)
            .Where(x => IsFavoriteQueryMatched(x, normalizedQuery))
            .OrderByDescending(x => x.AddedAt)
            .ThenByDescending(x => x.UpdatedAt)
            .ToList();

        totalCount = filtered.Count;
        return filtered.Skip((int)(pageIndex * pageSize)).Take((int)pageSize).ToList();
    }

    public static string? NormalizeFavoriteNamespace(string ns) =>
        string.IsNullOrEmpty(ns) ? null : ns;

    public static IReadOnlyList<string> DeserializeFavoriteTags(string tags) =>
        JsonSerializer.Deserialize<IReadOnlyList<string>>(tags) ?? [];

    private static bool IsFavoriteQueryMatched(FavoriteProject favorite, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return favorite.ProjectName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || favorite.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
            || favorite.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || DeserializeFavoriteTags(favorite.Tags)
                .Any(x => x.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
