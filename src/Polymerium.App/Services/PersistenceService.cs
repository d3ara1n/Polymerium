using System;
using System.Collections.Generic;
using System.Text.Json;
using FreeSql.DataAnnotations;
using Polymerium.Trident.Accounts;

namespace Polymerium.App.Services
{
    public class PersistenceService(IFreeSql freeSql)
    {
        #region ActionKind enum

        public enum ActionKind { Install, Update, Unlock, Reset, Rename, EditPackage, EditLoader }

        #endregion

        #region Nested type: Account

        public class Account(
            string uuid,
            string kind,
            string data,
            DateTime enrolledAt,
            DateTime? lastUsedAt,
            bool isDefault)
        {
            [Column(IsPrimary = true)]
            public string Uuid { get; set; } = uuid;

            public string Kind { get; set; } = kind;

            [Column(DbType = "BLOB")]
            public string Data { get; set; } = data;

            public DateTime EnrolledAt { get; set; } = enrolledAt;
            public DateTime? LastUsedAt { get; set; } = lastUsedAt;
            public bool IsDefault { get; set; } = isDefault;
        }

        #endregion

        #region Nested type: AccountSelector

        public class AccountSelector(string key, string uuid)
        {
            [Column(IsPrimary = true)]
            public string Key { get; set; } = key;

            public string Uuid { get; set; } = uuid;
        }

        #endregion

        #region Nested type: Action

        public class Action(string key, ActionKind kind, string? old, string? @new)
        {
            public DateTime At { get; set; } = DateTime.Now;
            public string Key { get; set; } = key;

            public ActionKind Kind { get; set; } = kind;
            public string? Old { get; set; } = old;
            public string? New { get; set; } = @new;
        }

        #endregion

        #region Nested type: Activity

        public class Activity(string key, DateTimeOffset begin, DateTimeOffset end, bool dieInPeace)
        {
            public string Key { get; set; } = key;

            public DateTime Begin { get; set; } = begin.DateTime;
            public DateTime End { get; set; } = end.DateTime;

            public bool DieInPeace { get; set; } = dieInPeace;
        }

        #endregion

        #region Nested type: WidgetLocalSection

        #region Nested Type: WidgetLocalSection

        public class WidgetLocalSection(string key, string widgetId, string indicator, string data)
        {
            [Column(IsPrimary = true)]
            public string Key { get; set; } = key;

            [Column(IsPrimary = true)]
            public string WidgetId { get; set; } = widgetId;

            [Column(IsPrimary = true)]
            public string Indicator { get; set; } = indicator;

            [Column(DbType = "BLOB")]
            public string Data { get; set; } = data;
        }

        #endregion

        #endregion

        // Preferences
        // Activities

        // Preferences 会用 Key-PreferenceId-PreferenceValue 的形式储存

        #region Actions

        public void AppendAction(Action action) => freeSql.Insert(action).ExecuteAffrows();

        public IReadOnlyList<Action> GetLatestActions(string key, DateTimeOffset since) =>
            freeSql
               .Select<Action>()
               .Where(x => x.Key == key && x.Kind == ActionKind.EditPackage && x.At >= since.LocalDateTime)
               .OrderByDescending(x => x.At)
               .ToList();

        #endregion

        #region Activity

        public void AppendActivity(Activity activity) => freeSql.Insert(activity).ExecuteAffrows();

        public Activity? GetLastActivity(string key) =>
            freeSql.Select<Activity>().Where(x => x.Key == key).OrderByDescending(x => x.End).First();

        public TimeSpan GetTotalPlayTime(string key)
        {
            var totalSeconds = freeSql
                              .Select<Activity>()
                              .Where(x => x.Key == key)
                              .Sum(x => (x.End - x.Begin).TotalSeconds);
            return TimeSpan.FromSeconds((double)totalSeconds);
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

        #endregion

        #region Accounts

        public void AppendAccount(Account account) => freeSql.Insert(account).ExecuteAffrows();

        public IReadOnlyList<Account> GetAccounts() => freeSql.Select<Account>().ToList();

        public Account? GetDefaultAccount() => freeSql.Select<Account>().Where(x => x.IsDefault).First();

        public Account? GetAccount(string uuid) => freeSql.Select<Account>().Where(x => x.Uuid == uuid).First();

        public bool HasMicrosoftAccount() =>
            freeSql.Select<Account>().Where(x => x.Kind == nameof(MicrosoftAccount)).Any();

        public void MarkDefaultAccount(string uuid) =>
            freeSql.Transaction(() =>
            {
                freeSql.Update<Account>().Set(x => x.IsDefault, false).ExecuteAffrows();
                freeSql.Update<Account>().Where(x => x.Uuid == uuid).Set(x => x.IsDefault, true).ExecuteAffrows();
            });

        public void RemoveAccount(string uuid) => freeSql.Delete<Account>().Where(x => x.Uuid == uuid).ExecuteAffrows();

        public void UseAccount(string uuid) =>
            freeSql.Update<Account>().Where(x => x.Uuid == uuid).Set(x => x.LastUsedAt, DateTime.Now).ExecuteAffrows();

        public void UpdateAccount(string uuid, string data) =>
            freeSql.Update<Account>().Where(x => x.Uuid == uuid).Set(x => x.Data, data).ExecuteAffrows();

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
                    freeSql.Insert(new AccountSelector(key, uuid)).ExecuteAffrows();
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
            var widgetSection = new WidgetLocalSection(key, widgetId, indicator, serializedData);

            freeSql.InsertOrUpdate<WidgetLocalSection>().SetSource(widgetSection).ExecuteAffrows();
        }

        #endregion

        // public class Preference
        // {
        //     public const string BEHAVIOR_DEPLOY_METHOD = "behavior.deploy.method";
        //     public const string BEHAVIOR_DEPLOY_FASTMODE = "behavior.deploy.fastmode";
        //     public const string BEHAVIOR_RESOLVE_DEPENDENCY = "behavior.resolve.dependency";
        //
        //     public string Key { get; set; }
        //     public string Id { get; set; }
        //     public string? String { get; set; }
        //     public bool? Boolean { get; set; }
        //     public int? Integer { get; set; }
        // }
    }
}
