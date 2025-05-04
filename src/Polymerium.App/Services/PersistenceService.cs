using System;
using System.Collections.Generic;
using System.IO;
using FreeSql;
using FreeSql.DataAnnotations;
using Polymerium.Trident.Accounts;
using Trident.Abstractions;

namespace Polymerium.App.Services;

public class PersistenceService : IDisposable
{
    // Preferences
    // Activities

    // Preferences 会用 Key-PreferenceId-PreferenceValue 的形式储存

    private readonly Lazy<IFreeSql> _db = new(() =>
    {
        var dir = PathDef.Default.PrivateDirectory(Program.Brand);
        var path = Path.Combine(dir, "persistence.sqlite.db");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return new FreeSqlBuilder()
              .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
              .UseAutoSyncStructure(true)
              .Build();
    });

    public void AppendAction(Action action)
    {
        _db.Value.Insert(action).ExecuteAffrows();
    }

    public void AppendActivity(Activity activity)
    {
        _db.Value.Insert(activity).ExecuteAffrows();
    }

    public Activity? GetLastActivity(string key)
    {
        return _db.Value.Select<Activity>().Where(x => x.Key == key).OrderByDescending(x => x.End).First();
    }

    public TimeSpan GetTotalPlayTime(string key)
    {
        var totalSeconds = _db
                          .Value.Select<Activity>()
                          .Where(x => x.Key == key)
                          .Sum(x => (x.End - x.Begin).TotalSeconds);
        return TimeSpan.FromSeconds((double)totalSeconds);
    }

    public double GetPercentageInTotalPlayTime(string key)
    {
        var keyTotalSeconds = _db
                             .Value.Select<Activity>()
                             .Where(x => x.Key == key)
                             .Sum(x => (x.End - x.Begin).TotalSeconds);


        var allTotalSeconds = _db.Value.Select<Activity>().Sum(x => (x.End - x.Begin).TotalSeconds);


        if (allTotalSeconds == 0)
            return 0d;


        return (double)(keyTotalSeconds / allTotalSeconds);
    }

    public void AppendAccount(Account account)
    {
        _db.Value.Insert(account).ExecuteAffrows();
    }

    public IReadOnlyList<Account> GetAccounts() => _db.Value.Select<Account>().ToList();

    public Account? GetDefaultAccount()
    {
        return _db.Value.Select<Account>().Where(x => x.IsDefault).First();
    }

    public Account? GetAccount(string uuid)
    {
        return _db.Value.Select<Account>().Where(x => x.Uuid == uuid).First();
    }

    public bool HasMicrosoftAccount()
    {
        return _db.Value.Select<Account>().Where(x => x.Kind == nameof(MicrosoftAccount)).Any();
    }

    public void MarkDefaultAccount(string uuid)
    {
        _db.Value.Update<Account>().Set(x => x.IsDefault, false).ExecuteAffrows();
        _db.Value.Update<Account>().Where(x => x.Uuid == uuid).Set(x => x.IsDefault, true).ExecuteAffrows();
    }

    public void RemoveAccount(string uuid)
    {
        _db.Value.Delete<Account>().Where(x => x.Uuid == uuid).ExecuteAffrows();
    }

    public void Dispose()
    {
        // TODO 在此释放托管资源
        if (_db.IsValueCreated)
            _db.Value.Dispose();
    }

    public enum ActionKind { Install, Update, Unlock, Reset, Rename, EditPackage, EditLoader }

    public class Action(string key, ActionKind kind, string? old, string? @new)
    {
        public DateTimeOffset At { get; set; } = DateTimeOffset.Now;

        public string Key { get; set; } = key;
        public ActionKind Kind { get; set; } = kind;
        public string? Old { get; set; } = old;
        public string? New { get; set; } = @new;
    }

    public class Activity(string key, DateTimeOffset begin, DateTimeOffset end, bool dieInPeace)
    {
        public string Key { get; set; } = key;
        public DateTime Begin { get; set; } = begin.DateTime;
        public DateTime End { get; set; } = end.DateTime;

        public bool DieInPeace { get; set; } = dieInPeace;
    }

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
        public string Data { get; set; } = data;
        public DateTime EnrolledAt { get; set; } = enrolledAt;
        public DateTime? LastUsedAt { get; set; } = lastUsedAt;
        public bool IsDefault { get; set; } = isDefault;
    }

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