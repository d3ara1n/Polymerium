using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FreeSql.DataAnnotations;
using Trident.Core.Accounts;

namespace Polymerium.App.Services;

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


    #region Actions

    public void AppendAction(Action action) => freeSql.Insert(action).ExecuteAffrows();

    public IReadOnlyList<Action> GetLatestActions(string key, DateTimeOffset since) =>
        freeSql
            .Select<Action>()
            .Where(x =>
                x.Key == key && x.Kind == ActionKind.EditPackage && x.At >= since.LocalDateTime
            )
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
            .Where(x => x.Key == key && x.End.Date == date.DateTime.Date)
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
}
