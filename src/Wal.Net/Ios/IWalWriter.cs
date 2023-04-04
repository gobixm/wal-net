using Wal.Net.Records;

namespace Wal.Net.Ios;

public interface IWalWriter
{
    long Offset { get; }
    Task WriteAsync(IEnumerable<WalRecord> records, CancellationToken cancellationToken = default);
    Task WriteAsync(WalRecord record, CancellationToken cancellationToken = default);
}