using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FreeSql.DataAnnotations;
using Microsoft.Extensions.Logging;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.Avalonia.Snapshots;

public class SnapshotStore : ISnapshotStore
{
    private readonly IFreeSql _freeSql;
    private readonly ILogger _logger;

    public SnapshotStore(IFreeSql freeSql, ILogger<SnapshotStore> logger)
    {
        _freeSql = freeSql;
        _logger = logger;

        logger.LogDebug("Initialized SnapshotStore");
    }

    #region Fields

    private bool _isDisposed;

    #endregion

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _freeSql.Dispose();
        _logger.LogDebug("SnapshotStore disposed");
    }

    public void InsertSnapshot(SnapshotInfo snapshot, IEnumerable<ReferenceInfo> references)
    {
        var guid = ParseId(snapshot.Id);
        _freeSql.Transaction(() =>
        {
            _freeSql.Insert(ToRecord(snapshot)).ExecuteAffrows();
            _freeSql.Insert(references.Select(x => ToRecord(x, guid))).ExecuteAffrows();
        });
    }

    public IReadOnlyList<SnapshotInfo> GetSnapshots()
    {
        return _freeSql.Select<SnapshotRecord>().OrderByDescending(x => x.CreatedAt).ToList().Select(FromRecord).ToList();
    }

    public SnapshotInfo? GetSnapshot(object id)
    {
        var guid = ParseId(id);
        return _freeSql.Select<SnapshotRecord>().Where(x => guid == x.Id).ToOne() is { } r ? FromRecord(r) : null;
    }

    public IReadOnlyList<ReferenceInfo> GetReferences(object snapshotId)
    {
        var guid = ParseId(snapshotId);
        return _freeSql.Select<ReferenceRecord>().Where(x => x.SnapshotId == guid).ToList().Select(FromRecord).ToList();
    }

    public void DeleteSnapshot(object id)
    {
        var guid = ParseId(id);
        _freeSql.Transaction(() =>
        {
            _freeSql.Delete<SnapshotRecord>().Where(x => x.Id == guid).ExecuteAffrows();
            _freeSql.Delete<ReferenceRecord>().Where(x => x.SnapshotId == guid).ExecuteAffrows();
        });
    }

    public ISet<string> GetAllReferencedHashes()
    {
        var data = new HashSet<string>(_freeSql.Select<ReferenceRecord>().ToList(x => x.Hash));
        return data;
    }

    public void DeleteOrphanReferences()
    {
        var existingSnapshotIds = _freeSql.Select<SnapshotRecord>().ToList(x => x.Id).ToHashSet();
        var orphanIds = _freeSql.Select<ReferenceRecord>().ToList(x => x.SnapshotId)
            .Distinct().Where(id => !existingSnapshotIds.Contains(id)).ToList();
        if (orphanIds.Count > 0)
            _freeSql.Delete<ReferenceRecord>()
                .Where(x => orphanIds.Contains(x.SnapshotId)).ExecuteAffrows();
    }

    #region Helper Methods

    private static Guid ParseId(object id)
    {
        var guid = id switch
        {
            Guid it => it,
            string it => Guid.Parse(it),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
        return guid;
    }

    private static SnapshotInfo FromRecord(SnapshotRecord record)
    {
        var metadata = JsonSerializer.Deserialize<Profile.Rice>(record.Metadata, JsonSerializerOptions.Default)
                    ?? throw new InvalidDataException($"Corrupted snapshot metadata: {record.Id}");

        var info = new SnapshotInfo(record.Id,
                                    record.Label,
                                    record.Remark,
                                    metadata,
                                    record.PackageCount,
                                    record.FileCount,
                                    record.TotalSize,
                                    record.CreatedAt);

        return info;
    }

    private static ReferenceInfo FromRecord(ReferenceRecord record)
    {
        var info = new ReferenceInfo(record.Id,
                                     record.Hash,
                                     record.RelativePath,
                                     record.Size,
                                     record.LastModifiedAt,
                                     (FileAttributes)record.Attributes);

        return info;
    }

    private static SnapshotRecord ToRecord(SnapshotInfo info)
    {
        var record = new SnapshotRecord()
        {
            Id = ParseId(info.Id),
            Label = info.Label,
            Remark = info.Remark,
            Metadata = JsonSerializer.Serialize(info.Metadata, JsonSerializerOptions.Default),
            PackageCount = info.PackageCount,
            FileCount = info.FileCount,
            TotalSize = info.TotalSize,
            CreatedAt = info.CreatedAt,
        };

        return record;
    }

    private static ReferenceRecord ToRecord(ReferenceInfo info, Guid snapshotId)
    {
        var record = new ReferenceRecord()
        {
            Id = ParseId(info.Id),
            SnapshotId = snapshotId,
            Hash = info.Hash,
            Size = info.Size,
            LastModifiedAt = info.LastModifiedAt,
            Attributes = (int)info.Attributes,
            RelativePath = info.RelativePath
        };
        return record;
    }

    #endregion

    #region Nested type: SnapshotRecord

    public class SnapshotRecord
    {
        [Column(IsPrimary = true)]
        public required Guid Id { get; init; }

        public required string Label { get; init; }
        public required string Remark { get; init; }

        [Column(DbType = "BLOB")]
        public required string Metadata { get; init; }

        public required int PackageCount { get; init; }
        public required int FileCount { get; init; }
        public required long TotalSize { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    #endregion

    #region Nested type: ReferenceRecord

    [Index("IX_Ref_SnapshotId", nameof(SnapshotId))]
    [Index("IX_Ref_Hash", nameof(Hash))]
    [Index("UQ_Ref_Snapshot_Path", $"{nameof(SnapshotId)},{nameof(RelativePath)}", IsUnique = true)]
    public class ReferenceRecord
    {
        [Column(IsPrimary = true)]
        public required Guid Id { get; init; }

        public required Guid SnapshotId { get; init; }
        public required string Hash { get; init; }
        public required string RelativePath { get; init; }
        public required long Size { get; init; }
        public required DateTime LastModifiedAt { get; init; }
        public required int Attributes { get; init; }
    }

    #endregion
}
